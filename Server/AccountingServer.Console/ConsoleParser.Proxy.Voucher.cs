using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class ConsoleParser
    {
        public partial class VoucherQueryContext : IVoucherQueryAtom
        {
            /// <inheritdoc />
            public bool ForAll { get { return Op != null && Op.Text == "A"; } }

            /// <inheritdoc />
            public Voucher VoucherFilter
            {
                get
                {
                    var vfilter = new Voucher
                                      {
                                          ID = CaretQuotedString().Dequotation(),
                                          Remark = PercentQuotedString().Dequotation()
                                      };
                    if (VoucherType() != null)
                    {
                        var s = VoucherType().GetText();
                        VoucherType type;
                        if (Enum.TryParse(s, out type))
                            vfilter.Type = type;
                        else if (s == "G")
                            vfilter.Type = Entities.VoucherType.General;
                        else
                            throw new InvalidOperationException();
                    }
                    return vfilter;
                }
            }

            /// <inheritdoc />
            public DateFilter Range { get { return range().TheRange(); } }

            /// <inheritdoc />
            public IQueryCompunded<IDetailQueryAtom> DetailFilter { get { return details(); } }
        }

        public partial class VouchersContext : IQueryAry<IVoucherQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator { get { return OperatorType.None; } }

            /// <inheritdoc />
            public IQueryCompunded<IVoucherQueryAtom> Filter1
            {
                get
                {
                    if (voucherQuery() != null)
                        return voucherQuery();
                    return vouchersB();
                }
            }

            /// <inheritdoc />
            public IQueryCompunded<IVoucherQueryAtom> Filter2 { get { throw new InvalidOperationException(); } }
        }

        public partial class VouchersBContext : IQueryAry<IVoucherQueryAtom>
        {
            /// <inheritdoc />
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

            /// <inheritdoc />
            public IQueryCompunded<IVoucherQueryAtom> Filter1
            {
                get
                {
                    if (voucherQuery() != null)
                        return voucherQuery();
                    return vouchersB(0);
                }
            }

            /// <inheritdoc />
            public IQueryCompunded<IVoucherQueryAtom> Filter2 { get { return vouchersB(1); } }
        }

        public partial class SubtotalContext : ISubtotal
        {
            /// <inheritdoc />
            public GatheringType GatherType
            {
                get
                {
                    switch (SubtotalMark.Text)
                    {
                        case "`":
                            return GatheringType.NonZero;
                        case "``":
                            return GatheringType.Zero;
                        case "!":
                            return GatheringType.Count;
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }

            /// <inheritdoc />
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

            /// <inheritdoc />
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

            /// <inheritdoc />
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
            /// <inheritdoc />
            public IQueryCompunded<IDetailQueryAtom> DetailFilter { get { return details(); } }
        }

        public partial class VoucherDetailQueryContext : IVoucherDetailQuery
        {
            /// <inheritdoc />
            public IQueryCompunded<IVoucherQueryAtom> VoucherQuery
            {
                get
                {
                    if (voucherQuery() != null)
                        return voucherQuery();
                    return vouchers();
                }
            }

            /// <inheritdoc />
            public IEmit DetailEmitFilter { get { return emit(); } }
        }


        public partial class GroupedQueryContext : IGroupedQuery
        {
            /// <inheritdoc />
            public IVoucherDetailQuery VoucherEmitQuery { get { return voucherDetailQuery(); } }

            /// <inheritdoc />
            public ISubtotal Subtotal { get { return subtotal(); } }
        }
    }
}
