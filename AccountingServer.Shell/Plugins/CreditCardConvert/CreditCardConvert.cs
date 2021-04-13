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
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell.Plugins.CreditCardConvert
{
    /// <summary>
    ///     信用卡自动购汇
    /// </summary>
    internal class CreditCardConvert : PluginBase
    {
        public CreditCardConvert(Accountant accountant) : base(accountant) { }

        /// <inheritdoc />
        public override IQueryResult Execute(string expr, IEntitiesSerializer serializer)
        {
            var content = Parsing.Token(ref expr);

            return ParsingF.Optional(ref expr, "q") ? Query(content, ref expr) : Create(content, ref expr, serializer);
        }

        private IQueryResult Query(string content, ref string expr)
        {
            var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;

            var trans = new List<Trans>();
            var rebates = new List<Rebate>();
            var convs = new List<Conversion>();
            foreach (var voucher in Accountant.RunVoucherQuery($"T224101 {content.Quotation('\'')} {rng.AsDateRange()}")
            )
            {
                var ds = voucher.Details.Where(d => d.Title == 2241 && d.SubTitle == 01 && d.Content == content)
                    .ToList();
                if (ds.Count > 2)
                    throw new ApplicationException("过多相关细目");

                if (ds.Count == 2)
                {
                    // ReSharper disable PossibleInvalidOperationException
                    var v1 = ds[0].Fund.Value;
                    var v2 = ds[1].Fund.Value;
                    // ReSharper restore PossibleInvalidOperationException

                    if (v1 < 0 &&
                        v2 > 0)
                    {
                        convs.Add(
                            new Conversion
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
                            new Conversion
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
                    // ReSharper disable once PossibleInvalidOperationException
                    ds.Where(d => d.Fund.Value < 0)
                        .Select(
                            d => new Trans { Date = voucher.Date, RawCurrency = d.Currency, RawFund = -d.Fund.Value }));
            }


            foreach (var voucher in Accountant.RunVoucherQuery(
                $"{{T224101 {content.Quotation('\'')}}}*{{T660300}}*{{T224101 {content.Quotation('\'')} +T3999+T660300 A {rng.AsDateRange()}}}")
            )
            {
                // Assume proper ordering here
                var d0 = voucher.Details.Single(d => d.Title == 2241);
                var d1 = voucher.Details.Single(d => d.Title == 6603);
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
                    new Rebate
                        {
                            Date = voucher.Date,
                            RawCurrency = d1.Currency,
                            RawFund = d1.Fund.Value,
                            TheConversion = new Conversion
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
                var conv = convs.FirstOrDefault(
                    c => c.Date >= tran.Date && c.OriginCurrency == tran.RawCurrency &&
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
                sb.Append($" @{conv.OriginCurrency} {conv.OriginFund.AsCurrency().CPadLeft(15)}");
                sb.AppendLine(
                    $" {conv.Date.AsDate()} @{conv.TargetCurrency} {conv.TargetFund.AsCurrency().CPadLeft(15)} !!!");
            }

            if (sb.Length != 0)
                sb.AppendLine("===========================================================");

            foreach (var tran in trans.Concat(rebates).OrderByDescending(t => t.Date))
            {
                sb.Append(tran.Date.AsDate());
                sb.Append($" @{tran.RawCurrency} {tran.RawFund.AsCurrency().CPadLeft(15)}");
                if (tran.TheConversion != null)
                    sb.AppendLine(
                        $" {tran.TheConversion.Date.AsDate()} @{tran.TheConversion.TargetCurrency} {tran.TheConversion.TargetFund.AsCurrency().CPadLeft(15)}");
                else
                    sb.AppendLine();
            }

            return new PlainText(sb.ToString());
        }

        private IQueryResult Create(string content, ref string expr, IEntitiesSerializer serializer)
        {
            var currency = Parsing.Token(ref expr, false);
            var baseCurrency = Parsing.Token(ref expr, false);

            var lst = new List<Voucher>();
            while (!string.IsNullOrWhiteSpace(expr))
            {
                var date = Parsing.UniqueTime(ref expr);
                if (!date.HasValue)
                {
                    var dayF = ParsingF.DoubleF(ref expr);
                    var day = (int)dayF;
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (day != dayF)
                        throw new ApplicationException("非整数日期");

                    date = ClientDateTime.Today.Day < day
                        ? ClientDateTime.Today.AddMonths(-1).AddDays(day - ClientDateTime.Today.Day)
                        : ClientDateTime.Today.AddDays(day - ClientDateTime.Today.Day);
                }

                var from = ParsingF.DoubleF(ref expr);
                var to = ParsingF.DoubleF(ref expr);
                var voucher = new Voucher
                    {
                        Date = date.Value,
                        Details = new List<VoucherDetail>
                            {
                                new VoucherDetail
                                    {
                                        Currency = baseCurrency,
                                        Title = 2241,
                                        SubTitle = 01,
                                        Content = content,
                                        Fund = -to,
                                    },
                                new VoucherDetail { Currency = baseCurrency, Title = 3999, Fund = to },
                                new VoucherDetail { Currency = currency, Title = 3999, Fund = -from },
                                from >= 0
                                    ? new VoucherDetail
                                        {
                                            Currency = currency,
                                            Title = 2241,
                                            SubTitle = 01,
                                            Content = content,
                                            Fund = from,
                                        }
                                    : new VoucherDetail { Currency = currency, Title = 6603, Fund = from },
                            },
                    };
                Accountant.Upsert(voucher);
                lst.Add(voucher);
            }

            if (lst.Any())
                return new DirtyText(serializer.PresentVouchers(lst));

            return new PlainSucceed();
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
}
