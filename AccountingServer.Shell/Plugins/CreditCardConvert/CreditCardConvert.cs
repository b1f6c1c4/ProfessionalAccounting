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
        public CreditCardConvert(Accountant accountant, IEntitySerializer serializer) : base(
            accountant,
            serializer) { }

        /// <inheritdoc />
        public override IQueryResult Execute(string expr)
        {
            var content = Parsing.Token(ref expr);

            return ParsingF.Optional(ref expr, "q") ? Query(content, ref expr) : Create(content, ref expr);
        }

        private sealed class Trans
        {
            public DateTime? Date;
            public string RawCurrency;
            public double RawFund;
            public Conversion TheConversion;
        }

        private sealed class Conversion
        {
            public DateTime? Date;

            public string OriginCurrency;
            public double OriginFund;

            public string TargetCurrency;
            public double TargetFund;
        }

        private IQueryResult Query(string content, ref string expr)
        {
            var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;

            var trans = new List<Trans>();
            var convs = new List<Conversion>();
            foreach (var voucher in Accountant.RunVoucherQuery($"T2241 {content.Quotation('\'')} {rng.AsDateRange()}"))
            {
                var ds = voucher.Details.Where(d => d.Title == 2241 && d.Content == content).ToList();
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
                                    OriginFund = v2
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
                                    OriginFund = v1
                                });
                        continue;
                    }
                }

                trans.AddRange(
                    // ReSharper disable once PossibleInvalidOperationException
                    ds.Where(d => d.Fund.Value < 0)
                        .Select(
                            d => new Trans
                                {
                                    Date = voucher.Date,
                                    RawCurrency = d.Currency,
                                    RawFund = -d.Fund.Value
                                }));
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
            foreach (var tran in trans.OrderByDescending(t => t.Date))
            {
                sb.Append(tran.Date.AsDate());
                if (tran.RawCurrency == VoucherDetail.BaseCurrency)
                    sb.Append("                              ");
                sb.Append($" @{tran.RawCurrency} {tran.RawFund.AsCurrency().CPadLeft(15)}");
                if (tran.TheConversion != null)
                    sb.AppendLine(
                        $" {tran.TheConversion.Date.AsDate()} @{tran.TheConversion.TargetCurrency} {tran.TheConversion.TargetFund.AsCurrency().CPadLeft(15)}");
                else
                    sb.AppendLine();
            }

            return new UnEditableText(sb.ToString());
        }

        private IQueryResult Create(string content, ref string expr)
        {
            var currency = Parsing.Token(ref expr, false);

            var sb = new StringBuilder();
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

                    date = DateTime.Today.Day < day
                        ? DateTime.Today.AddMonths(-1).AddDays(day - DateTime.Today.Day)
                        : DateTime.Today.AddDays(day - DateTime.Today.Day);
                }

                var from = ParsingF.DoubleF(ref expr);
                var to = ParsingF.DoubleF(ref expr);
                var voucher = new Voucher
                    {
                        Date = date,
                        Details = new List<VoucherDetail>
                            {
                                new VoucherDetail
                                    {
                                        Title = 2241,
                                        SubTitle = 01,
                                        Content = content,
                                        Fund = -to
                                    },
                                new VoucherDetail
                                    {
                                        Title = 3999,
                                        Fund = to
                                    },
                                new VoucherDetail
                                    {
                                        Currency = currency,
                                        Title = 3999,
                                        Fund = -from
                                    },
                                new VoucherDetail
                                    {
                                        Currency = currency,
                                        Title = 2241,
                                        SubTitle = 01,
                                        Content = content,
                                        Fund = from
                                    }
                            }
                    };
                Accountant.Upsert(voucher);
                sb.Append(Serializer.PresentVoucher(voucher).Wrap());
            }

            return new EditableText(sb.ToString());
        }
    }
}
