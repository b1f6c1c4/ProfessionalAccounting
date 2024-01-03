/* Copyright (C) 2020-2024 b1f6c1c4
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

namespace AccountingServer.BLL.Parsing;

internal partial class QueryParser
{
    public partial class TitleContext : ITitle
    {
        public int? Title
        {
            get
            {
                if (DetailTitle() != null)
                    return int.Parse(DetailTitle().GetText().TrimStart('T'));

                if (DetailTitleSubTitle() != null)
                    return int.Parse(DetailTitleSubTitle().GetText().TrimStart('T')) / 100;

                if (DetailSubTitle() != null)
                    return null;

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

                if (DetailSubTitle() != null)
                    return int.Parse(DetailSubTitle().GetText().TrimStart('T'));

                throw new MemberAccessException("表达式错误");
            }
        }
    }

    public partial class DetailQueryContext : IClientDependable, IDetailQueryAtom
    {
        private string ContentText => token()?.GetPureText();

        private string RemarkText => DoubleQuotedString()?.GetText().Dequotation();

        public Client Client { private get; set; }

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
                        User = (UserSpec()?.GetText()).ParseUserSpec(Client ?? new()), // workaround IsDangerous
                        Currency = VoucherCurrency()?.GetText().ParseCurrency(),
                        Title = t?.Title,
                        SubTitle = t?.SubTitle,
                        Content = token()?.GetPureText(),
                        Remark = DoubleQuotedString()?.GetText().Dequotation(),
                    };

                var (cEtc, rEtc) = DecideEtc();
                if (cEtc)
                    filter.Content = null;
                if (rEtc)
                    filter.Remark = null;

                if (Floating() != null)
                {
                    var f = Floating().GetText()[1..];
                    filter.Fund = double.Parse(f);
                }

                return filter;
            }
        }

        /// <inheritdoc />
        public bool IsFundBidirectional
            => Floating()?.GetText()?[1] switch
                {
                    null => false,
                    '+' => false,
                    '-' => false,
                    _ => true,
                };

        /// <inheritdoc />
        public int Dir
            => Direction()?.GetText() switch
                {
                    null => 0,
                    ">" => 1,
                    "<" => -1,
                    _ => throw new MemberAccessException("表达式错误"),
                };

        public string ContentPrefix
            => DecideEtc().Item1 ? ContentText : null;

        public string RemarkPrefix
            => DecideEtc().Item2 ? RemarkText : null;

        /// <inheritdoc />
        public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);

        private (bool, bool) DecideEtc()
        {
            switch (Etc().Length)
            {
                case 0:
                    return (false, false);
                case 2:
                    return (true, true);
            }

            if (ContentText == null)
                return (false, true);
            if (RemarkText == null)
                return (true, false);
            return Etc(0).SourceInterval.StartsBeforeDisjoint(DoubleQuotedString().SourceInterval)
                ? (true, false)
                : (false, true);
        }
    }

    public partial class DetailsContext : IClientDependable, IQueryAry<IDetailQueryAtom>
    {
        public Client Client { private get; set; }

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
            => (IQueryCompounded<IDetailQueryAtom>)details().Assign(Client) ?? details1().Assign(Client);

        /// <inheritdoc />
        public IQueryCompounded<IDetailQueryAtom> Filter2
            => details1().Assign(Client);

        /// <inheritdoc />
        public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
    }

    public partial class Details1Context : IClientDependable, IQueryAry<IDetailQueryAtom>
    {
        public Client Client { private get; set; }

        /// <inheritdoc />
        public OperatorType Operator => Op == null ? OperatorType.None : OperatorType.Intersect;

        /// <inheritdoc />
        public IQueryCompounded<IDetailQueryAtom> Filter1
            => details0().Assign(Client);

        /// <inheritdoc />
        public IQueryCompounded<IDetailQueryAtom> Filter2
            => details1().Assign(Client);

        /// <inheritdoc />
        public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
    }

    public partial class Details0Context : IClientDependable, IQueryAry<IDetailQueryAtom>
    {
        public Client Client { private get; set; }

        /// <inheritdoc />
        public OperatorType Operator => OperatorType.None;

        /// <inheritdoc />
        public IQueryCompounded<IDetailQueryAtom> Filter1
            => (IQueryCompounded<IDetailQueryAtom>)detailQuery().Assign(Client) ?? details().Assign(Client);

        /// <inheritdoc />
        public IQueryCompounded<IDetailQueryAtom> Filter2 => null;

        /// <inheritdoc />
        public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
    }

    public partial class TokenContext
    {
        public string GetPureText()
            => SingleQuotedString()?.GetText().Dequotation() ?? GetText();
    }
}
