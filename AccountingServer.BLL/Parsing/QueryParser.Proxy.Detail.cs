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

                    if (details().Length == 1)
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
                        case "*":
                            return OperatorType.Intersect;
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
                    if (detailQuery() != null)
                        return detailQuery();

                    return details(0);
                }
            }

            /// <inheritdoc />
            public IQueryCompunded<IDetailQueryAtom> Filter2 => details(1);
        }
    }
}
