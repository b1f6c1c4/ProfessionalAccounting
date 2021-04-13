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
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

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
                        return int.Parse(DetailTitle().GetText().TrimStart('T'));

                    if (DetailTitleSubTitle() != null)
                        return int.Parse(DetailTitleSubTitle().GetText().TrimStart('T')) / 100;

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
                        return int.Parse(DetailTitleSubTitle().GetText().TrimStart('T')) % 100;

                    throw new MemberAccessException("表达式错误");
                }
            }
        }

        public partial class DetailQueryContext : IDetailQueryAtom
        {
            /// <inheritdoc />
            public TitleKind? Kind
                => TitleKind() switch
                    {
                        null => null,
                        var x => (TitleKind?)Enum.Parse(typeof(TitleKind), x.GetText()),
                    };

            /// <inheritdoc />
            public VoucherDetail Filter
            {
                get
                {
                    var t = title();
                    var filter = new VoucherDetail
                        {
                            User = (UserSpec()?.GetText()).ParseUserSpec(),
                            Currency = VoucherCurrency()?.GetText().ParseCurrency(),
                            Title = t?.Title,
                            SubTitle = t?.SubTitle,
                            Content = token()?.GetPureText(),
                            Remark = DoubleQuotedString()?.GetText().Dequotation(),
                        };

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
                => Direction()?.GetText() switch
                    {
                        null => 0,
                        ">" => 1,
                        "<" => -1,
                        _ => throw new MemberAccessException("表达式错误"),
                    };

            /// <inheritdoc />
            public bool IsDangerous() => Filter.IsDangerous();

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
        }

        public partial class DetailsContext : IQueryAry<IDetailQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator
                => Op switch
                    {
                        null => OperatorType.None,
                        { Text: var x } when details() == null => x switch
                            {
                                "+" => OperatorType.Identity,
                                "-" => OperatorType.Complement,
                                _ => throw new MemberAccessException("表达式错误"),
                            },
                        { Text: var x } => x switch
                            {
                                "+" => OperatorType.Union,
                                "-" => OperatorType.Subtract,
                                _ => throw new MemberAccessException("表达式错误"),
                            },
                    };

            /// <inheritdoc />
            public IQueryCompounded<IDetailQueryAtom> Filter1
                => (IQueryCompounded<IDetailQueryAtom>)details() ?? details1();

            /// <inheritdoc />
            public IQueryCompounded<IDetailQueryAtom> Filter2 => details1();

            /// <inheritdoc />
            public bool IsDangerous()
                => Operator switch
                    {
                        OperatorType.None => Filter1.IsDangerous(),
                        OperatorType.Identity => Filter1.IsDangerous(),
                        OperatorType.Complement => true,
                        OperatorType.Union => Filter1.IsDangerous() || Filter2.IsDangerous(),
                        OperatorType.Subtract => Filter1.IsDangerous(),
                        _ => throw new ArgumentOutOfRangeException(),
                    };

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
        }

        public partial class Details1Context : IQueryAry<IDetailQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator => Op == null ? OperatorType.None : OperatorType.Intersect;

            /// <inheritdoc />
            public IQueryCompounded<IDetailQueryAtom> Filter1 => details0();

            /// <inheritdoc />
            public IQueryCompounded<IDetailQueryAtom> Filter2 => details1();

            /// <inheritdoc />
            public bool IsDangerous() => Filter1.IsDangerous() && (Filter2?.IsDangerous() ?? true);

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
        }

        public partial class Details0Context : IQueryAry<IDetailQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator => OperatorType.None;

            /// <inheritdoc />
            public IQueryCompounded<IDetailQueryAtom> Filter1
                => detailQuery() ?? (IQueryCompounded<IDetailQueryAtom>)details();

            /// <inheritdoc />
            public IQueryCompounded<IDetailQueryAtom> Filter2 => null;

            /// <inheritdoc />
            public bool IsDangerous() => Filter1.IsDangerous();

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
        }

        public partial class TokenContext
        {
            public string GetPureText()
                => SingleQuotedString()?.GetText().Dequotation() ?? GetText();
        }
    }
}
