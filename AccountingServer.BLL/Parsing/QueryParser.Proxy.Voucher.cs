using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;

namespace AccountingServer.BLL.Parsing
{
    internal partial class QueryParser
    {
        public partial class VoucherQueryContext : IVoucherQueryAtom
        {
            /// <inheritdoc />
            public bool ForAll => Op != null && Op.Text == "A";

            /// <inheritdoc />
            public Voucher VoucherFilter
            {
                get
                {
                    var vfilter = new Voucher
                        {
                            ID = CaretQuotedString()?.GetText().Dequotation(),
                            Remark = PercentQuotedString()?.GetText().Dequotation()
                        };
                    if (VoucherType() != null)
                    {
                        var s = VoucherType().GetText();
                        if (Enum.TryParse(s, out VoucherType type))
                            vfilter.Type = type;
                        else if (s == "G")
                            vfilter.Type = Entities.VoucherType.General;
                        else
                            throw new MemberAccessException("表达式错误");
                    }

                    return vfilter;
                }
            }

            /// <inheritdoc />
            public DateFilter Range => range().TheRange();

            /// <inheritdoc />
            public IQueryCompunded<IDetailQueryAtom> DetailFilter => details();
        }

        public partial class VouchersContext : IQueryAry<IVoucherQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator => OperatorType.None;

            /// <inheritdoc />
            public IQueryCompunded<IVoucherQueryAtom> Filter1
            {
                get
                {
                    if (voucherQuery() != null)
                        return voucherQuery();

                    return vouchers2();
                }
            }

            /// <inheritdoc />
            public IQueryCompunded<IVoucherQueryAtom> Filter2 => throw new MemberAccessException("表达式错误");
        }

        public partial class Vouchers2Context : IQueryAry<IVoucherQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator
            {
                get
                {
                    if (Op == null)
                        return OperatorType.None;

                    if (vouchers2() == null)
                        switch (Op.Text)
                        {
                            case "+":
                                return OperatorType.Identity;
                            case "-":
                                return OperatorType.Complement;
                            default:
                                throw new MemberAccessException("表达式错误");
                        }

                    switch (Op.Text)
                    {
                        case "+":
                            return OperatorType.Union;
                        case "-":
                            return OperatorType.Substract;
                        default:
                            throw new MemberAccessException("表达式错误");
                    }
                }
            }

            /// <inheritdoc />
            public IQueryCompunded<IVoucherQueryAtom> Filter1
            {
                get
                {
                    if (vouchers2() != null)
                        return vouchers2();

                    return vouchers1();
                }
            }

            /// <inheritdoc />
            public IQueryCompunded<IVoucherQueryAtom> Filter2 => vouchers1();
        }

        public partial class Vouchers1Context : IQueryAry<IVoucherQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator => Op == null ? OperatorType.None : OperatorType.Intersect;

            /// <inheritdoc />
            public IQueryCompunded<IVoucherQueryAtom> Filter1 => vouchers0();

            /// <inheritdoc />
            public IQueryCompunded<IVoucherQueryAtom> Filter2 => vouchers1();
        }

        public partial class Vouchers0Context : IQueryAry<IVoucherQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator => OperatorType.None;

            /// <inheritdoc />
            public IQueryCompunded<IVoucherQueryAtom> Filter1 => voucherQuery() ??
                (IQueryCompunded<IVoucherQueryAtom>)vouchers2();

            /// <inheritdoc />
            public IQueryCompunded<IVoucherQueryAtom> Filter2 => null;
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
                            throw new MemberAccessException("表达式错误");
                    }
                }
            }

            /// <inheritdoc />
            public IReadOnlyList<SubtotalLevel> Levels
            {
                get
                {
                    if (SubtotalFields() == null)
                        return new[]
                            {
                                SubtotalLevel.Currency, SubtotalLevel.Title, SubtotalLevel.SubTitle,
                                SubtotalLevel.Content
                            };

                    if (SubtotalFields().GetText() == "v")
                        return new SubtotalLevel[0];

                    return SubtotalFields()
                        .GetText()
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
                                    case 'C':
                                        return SubtotalLevel.Currency;
                                    case 'd':
                                        return SubtotalLevel.Day;
                                    case 'w':
                                        return SubtotalLevel.Week;
                                    case 'm':
                                        return SubtotalLevel.Month;
                                    case 'y':
                                        return SubtotalLevel.Year;
                                }

                                throw new MemberAccessException("表达式错误");
                            })
                        .ToList();
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
            public IQueryCompunded<IDetailQueryAtom> DetailFilter => details();
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
            public IEmit DetailEmitFilter => emit();
        }


        public partial class GroupedQueryContext : IGroupedQuery
        {
            /// <inheritdoc />
            public IVoucherDetailQuery VoucherEmitQuery => voucherDetailQuery();

            /// <inheritdoc />
            public ISubtotal Subtotal => subtotal();
        }
    }
}
