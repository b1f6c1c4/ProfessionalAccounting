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
using System.Text;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell.Plugins.CreditCardConvert;

/// <summary>
///     信用卡自动购汇
/// </summary>
internal class CreditCardConvert : PluginBase
{
    /// <inheritdoc />
    public override IAsyncEnumerable<string> Execute(string expr, Context ctx)
    {
        var content = Parsing.Token(ref expr);

        return ParsingF.Optional(ref expr, "q")
            ? Query(content, expr, ctx)
            : Create(content, expr, ctx);
    }

    private async IAsyncEnumerable<string> Query(string content, string expr, Context ctx)
    {
        var rng = Parsing.Range(ref expr, ctx.Client) ?? DateFilter.Unconstrained;

        var trans = new List<Trans>();
        var rebates = new List<Rebate>();
        var convs = new List<Conversion>();
        await foreach (var voucher in
                       ctx.Accountant.RunVoucherQueryAsync(
                           $"T224101 {content.Quotation('\'')} {rng.AsDateRange()}"))
        {
            var ds = voucher.Details.Where(d => d.Title == 2241 && d.SubTitle == 01 && d.Content == content)
                .ToList();
            if (ds.Count > 2)
                throw new ApplicationException("过多相关细目");

            if (ds.Count == 2)
            {
                var v1 = ds[0].Fund!.Value;
                var v2 = ds[1].Fund!.Value;

                if (v1 < 0 &&
                    v2 > 0)
                {
                    convs.Add(
                        new()
                            {
                                Date = voucher.Date,
                                TargetCurrency = ds[0].Currency,
                                TargetFund = -v1,
                                OriginCurrency = ds[1].Currency,
                                OriginFund = v2,
                            });
                    continue;
                }

                if (v1 > 0 &&
                    v2 < 0)
                {
                    convs.Add(
                        new()
                            {
                                Date = voucher.Date,
                                TargetCurrency = ds[1].Currency,
                                TargetFund = -v2,
                                OriginCurrency = ds[0].Currency,
                                OriginFund = v1,
                            });
                    continue;
                }
            }

            trans.AddRange(
                ds.Where(static d => d.Fund!.Value < 0)
                    .Select(
                        d => new Trans { Date = voucher.Date, RawCurrency = d.Currency, RawFund = -d.Fund.Value }));
        }


        await foreach (var voucher in ctx.Accountant.RunVoucherQueryAsync(
                           $"{{T224101 {content.Quotation('\'')}}}*{{T660300}}*{{T224101 {content.Quotation('\'')} +T3999+T660300 A {rng.AsDateRange()}}}"))
        {
            // Assume proper ordering here
            var d0 = voucher.Details.Single(static d => d.Title == 2241);
            var d1 = voucher.Details.Single(static d => d.Title == 6603);
            if (d0.Title != 2241 ||
                d0.SubTitle != 01 ||
                d0.Content != content)
                continue;
            if (d1.Title != 6603 ||
                d1.SubTitle != null ||
                d1.Content != null)
                continue;
            if (!d0.Fund.HasValue ||
                !d1.Fund.HasValue)
                continue;

            rebates.Add(
                new()
                    {
                        Date = voucher.Date,
                        RawCurrency = d1.Currency,
                        RawFund = d1.Fund.Value,
                        TheConversion = new()
                            {
                                Date = voucher.Date,
                                OriginCurrency = d1.Currency,
                                OriginFund = d1.Fund.Value,
                                TargetCurrency = d0.Currency,
                                TargetFund = d0.Fund.Value,
                            },
                    });
        }

        foreach (var tran in trans)
        {
            var conv = convs.FirstOrDefault(c
                => c.Date >= tran.Date && c.OriginCurrency == tran.RawCurrency &&
                (c.OriginFund - tran.RawFund).IsZero());
            if (conv == null)
                continue;

            tran.TheConversion = conv;
            convs.Remove(conv);
        }

        var sb = new StringBuilder();
        foreach (var conv in convs)
        {
            sb.Append(conv.Date.AsDate());
            sb.Append($" {conv.OriginCurrency.AsCurrency()} {conv.OriginFund.AsFund().CPadLeft(15)}");
            sb.Append(
                $" {conv.Date.AsDate()} {conv.TargetCurrency.AsCurrency()} {conv.TargetFund.AsFund().CPadLeft(15)} !!!\n");
            yield return sb.ToString();
            sb.Clear();
        }

        yield return "===========================================================\n";

        foreach (var tran in trans.Concat(rebates).OrderByDescending(static t => t.Date))
        {
            sb.Append(tran.Date.AsDate());
            sb.Append($" {tran.RawCurrency.AsCurrency()} {tran.RawFund.AsFund().CPadLeft(15)}");
            if (tran.TheConversion != null)
                sb.Append(
                    $" {tran.TheConversion.Date.AsDate()} {tran.TheConversion.TargetCurrency.AsCurrency()} {tran.TheConversion.TargetFund.AsFund().CPadLeft(15)}\n");
            else
                sb.Append("\n");
            yield return sb.ToString();
            sb.Clear();
        }
    }

    private async IAsyncEnumerable<string> Create(string content, string expr, Context ctx)
    {
        var currency = Parsing.Token(ref expr, false);
        var baseCurrency = Parsing.Token(ref expr, false);

        await using var vir = ctx.Accountant.Virtualize();
        while (!string.IsNullOrWhiteSpace(expr))
        {
            var date = Parsing.UniqueTime(ref expr, ctx.Client);
            if (!date.HasValue)
            {
                var dayF = ParsingF.DoubleF(ref expr);
                var day = (int)dayF;
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (day != dayF)
                    throw new ApplicationException("非整数日期");

                var today = ctx.Client.Today;
                date = today.Day < day
                    ? today.AddMonths(-1).AddDays(day - today.Day)
                    : today.AddDays(day - today.Day);
            }

            var from = ParsingF.DoubleF(ref expr);
            var to = ParsingF.DoubleF(ref expr);
            await ctx.Accountant.UpsertAsync(new Voucher
                {
                    Date = date.Value,
                    Details = new()
                        {
                            new()
                                {
                                    Currency = baseCurrency,
                                    Title = 2241,
                                    SubTitle = 01,
                                    Content = content,
                                    Fund = -to,
                                },
                            new() { Currency = baseCurrency, Title = 3999, Fund = to },
                            new() { Currency = currency, Title = 3999, Fund = -from },
                            from >= 0
                                ? new()
                                    {
                                        Currency = currency,
                                        Title = 2241,
                                        SubTitle = 01,
                                        Content = content,
                                        Fund = from,
                                    }
                                : new() { Currency = currency, Title = 6603, Fund = from },
                        },
                });
        }

        yield return $"{vir.CachedVouchers}\n";
    }

    private class Trans
    {
        public DateTime? Date;
        public string RawCurrency;
        public double RawFund;
        public Conversion TheConversion;
    }

    private sealed class Rebate : Trans { }

    private sealed class Conversion
    {
        public DateTime? Date;

        public string OriginCurrency;
        public double OriginFund;

        public string TargetCurrency;
        public double TargetFund;
    }
}
