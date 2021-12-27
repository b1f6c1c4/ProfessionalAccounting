/* Copyright (C) 2020-2021 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Util;

namespace AccountingServer.Shell.Carry;

internal partial class CarryShell
{
    public static IConfigManager<CarrySettings> CarrySettings { private get; set; } =
        new ConfigManager<CarrySettings>("Carry.xml");

    private long ResetCarry(DateFilter rng)
        => m_Accountant.DeleteVouchers($"{rng.AsDateRange()} Carry");

    /// <summary>
    ///     月末结转
    /// </summary>
    /// <param name="sb">日志记录</param>
    /// <param name="dt">月，若为<c>null</c>则表示对无日期进行结转</param>
    private void Carry(StringBuilder sb, DateTime? dt)
    {
        DateTime? ed;
        DateFilter rng;
        if (dt.HasValue)
        {
            var sd = new DateTime(dt.Value.Year, dt.Value.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            ed = AccountantHelper.LastDayOfMonth(dt.Value.Year, dt.Value.Month);
            rng = new(sd, ed);
        }
        else
        {
            ed = null;
            rng = DateFilter.TheNullOnly;
        }

        var tasks = CarrySettings.Config.UserSettings
            .Single(us => us.User == ClientUser.Name).Targets
            .Select(t => new CarryTask
                {
                    Target = t,
                    Value = 0D,
                    Voucher = new() { Date = ed, Type = VoucherType.Carry, Details = new() },
                }).ToList();
        foreach (var task in tasks)
            PartialCarry(sb, task, rng, false);

        var baseCur = BaseCurrency.At(ed);
        if (ed.HasValue)
        {
            var totalG =
                m_Accountant.RunGroupedQuery($"T3999 [~{ed.AsDate()}]`C")
                    .Items.Cast<ISubtotalCurrency>().Sum(
                        bal => m_Accountant.Query(ed.Value, bal.Currency, baseCur)
                            * bal.Fund);
            var totalC =
                tasks.SelectMany(t => t.Voucher.Details)
                    .Where(d => d.Title == 3999).Sum(
                        d => m_Accountant.Query(ed.Value, d.Currency, baseCur)
                            * d.Fund!.Value);

            var total = totalG + totalC;
            if (!total.IsZero())
            {
                sb.AppendLine(total < 0
                    ? $"{dt.AsDate(SubtotalLevel.Month)} CurrencyCarry Gain @{baseCur} {(-total).AsCurrency(baseCur)}"
                    : $"{dt.AsDate(SubtotalLevel.Month)} CurrencyCarry Lost @{baseCur} {(+total).AsCurrency(baseCur)}");
                m_Accountant.Upsert(new Voucher
                    {
                        Date = ed,
                        Type = VoucherType.Carry,
                        Remark = "currency carry",
                        Details = new()
                            {
                                new() { Currency = baseCur, Title = 3999, Fund = -total },
                                new()
                                    {
                                        Currency = baseCur, Title = 6603, SubTitle = 03, Fund = total,
                                    },
                            },
                    });
            }
        }

        foreach (var task in tasks)
            PartialCarry(sb, task, rng, true);

        if (tasks.Any(t => !t.Value.IsZero()))
        {
            var grand = tasks.Sum(t => t.Value);
            sb.AppendLine($"{dt.AsDate(SubtotalLevel.Month)} Carry => @@ {grand.AsCurrency(baseCur)}");
        }

        foreach (var task in tasks)
        {
            if (!task.Value.IsZero())
                task.Voucher.Details.Add(
                    new()
                        {
                            Currency = baseCur,
                            Title = 4103,
                            SubTitle = task.Target.IsSpecial ? 01 : null,
                            Fund = task.Value,
                        });

            if (task.Voucher.Details.Any())
                m_Accountant.Upsert(task.Voucher);
        }
    }

    /// <summary>
    ///     按目标月末结转
    /// </summary>
    /// <param name="sb">日志记录</param>
    /// <param name="task">任务</param>
    /// <param name="rng">范围</param>
    /// <param name="baseCurrency">是否为基准</param>
    /// <returns>结转记账凭证</returns>
    private void PartialCarry(StringBuilder sb, CarryTask task, DateFilter rng, bool baseCurrency)
    {
        var total = 0D;
        var ed = rng.NullOnly ? null : rng.EndDate;
        var target = task.Target;
        var voucher = task.Voucher;
        var baseCur = BaseCurrency.At(ed);
        var res =
            m_Accountant.RunGroupedQuery(
                $"({target.Query}) {(baseCurrency ? '*' : '-')}@{baseCur} {rng.AsDateRange()}`Ctscr");
        var flag = 0;
        foreach (var grpC in res.Items.Cast<ISubtotalCurrency>())
        {
            flag++;
            var b = grpC.Fund;
            sb.AppendLine(
                $"{rng.StartDate.AsDate(SubtotalLevel.Month)} PartialCarry => @{grpC.Currency} {b.AsCurrency(grpC.Currency)} (S={task.Target.IsSpecial})");
            foreach (var grpt in grpC.Items.Cast<ISubtotalTitle>())
            foreach (var grps in grpt.Items.Cast<ISubtotalSubTitle>())
            foreach (var grpc in grps.Items.Cast<ISubtotalContent>())
            foreach (var grpr in grpc.Items.Cast<ISubtotalRemark>())
                voucher.Details.Add(
                    new()
                        {
                            Currency = grpC.Currency,
                            Title = grpt.Title,
                            SubTitle = grps.SubTitle,
                            Content = grpc.Content,
                            Remark = grpr.Remark,
                            Fund = -grpr.Fund,
                        });

            if (b.IsZero())
                continue;

            if (grpC.Currency == baseCur)
            {
                total += b;
                continue;
            }

            if (!ed.HasValue)
                throw new InvalidOperationException("无穷长时间以前不存在汇率");

            var cob = m_Accountant.Query(ed.Value, grpC.Currency, baseCur) * b;

            voucher.Details.Add(new() { Currency = grpC.Currency, Title = 3999, Fund = b });
            voucher.Details.Add(new() { Currency = baseCur, Title = 3999, Remark = voucher.ID, Fund = -cob });
            total += cob;
        }

        if (flag >= 2)
            sb.AppendLine(
                $"{rng.StartDate.AsDate(SubtotalLevel.Month)} PartialCarry ==> @@ {total.AsCurrency(baseCur)} (S={task.Target.IsSpecial})");
        task.Value += total;
    }
}

[Serializable]
[XmlRoot("CarrySettings")]
public class CarrySettings
{
    [XmlElement("User")] public List<UserCarrySettings> UserSettings;
}

[Serializable]
public class UserCarrySettings
{
    [XmlElement("Target")] public List<CarryTarget> Targets;

    [XmlAttribute("name")]
    public string User { get; set; }
}

[Serializable]
public class CarryTarget
{
    [XmlText]
    public string Query { get; set; }

    [XmlAttribute("special")]
    public bool IsSpecial { get; set; }
}

internal class CarryTask
{
    public CarryTarget Target { get; init; }

    public double Value { get; set; }

    public Voucher Voucher { get; init; }
}
