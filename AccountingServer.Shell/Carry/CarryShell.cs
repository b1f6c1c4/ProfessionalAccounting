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
using System.Xml.Serialization;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Carry
{
    /// <summary>
    ///     结转表达式解释器
    /// </summary>
    internal class CarryShell : IShellComponent
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public CarryShell(Accountant helper) => m_Accountant = helper;

        public static IConfigManager<CarrySettings> CarrySettings { private get; set; } =
            new ConfigManager<CarrySettings>("Carry.xml");

        /// <inheritdoc />
        public IQueryResult Execute(string expr, IEntitiesSerializer serializer)
        {
            expr = expr.Rest();
            return expr?.Initial() switch
                {
                    "ap" => DoCarry(expr.Rest()),
                    "rst" => ResetCarry(expr.Rest()),
                    _ => throw new InvalidOperationException("表达式无效"),
                };
        }

        /// <inheritdoc />
        public bool IsExecutable(string expr) => expr.Initial() == "ca";

        /// <summary>
        ///     执行摊销
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private IQueryResult DoCarry(string expr)
        {
            var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
            Parsing.Eof(expr);

            var cnt = 0L;

            if (rng.NullOnly)
            {
                cnt += Carry(null);
                return new NumberAffected(cnt);
            }

            if (!rng.StartDate.HasValue ||
                !rng.EndDate.HasValue)
                throw new ArgumentException("时间范围无界", nameof(expr));

            var dt = new DateTime(rng.StartDate.Value.Year, rng.StartDate.Value.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            while (dt <= rng.EndDate.Value)
            {
                cnt += Carry(dt);
                dt = dt.AddMonths(1);
            }

            if (rng.Nullable)
                cnt += Carry(null);

            return new NumberAffected(cnt);
        }

        /// <summary>
        ///     取消摊销
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ResetCarry(string expr)
        {
            var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
            Parsing.Eof(expr);

            var cnt = m_Accountant.DeleteVouchers($"{rng.AsDateRange()} Carry");
            return new NumberAffected(cnt);
        }


        /// <summary>
        ///     月末结转
        /// </summary>
        /// <param name="dt">月，若为<c>null</c>则表示对无日期进行结转</param>
        /// <returns>记账凭证数</returns>
        private long Carry(DateTime? dt)
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
                PartialCarry(task, rng, false);

            var cnt = 0L;
            if (ed.HasValue)
            {
                var baseCur = BaseCurrency.At(ed);
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
                    m_Accountant.Upsert(
                        new Voucher
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
                    cnt++;
                }
            }

            foreach (var task in tasks)
                PartialCarry(task, rng, true);

            foreach (var task in tasks)
            {
                if (!task.Value.IsZero())
                    task.Voucher.Details.Add(
                        new()
                            {
                                Currency = BaseCurrency.At(task.Voucher.Date),
                                Title = 4103,
                                SubTitle = task.Target.IsSpecial ? 01 : null,
                                Fund = task.Value,
                            });

                if (task.Voucher.Details.Any())
                {
                    m_Accountant.Upsert(task.Voucher);
                    cnt++;
                }
            }

            return cnt;
        }

        /// <summary>
        ///     按目标月末结转
        /// </summary>
        /// <param name="task">任务</param>
        /// <param name="rng">范围</param>
        /// <param name="baseCurrency">是否为基准</param>
        /// <returns>结转记账凭证</returns>
        private void PartialCarry(CarryTask task, DateFilter rng, bool baseCurrency)
        {
            var total = 0D;
            var ed = rng.NullOnly ? null : rng.EndDate;
            var target = task.Target;
            var voucher = task.Voucher;
            var baseCur = BaseCurrency.At(ed);
            var res =
                m_Accountant.RunGroupedQuery(
                    $"({target.Query}) {(baseCurrency ? '*' : '-')}@{baseCur} {rng.AsDateRange()}`Ctsc");
            foreach (var grpC in res.Items.Cast<ISubtotalCurrency>())
            {
                var b = grpC.Fund;
                foreach (var grpt in grpC.Items.Cast<ISubtotalTitle>())
                foreach (var grps in grpt.Items.Cast<ISubtotalSubTitle>())
                foreach (var grpc in grps.Items.Cast<ISubtotalContent>())
                    voucher.Details.Add(
                        new()
                            {
                                Currency = grpC.Currency,
                                Title = grpt.Title,
                                SubTitle = grps.SubTitle,
                                Content = grpc.Content,
                                Fund = -grpc.Fund,
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

                var cob = m_Accountant.Query(ed.Value, grpC.Currency, baseCur)
                    * b;

                voucher.Details.Add(
                    new() { Currency = grpC.Currency, Title = 3999, Fund = b });
                voucher.Details.Add(
                    new() { Currency = baseCur, Title = 3999, Remark = voucher.ID, Fund = -cob });
                total += cob;
            }

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
}
