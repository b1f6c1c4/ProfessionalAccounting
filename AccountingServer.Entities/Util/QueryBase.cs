using System;
using System.Collections.Generic;

namespace AccountingServer.Entities.Util
{
    public sealed class DetailQueryUnconstrained : IDetailQueryAtom
    {
        public static IDetailQueryAtom Instance { get; } = new DetailQueryUnconstrained();

        private DetailQueryUnconstrained() { }

        public VoucherDetail Filter { get; } = new VoucherDetail();

        public int Dir => 0;

        public void Accept(IQueryVisitor<IDetailQueryAtom> visitor) => visitor.Visit(this);
        public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
    }

    public sealed class VoucherQueryUnconstrained : IVoucherQueryAtom
    {
        public static IVoucherQueryAtom Instance { get; } = new VoucherQueryUnconstrained();

        private VoucherQueryUnconstrained() { }

        public bool ForAll => true;

        public Voucher VoucherFilter { get; } = new Voucher();

        public DateFilter Range => DateFilter.Unconstrained;

        public IQueryCompunded<IDetailQueryAtom> DetailFilter => DetailQueryUnconstrained.Instance;

        public void Accept(IQueryVisitor<IVoucherQueryAtom> visitor) => visitor.Visit(this);
        public T Accept<T>(IQueryVisitor<IVoucherQueryAtom, T> visitor) => visitor.Visit(this);
    }

    public sealed class DistributedQueryUnconstrained : IDistributedQueryAtom
    {
        public static IDistributedQueryAtom Instance { get; } = new DistributedQueryUnconstrained();

        private DistributedQueryUnconstrained() { }

        private sealed class TheFilter : IDistributed
        {
            public Guid? ID => null;

            public string Name => null;

            public DateTime? Date => null;

            public double? Value => null;

            public string Remark => null;

            public IEnumerable<IDistributedItem> TheSchedule => null;
        }

        public IDistributed Filter { get; } = new TheFilter();

        public void Accept(IQueryVisitor<IDistributedQueryAtom> visitor) => visitor.Visit(this);
        public T Accept<T>(IQueryVisitor<IDistributedQueryAtom, T> visitor) => visitor.Visit(this);
    }
}
