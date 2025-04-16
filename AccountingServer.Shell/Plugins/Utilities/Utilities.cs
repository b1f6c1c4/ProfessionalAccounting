/* Copyright (C) 2020-2025 b1f6c1c4
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
using System.Threading.Tasks;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell.Plugins.Utilities;

/// <summary>
///     常见记账凭证自动填写
/// </summary>
internal class Utilities : PluginBase
{
    static Utilities()
        => Cfg.RegisterType<UtilTemplates>("Util");

    /// <inheritdoc />
    public override async IAsyncEnumerable<string> Execute(string expr, Session session)
    {
        var body = expr;
        expr = ParsingF.Line(ref body);

        var dt = string.IsNullOrWhiteSpace(body)
            ? Parsing.UniqueTime(ref expr, session.Client)
            : session.Client.Today;

        var abbr = Parsing.Token(ref expr);
        var template = Cfg.Get<UtilTemplates>().Templates.FirstOrDefault(t => t.Name == abbr) ??
            throw new KeyNotFoundException($"找不到常见记账凭证{abbr}");

        if (!string.IsNullOrWhiteSpace(body))
        {
            ParsingF.Eof(expr);

            while (!string.IsNullOrWhiteSpace(body))
            {
                var line = ParsingF.Line(ref body);
                switch (Parsing.Optional(ref line, "=", "+", "-"))
                {
                    case "+":
                        dt = dt?.AddDays(1);
                        break;
                    case "-":
                        dt = dt?.AddDays(1);
                        break;
                    case "=":
                        break;
                    default:
                        dt = Parsing.UniqueTime(ref line, session.Client);
                        break;
                }
                while (!string.IsNullOrWhiteSpace(line))
                {
                    var (voucher, ee) = await GenerateVoucher(session, template, line, dt, false);
                    line = ee;
                    if (voucher == null)
                        continue;

                    await session.Accountant.UpsertAsync(voucher);
                    yield return session.Serializer.PresentVoucher(voucher).Wrap();
                }
                ParsingF.Eof(line);
            }
            ParsingF.Eof(body);
        }
        else
        {
            var (voucher, ee) = await GenerateVoucher(session, template, expr, dt, true);
            expr = ee;
            if (voucher != null)
            {
                await session.Accountant.UpsertAsync(voucher);
                yield return session.Serializer.PresentVoucher(voucher).Wrap();
            }
        }

        ParsingF.Eof(expr);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<string> ListHelp()
    {
        await foreach (var s in base.ListHelp())
            yield return s;

        foreach (var util in Cfg.Get<UtilTemplates>().Templates)
            yield return $"{util.Name,20} {(util.TemplateType == UtilTemplateType.Fixed ? ' ' : '*')} {util.Description}\n";
    }

    private async ValueTask<(Voucher, string)> GenerateVoucher(Session session, UtilTemplate template, string expr,
        DateTime? time, bool allowEmpty = true)
    {
        var num = 1D;
        if (template.TemplateType is UtilTemplateType.Value or UtilTemplateType.Fill)
        {
            double val;
            if (allowEmpty)
            {
                var valt = Parsing.Double(ref expr);
                val = valt ?? (template.Default ?? throw new ApplicationException($"常见记账凭证{template.Name}没有默认值"));
            }
            else
            {
                if (!Parsing.Optional(ref expr, "."))
                    val = ParsingF.Double(ref expr)!.Value;
                else
                    val = template.Default ?? throw new ApplicationException($"常见记账凭证{template.Name}没有默认值");
            }

            if (template.TemplateType == UtilTemplateType.Value)
                num = val;
            else
            {
                var arr = (await session.Accountant.RunGroupedQueryAsync($"{template.Query} [~{time:yyyyMMdd}] ``v"))
                    .Fund;
                num = arr - val;
            }
        }
        else if (!allowEmpty)
        {
            if (!Parsing.Optional(ref expr, "."))
                return (null, expr);
        }

        return (num.IsZero() ? null : MakeVoucher(template, num, time), expr);
    }

    /// <summary>
    ///     从模板生成记账凭证
    /// </summary>
    /// <param name="template">记账凭证模板</param>
    /// <param name="num">倍乘</param>
    /// <param name="time">日期</param>
    /// <returns>记账凭证</returns>
    private static Voucher MakeVoucher(Voucher template, double num, DateTime? time)
    {
        var lst =
            template.Details
                .Select(
                    d =>
                        new VoucherDetail
                            {
                                User = d.User,
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
