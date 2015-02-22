﻿using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class ConsoleParser
    {
        public partial class VoucherQueryContext : IVoucherQueryAtom
        {
            public bool ForAll { get { return Op != null && Op.Text == "A"; } }

            public Voucher VoucherFilter
            {
                get
                {
                    var vfilter = new Voucher
                                      {
                                          ID = DollarQuotedString().Dequotation(),
                                          Remark = PercentQuotedString().Dequotation()
                                      };
                    if (VoucherType() != null)
                    {
                        var s = VoucherType().GetText();
                        VoucherType type;
                        if (Enum.TryParse(s, out type))
                            vfilter.Type = type;
                        else
                            throw new InvalidOperationException();
                    }
                    return vfilter;
                }
            }

            public DateFilter Range
            {
                get
                {
                    if (range() == null)
                        return DateFilter.Unconstrained;
                    return range().Range;
                }
            }

            public IQueryCompunded<IDetailQueryAtom> DetailFilter { get { return details(); } }
        }

        public partial class VouchersContext : IQueryAry<IVoucherQueryAtom>
        {
            public OperatorType Operator { get { return OperatorType.None; } }

            public IQueryCompunded<IVoucherQueryAtom> Filter1
            {
                get
                {
                    if (voucherQuery() != null)
                        return voucherQuery();
                    return vouchersB();
                }
            }

            public IQueryCompunded<IVoucherQueryAtom> Filter2 { get { throw new InvalidOperationException(); } }
        }

        public partial class VouchersBContext : IQueryAry<IVoucherQueryAtom>
        {
            public OperatorType Operator
            {
                get
                {
                    if (Op == null)
                        return OperatorType.None;
                    if (vouchersB().Count == 1)
                    {
                        if (Op.Text == "+")
                            return OperatorType.Identity;
                        if (Op.Text == "-")
                            return OperatorType.Complement;
                        throw new InvalidOperationException();
                    }
                    if (Op.Text == "+")
                        return OperatorType.Union;
                    if (Op.Text == "-")
                        return OperatorType.Substract;
                    if (Op.Text == "*")
                        return OperatorType.Intersect;
                    throw new InvalidOperationException();
                }
            }

            public IQueryCompunded<IVoucherQueryAtom> Filter1
            {
                get
                {
                    if (voucherQuery() != null)
                        return voucherQuery();
                    return vouchersB(0);
                }
            }

            public IQueryCompunded<IVoucherQueryAtom> Filter2 { get { return vouchersB(1); } }
        }

        public partial class SubtotalContext : ISubtotal
        {
            public bool NonZero { get { return SubtotalMark.Text.Length < 2; } }

            public IReadOnlyList<SubtotalLevel> Levels
            {
                get
                {
                    if (SubtotalFields() == null)
                        return new[] { SubtotalLevel.Title, SubtotalLevel.SubTitle, SubtotalLevel.Content };

                    if (SubtotalFields().GetText() == "v")
                        return new SubtotalLevel[0];

                    return SubtotalFields().GetText()
                                           .Select(
                                                   ch =>
                                                   {
                                                       switch (ch)
                                                       {
                                                           case 't':
                                                               return SubtotalLevel.Title;
                                                           case 's':
                                                               return SubtotalLevel.SubTitle;
                                                           case 'c':
                                                               return SubtotalLevel.Content;
                                                           case 'r':
                                                               return SubtotalLevel.Remark;
                                                           case 'd':
                                                               return SubtotalLevel.Day;
                                                           case 'w':
                                                               return SubtotalLevel.Week;
                                                           case 'm':
                                                               return SubtotalLevel.Month;
                                                           case 'f':
                                                               return SubtotalLevel.FinancialMonth;
                                                           case 'b':
                                                               return SubtotalLevel.BillingMonth;
                                                           case 'y':
                                                               return SubtotalLevel.Year;
                                                       }
                                                       throw new InvalidOperationException();
                                                   }).ToList();
                }
            }

            public AggregationType AggrType
            {
                get
                {
                    if (subtotalAggr() == null)
                        return AggregationType.None;
                    if (subtotalAggr().IsAll == null &&
                        subtotalAggr().rangeCore() == null)
                        return AggregationType.ChangedDay;
                    return AggregationType.EveryDay;
                }
            }

            public IDateRange EveryDayRange
            {
                get
                {
                    if (subtotalAggr().IsAll != null)
                        return DateFilter.Unconstrained;
                    return subtotalAggr().rangeCore();
                }
            }
        }

        public partial class EmitContext : IEmit
        {
            public IQueryCompunded<IDetailQueryAtom> DetailFilter { get { return details(); } }
        }

        public partial class VoucherDetailQueryContext : IVoucherDetailQuery
        {
            public IQueryCompunded<IVoucherQueryAtom> VoucherQuery
            {
                get
                {
                    if (voucherQuery() != null)
                        return voucherQuery();
                    return vouchers();
                }
            }

            public IEmit DetailEmitFilter { get { return emit(); } }
        }


        public partial class GroupedQueryContext : IGroupedQuery
        {
            public IVoucherDetailQuery VoucherEmitQuery { get { return voucherDetailQuery(); } }
            public ISubtotal Subtotal { get { return subtotal(); } }
        }
    }
}
