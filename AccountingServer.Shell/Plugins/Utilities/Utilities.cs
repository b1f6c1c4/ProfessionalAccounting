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
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Plugins.Utilities;

/// <summary>
///     常见记账凭证自动填写
/// </summary>
internal class Utilities : PluginBase
{
    public Utilities(Accountant accountant) : base(accountant) { }

    public static IConfigManager<UtilTemplates> Templates { private get; set; } =
        new ConfigManager<UtilTemplates>("Util.xml");

    /// <inheritdoc />
    public override IQueryResult Execute(string expr, IEntitiesSerializer serializer)
    {
        var count = 0;
        while (!string.IsNullOrWhiteSpace(expr))
        {
            var voucher = GenerateVoucher(ref expr);
            if (voucher == null)
                continue;

            if (Accountant.Upsert(voucher))
                count++;
        }

        return new NumberAffected(count);
    }

    /// <inheritdoc />
    public override string ListHelp()
    {
        var sb = new StringBuilder();
        sb.Append(base.ListHelp());
        foreach (var util in Templates.Config.Templates)
            sb.AppendLine($"{util.Name,20}{util.Description}");

        return sb.ToString();
    }

    /// <summary>
    ///     根据表达式生成记账凭证
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <returns>记账凭证</returns>
    private Voucher GenerateVoucher(ref string expr)
    {
        var time = Parsing.UniqueTime(ref expr) ?? ClientDateTime.Today;
        var abbr = Parsing.Token(ref expr);

        var template = Templates.Config.Templates.FirstOrDefault(t => t.Name == abbr) ??
            throw new KeyNotFoundException($"找不到常见记账凭证{abbr}");

        var num = 1D;
        if (template.TemplateType is UtilTemplateType.Value or UtilTemplateType.Fill)
        {
            var valt = Parsing.Double(ref expr);
            var val = valt ?? (template.Default ?? throw new ApplicationException($"常见记账凭证{template.Name}没有默认值"));

            if (template.TemplateType == UtilTemplateType.Value)
                num = val;
            else
            {
                var arr = Accountant.RunGroupedQuery($"{template.Query} [~{time:yyyyMMdd}] ``v").Fund;
                num = arr - val;
            }
        }

        return num.IsZero() ? null : MakeVoucher(template, num, time);
    }

    /// <summary>
    ///     从模板生成记账凭证
    /// </summary>
    /// <param name="template">记账凭证模板</param>
    /// <param name="num">倍乘</param>
    /// <param name="time">日期</param>
    /// <returns></returns>
    private static Voucher MakeVoucher(Voucher template, double num, DateTime? time)
    {
        var lst =
            template.Details
                .Select(
                    d =>
                        new VoucherDetail
                            {
                                Currency = d.Currency,
                                Title = d.Title,
                                SubTitle = d.SubTitle,
                                Content =
                                    d.Content == "G()"
                                        ? Guid.NewGuid().ToString().ToUpperInvariant()
                                        : d.Content,
                                Fund = num * d.Fund,
                                Remark = d.Remark == "G()"
                                    ? Guid.NewGuid().ToString().ToUpperInvariant()
                                    : d.Remark,
                            })
                .ToList();
        var voucher = new Voucher { Date = time, Remark = template.Remark, Type = template.Type, Details = lst };
        return voucher;
    }
}