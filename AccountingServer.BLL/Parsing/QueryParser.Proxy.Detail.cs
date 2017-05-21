using System;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;

namespace AccountingServer.BLL.Parsing
{
    internal partial class QueryParser
    {
        public partial class TitleContext : ITitle
        {
            public int Title
            {
                get
                {
                    if (DetailTitle() != null)
                    {
                        var tt = int.Parse(DetailTitle().GetText().TrimStart('T'));
                        return tt;
                    }

                    if (DetailTitleSubTitle() != null)
                    {
                        var tt = int.Parse(DetailTitleSubTitle().GetText().TrimStart('T'));
                        return tt / 100;
                    }

                    throw new MemberAccessException("表达式错误");
                }
            }

            public int? SubTitle
            {
                get
                {
                    if (DetailTitle() != null)
                        return null;

                    if (DetailTitleSubTitle() != null)
                    {
                        var tt = int.Parse(DetailTitleSubTitle().GetText().TrimStart('T'));
                        return tt % 100;
                    }

                    throw new MemberAccessException("表达式错误");
                }
            }
        }

        public partial class DetailQueryContext : IDetailQueryAtom
        {
            /// <inheritdoc />
            public VoucherDetail Filter
            {
                get
                {
                    var t = title();
                    var filter = new VoucherDetail
                        {
                            Title = t?.Title,
                            SubTitle = t?.SubTitle,
                            Content = SingleQuotedString()?.GetText().Dequotation(),
                            Remark = DoubleQuotedString()?.GetText().Dequotation()
                        };

                    if (VoucherCurrency() != null)
                    {
                        var c = VoucherCurrency().GetText();
                        if (!c.StartsWith("@", StringComparison.Ordinal))
                            throw new MemberAccessException("表达式错误");

                        filter.Currency = c == "@@" ? VoucherDetail.BaseCurrency : c.Substring(1).ToUpperInvariant();
                    }

                    if (Floating() != null)
                    {
                        var f = Floating().GetText().Substring(1);
                        filter.Fund = double.Parse(f);
                    }

                    return filter;
                }
            }

            /// <inheritdoc />
            public int Dir
            {
                get
                {
                    if (Direction == null)
                        return 0;

                    switch (Direction.Text)
                    {
                        case ">":
                            return 1;
                        case "<":
                            return -1;
                        default:
                            throw new MemberAccessException("表达式错误");
                    }
                }
            }

            /// <inheritdoc />
            public void Accept(IQueryVisitor<IDetailQueryAtom> visitor) => visitor.Visit(this);

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
        }

        public partial class DetailsContext : IQueryAry<IDetailQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator
            {
                get
                {
                    if (Op == null)
                        return OperatorType.None;

                    if (details() == null)
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
            public IQueryCompunded<IDetailQueryAtom> Filter1
            {
                get
                {
                    if (details() != null)
                        return details();

                    return details1();
                }
            }

            /// <inheritdoc />
            public IQueryCompunded<IDetailQueryAtom> Filter2 => details1();

            /// <inheritdoc />
            public void Accept(IQueryVisitor<IDetailQueryAtom> visitor) => visitor.Visit(this);

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
        }

        public partial class Details1Context : IQueryAry<IDetailQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator => Op == null ? OperatorType.None : OperatorType.Intersect;

            /// <inheritdoc />
            public IQueryCompunded<IDetailQueryAtom> Filter1 => details0();

            /// <inheritdoc />
            public IQueryCompunded<IDetailQueryAtom> Filter2 => details1();

            /// <inheritdoc />
            public void Accept(IQueryVisitor<IDetailQueryAtom> visitor) => visitor.Visit(this);

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
        }

        public partial class Details0Context : IQueryAry<IDetailQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator => OperatorType.None;

            /// <inheritdoc />
            public IQueryCompunded<IDetailQueryAtom> Filter1 => detailQuery() ??
                (IQueryCompunded<IDetailQueryAtom>)details();

            /// <inheritdoc />
            public IQueryCompunded<IDetailQueryAtom> Filter2 => null;

            /// <inheritdoc />
            public void Accept(IQueryVisitor<IDetailQueryAtom> visitor) => visitor.Visit(this);

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
        }
    }
}
