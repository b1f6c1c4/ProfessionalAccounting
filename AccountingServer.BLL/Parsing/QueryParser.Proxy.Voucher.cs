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

namespace AccountingServer.BLL.Parsing;

internal partial class QueryParser
{
    public partial class VoucherQueryContext : IClientDependable, IVoucherQueryAtom
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
        public DateFilter Range
            => range().Assign(Client)?.Range ?? DateFilter.Unconstrained;

        /// <inheritdoc />
        public IQueryCompounded<IDetailQueryAtom> DetailFilter
            => details().Assign(Client);

        /// <inheritdoc />
        public bool IsDangerous()
            => VoucherFilter.IsDangerous() && Range.IsDangerous() && DetailFilter.IsDangerous();

        /// <inheritdoc />
        public T Accept<T>(IQueryVisitor<IVoucherQueryAtom, T> visitor) => visitor.Visit(this);

        public Client Client { private get; set; }
    }

    public partial class VouchersContext : IClientDependable, IQueryAry<IVoucherQueryAtom>
    {
        /// <inheritdoc />
        public OperatorType Operator => OperatorType.None;

        /// <inheritdoc />
        public IQueryCompounded<IVoucherQueryAtom> Filter1
            => (IQueryCompounded<IVoucherQueryAtom>)voucherQuery().Assign(Client) ?? vouchers2().Assign(Client);

        /// <inheritdoc />
        public IQueryCompounded<IVoucherQueryAtom> Filter2 => throw new MemberAccessException("表达式错误");

        /// <inheritdoc />
        public bool IsDangerous() => Filter1.IsDangerous();

        /// <inheritdoc />
        public T Accept<T>(IQueryVisitor<IVoucherQueryAtom, T> visitor) => visitor.Visit(this);

        public Client Client { private get; set; }
    }

    public partial class Vouchers2Context : IClientDependable, IQueryAry<IVoucherQueryAtom>
    {
        /// <inheritdoc />
        public OperatorType Operator
        {
            get
            {
                if (Op == null)
                    return OperatorType.None;

                if (vouchers2() == null)
                    return Op.Text switch
                        {
                            "+" => OperatorType.Identity,
                            "-" => OperatorType.Complement,
                            _ => throw new MemberAccessException("表达式错误"),
                        };

                return Op.Text switch
                    {
                        "+" => OperatorType.Union,
                        "-" => OperatorType.Subtract,
                        _ => throw new MemberAccessException("表达式错误"),
                    };
            }
        }

        /// <inheritdoc />
        public IQueryCompounded<IVoucherQueryAtom> Filter1
            => (IQueryCompounded<IVoucherQueryAtom>)vouchers2().Assign(Client) ?? vouchers1().Assign(Client);

        /// <inheritdoc />
        public IQueryCompounded<IVoucherQueryAtom> Filter2
            => vouchers1().Assign(Client);

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
        public T Accept<T>(IQueryVisitor<IVoucherQueryAtom, T> visitor) => visitor.Visit(this);

        public Client Client { private get; set; }
    }

    public partial class Vouchers1Context : IClientDependable, IQueryAry<IVoucherQueryAtom>
    {
        /// <inheritdoc />
        public OperatorType Operator => Op == null ? OperatorType.None : OperatorType.Intersect;

        /// <inheritdoc />
        public IQueryCompounded<IVoucherQueryAtom> Filter1
            => vouchers0().Assign(Client);

        /// <inheritdoc />
        public IQueryCompounded<IVoucherQueryAtom> Filter2
            => vouchers1().Assign(Client);

        /// <inheritdoc />
        public bool IsDangerous() => Filter1.IsDangerous() && (Filter2?.IsDangerous() ?? true);

        /// <inheritdoc />
        public T Accept<T>(IQueryVisitor<IVoucherQueryAtom, T> visitor) => visitor.Visit(this);

        public Client Client { private get; set; }
    }

    public partial class Vouchers0Context : IClientDependable, IQueryAry<IVoucherQueryAtom>
    {
        /// <inheritdoc />
        public OperatorType Operator => OperatorType.None;

        /// <inheritdoc />
        public IQueryCompounded<IVoucherQueryAtom> Filter1
            => (IQueryCompounded<IVoucherQueryAtom>)voucherQuery().Assign(Client) ?? vouchers2().Assign(Client);

        /// <inheritdoc />
        public IQueryCompounded<IVoucherQueryAtom> Filter2 => null;

        /// <inheritdoc />
        public bool IsDangerous() => Filter1.IsDangerous();

        /// <inheritdoc />
        public T Accept<T>(IQueryVisitor<IVoucherQueryAtom, T> visitor) => visitor.Visit(this);

        public Client Client { private get; set; }
    }

    public partial class EmitContext : IClientDependable, IEmit
    {
        /// <inheritdoc />
        public IQueryCompounded<IDetailQueryAtom> DetailFilter
            => details().Assign(Client);

        public Client Client { private get; set; }
    }

    public partial class VoucherDetailQueryContext : IClientDependable, IVoucherDetailQuery
    {
        /// <inheritdoc />
        public IQueryCompounded<IVoucherQueryAtom> VoucherQuery
            => (IQueryCompounded<IVoucherQueryAtom>)voucherQuery().Assign(Client) ?? vouchers().Assign(Client);

        /// <inheritdoc />
        public IEmit DetailEmitFilter => emit().Assign(Client);

        public Client Client { private get; set; }
    }
}
