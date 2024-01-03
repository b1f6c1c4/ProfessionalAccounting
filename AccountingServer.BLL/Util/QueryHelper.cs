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

using AccountingServer.Entities;

namespace AccountingServer.BLL.Util;

public abstract class BinaryQueries<TAtom> : IQueryAry<TAtom> where TAtom : class
{
    protected BinaryQueries(IQueryCompounded<TAtom> filter1,
        IQueryCompounded<TAtom> filter2)
    {
        Filter1 = filter1;
        Filter2 = filter2;
    }

    public abstract OperatorType Operator { get; }

    public IQueryCompounded<TAtom> Filter1 { get; }

    public IQueryCompounded<TAtom> Filter2 { get; }

    public T Accept<T>(IQueryVisitor<TAtom, T> visitor) => visitor.Visit(this);
}

public sealed class IntersectQueries<TAtom> : BinaryQueries<TAtom> where TAtom : class
{
    public IntersectQueries(IQueryCompounded<TAtom> filter1,
        IQueryCompounded<TAtom> filter2) : base(filter1, filter2) { }

    public override OperatorType Operator => OperatorType.Intersect;
}

public sealed class UnionQueries<TAtom> : BinaryQueries<TAtom> where TAtom : class
{
    public UnionQueries(IQueryCompounded<TAtom> filter1,
        IQueryCompounded<TAtom> filter2) : base(filter1, filter2) { }

    public override OperatorType Operator => OperatorType.Union;
}

public sealed class SimpleDetailQuery : IDetailQueryAtom
{
    public TitleKind? Kind { get; init; }
    public VoucherDetail Filter { get; init; }
    public bool IsFundBidirectional { get; init; }
    public int Dir { get; init; }
    public string ContentPrefix { get; init; }
    public string RemarkPrefix { get; init; }
    public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
}

public sealed class SimpleVoucherQuery : IVoucherQueryAtom
{
    public bool ForAll { get; init; } = false;
    public Voucher VoucherFilter { get; init; } = null;
    public DateFilter Range { get; init; } = DateFilter.Unconstrained;
    public IQueryCompounded<IDetailQueryAtom> DetailFilter { get; init; } = null;
    public T Accept<T>(IQueryVisitor<IVoucherQueryAtom, T> visitor) => visitor.Visit(this);
}
