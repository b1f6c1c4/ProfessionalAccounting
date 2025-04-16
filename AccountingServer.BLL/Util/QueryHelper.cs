/* Copyright (C) 2020-2025 b1f6c1c4
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
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

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

public sealed class SubtractQueries<TAtom> : BinaryQueries<TAtom> where TAtom : class
{
    public SubtractQueries(IQueryCompounded<TAtom> filter1,
        IQueryCompounded<TAtom> filter2) : base(filter1, filter2) { }

    public override OperatorType Operator => OperatorType.Subtract;
}

public sealed class SimpleDetailQuery : IDetailQueryAtom
{
    public TitleKind? Kind { get; init; }
    public VoucherDetail Filter { get; init; }
    public bool? IsPseudoCurrency { get; init; }
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
    public string RemarkPrefix { get; init; } = null;
    public IQueryCompounded<IDetailQueryAtom> DetailFilter { get; init; } = null;
    public T Accept<T>(IQueryVisitor<IVoucherQueryAtom, T> visitor) => visitor.Visit(this);
}

public static class QueryHelper
{
    public static IQueryCompounded<TAtom> QueryAny<TAtom>(
            this IEnumerable<IQueryCompounded<TAtom>> lst) where TAtom : class
        => !lst.Any() ? null : lst.First().QueryAny(lst.Skip(1));

    public static IQueryCompounded<TAtom> QueryAll<TAtom>(
            this IEnumerable<IQueryCompounded<TAtom>> lst) where TAtom : class
    {
        if (lst.Any())
            lst.First().QueryAll(lst.Skip(1));

        if (lst is IEnumerable<IQueryCompounded<IDetailQueryAtom>>)
            return (IQueryCompounded<TAtom>)DetailQueryUnconstrained.Instance;

        if (lst is IEnumerable<IQueryCompounded<IVoucherQueryAtom>>)
            return (IQueryCompounded<TAtom>)VoucherQueryUnconstrained.Instance;

        if (lst is IEnumerable<IQueryCompounded<IDistributedQueryAtom>>)
            return (IQueryCompounded<TAtom>)DistributedQueryUnconstrained.Instance;

        throw new NotImplementedException();
    }

    public static IQueryCompounded<TAtom> QueryAny<TAtom>(
            this IQueryCompounded<TAtom> b,
            IEnumerable<IQueryCompounded<TAtom>> lst) where TAtom : class
        => b == null ? lst.QueryAny() : lst.Aggregate(b, (q, v) => new UnionQueries<TAtom>(q, v));

    public static IQueryCompounded<TAtom> QueryAll<TAtom>(
            this IQueryCompounded<TAtom> b,
            IEnumerable<IQueryCompounded<TAtom>> lst) where TAtom : class
        => b == null ? null : lst.Aggregate(b, (q, v) => new IntersectQueries<TAtom>(q, v));

    public static IQueryCompounded<TAtom> QueryBut<TAtom>(
            this IQueryCompounded<TAtom> b,
            IEnumerable<IQueryCompounded<TAtom>> lst) where TAtom : class
        => b == null ? null : lst.Aggregate(b, (q, v) => new SubtractQueries<TAtom>(q, v));
}
