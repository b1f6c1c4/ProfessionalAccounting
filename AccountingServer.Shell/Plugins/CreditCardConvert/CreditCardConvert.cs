using System;
using System.Collections.Generic;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;
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
