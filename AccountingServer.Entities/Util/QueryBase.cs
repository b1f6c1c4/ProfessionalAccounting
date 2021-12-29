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
using System.Collections.Generic;

namespace AccountingServer.Entities.Util;

public sealed class DetailQueryUnconstrained : IDetailQueryAtom
{
    private DetailQueryUnconstrained() { }
    public static IDetailQueryAtom Instance { get; } = new DetailQueryUnconstrained();

    public TitleKind? Kind => null;

    public VoucherDetail Filter { get; } = new();

    public int Dir => 0;

    public string ContentPrefix => null;

    public string RemarkPrefix => null;

    public bool IsDangerous() => true;

    public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
}

public sealed class VoucherQueryUnconstrained : IVoucherQueryAtom
{
    private VoucherQueryUnconstrained() { }
    public static IVoucherQueryAtom Instance { get; } = new VoucherQueryUnconstrained();

    public bool ForAll => true;

    public Voucher VoucherFilter { get; } = new();

    public DateFilter Range => DateFilter.Unconstrained;

    public IQueryCompounded<IDetailQueryAtom> DetailFilter => DetailQueryUnconstrained.Instance;

    public bool IsDangerous() => true;

    public T Accept<T>(IQueryVisitor<IVoucherQueryAtom, T> visitor) => visitor.Visit(this);
}

public sealed class DistributedQueryUnconstrained : IDistributedQueryAtom
{
    private DistributedQueryUnconstrained() { }
    public static IDistributedQueryAtom Instance { get; } = new DistributedQueryUnconstrained();

    public IDistributed Filter { get; } = new TheFilter();

    public DateFilter Range => DateFilter.Unconstrained;

    public bool IsDangerous() => true;

    public T Accept<T>(IQueryVisitor<IDistributedQueryAtom, T> visitor) => visitor.Visit(this);

    private sealed class TheFilter : IDistributed
    {
        public Guid? ID => null;

        public string User => null;

        public string Name => null;

        public DateTime? Date => null;

        public double? Value => null;

        public string Remark => null;

        public IEnumerable<IDistributedItem> TheSchedule => null;
    }
}
