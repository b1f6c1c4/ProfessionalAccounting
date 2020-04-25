using System;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL.Parsing
{
    internal partial class QueryParser
    {
        public partial class VoucherQueryContext : IVoucherQueryAtom
        {
            /// <inheritdoc />
            public bool ForAll => MatchAllMark() != null;

            /// <inheritdoc />
            public Voucher VoucherFilter
            {
                get
                {
                    var vfilter = new Voucher
                        {
                            ID = CaretQuotedString()?.GetText().Dequotation(),
                            Remark = PercentQuotedString()?.GetText().Dequotation(),
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
            public IQueryCompounded<IDetailQueryAtom> DetailFilter => details();

            /// <inheritdoc />
            public bool IsDangerous()
                => VoucherFilter.IsDangerous() && Range.IsDangerous() && DetailFilter.IsDangerous();

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IVoucherQueryAtom, T> visitor) => visitor.Visit(this);
        }

        public partial class VouchersContext : IQueryAry<IVoucherQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator => OperatorType.None;

            /// <inheritdoc />
            public IQueryCompounded<IVoucherQueryAtom> Filter1
                => (IQueryCompounded<IVoucherQueryAtom>)voucherQuery() ?? vouchers2();

            /// <inheritdoc />
            public IQueryCompounded<IVoucherQueryAtom> Filter2 => throw new MemberAccessException("表达式错误");

            /// <inheritdoc />
            public bool IsDangerous() => Filter1.IsDangerous();

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IVoucherQueryAtom, T> visitor) => visitor.Visit(this);
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
                            return OperatorType.Subtract;
                        default:
                            throw new MemberAccessException("表达式错误");
                    }
                }
            }

            /// <inheritdoc />
            public IQueryCompounded<IVoucherQueryAtom> Filter1
            {
                get
                {
                    if (vouchers2() != null)
                        return vouchers2();

                    return vouchers1();
                }
            }

            /// <inheritdoc />
            public IQueryCompounded<IVoucherQueryAtom> Filter2 => vouchers1();

            /// <inheritdoc />
            public bool IsDangerous()
            {
                switch (Operator)
                {
                    case OperatorType.None:
                        return Filter1.IsDangerous();
                    case OperatorType.Identity:
                        return Filter1.IsDangerous();
                    case OperatorType.Complement:
                        return true;
                    case OperatorType.Union:
                        return Filter1.IsDangerous() || Filter2.IsDangerous();
                    case OperatorType.Subtract:
                        return Filter1.IsDangerous();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IVoucherQueryAtom, T> visitor) => visitor.Visit(this);
        }

        public partial class Vouchers1Context : IQueryAry<IVoucherQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator => Op == null ? OperatorType.None : OperatorType.Intersect;

            /// <inheritdoc />
            public IQueryCompounded<IVoucherQueryAtom> Filter1 => vouchers0();

            /// <inheritdoc />
            public IQueryCompounded<IVoucherQueryAtom> Filter2 => vouchers1();

            /// <inheritdoc />
            public bool IsDangerous() => Filter1.IsDangerous() && (Filter2?.IsDangerous() ?? true);

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IVoucherQueryAtom, T> visitor) => visitor.Visit(this);
        }

        public partial class Vouchers0Context : IQueryAry<IVoucherQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator => OperatorType.None;

            /// <inheritdoc />
            public IQueryCompounded<IVoucherQueryAtom> Filter1 => voucherQuery() ??
                (IQueryCompounded<IVoucherQueryAtom>)vouchers2();

            /// <inheritdoc />
            public IQueryCompounded<IVoucherQueryAtom> Filter2 => null;

            /// <inheritdoc />
            public bool IsDangerous() => Filter1.IsDangerous();

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IVoucherQueryAtom, T> visitor) => visitor.Visit(this);
        }

        public partial class EmitContext : IEmit
        {
            /// <inheritdoc />
            public IQueryCompounded<IDetailQueryAtom> DetailFilter => details();
        }

        public partial class VoucherDetailQueryContext : IVoucherDetailQuery
        {
            /// <inheritdoc />
            public IQueryCompounded<IVoucherQueryAtom> VoucherQuery
                => (IQueryCompounded<IVoucherQueryAtom>)voucherQuery() ?? vouchers();

            /// <inheritdoc />
            public IEmit DetailEmitFilter => emit();
        }
    }
}
