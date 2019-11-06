using System;
using System.Collections.Generic;

namespace AccountingServer.Entities.Util
{
    public sealed class DetailQueryUnconstrained : IDetailQueryAtom
    {
        private DetailQueryUnconstrained() { }
        public static IDetailQueryAtom Instance { get; } = new DetailQueryUnconstrained();

        public TitleKind? Kind => null;

        public VoucherDetail Filter { get; } = new VoucherDetail();

        public int Dir => 0;

        public bool IsDangerous() => true;

        public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
    }

    public sealed class VoucherQueryUnconstrained : IVoucherQueryAtom
    {
        private VoucherQueryUnconstrained() { }
        public static IVoucherQueryAtom Instance { get; } = new VoucherQueryUnconstrained();

        public bool ForAll => true;

        public Voucher VoucherFilter { get; } = new Voucher();

        public DateFilter Range => DateFilter.Unconstrained;

        public IQueryCompunded<IDetailQueryAtom> DetailFilter => DetailQueryUnconstrained.Instance;

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
}
