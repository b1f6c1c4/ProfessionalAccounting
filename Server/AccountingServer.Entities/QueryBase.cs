using System;
using System.Collections.Generic;
using System.Linq;

namespace AccountingServer.Entities
{
    public class DetailQueryAtomBase : IDetailQueryAtom
    {
        public DetailQueryAtomBase(VoucherDetail filter, int dir = 0)
        {
            Filter = filter;
            Dir = dir;
        }

        public VoucherDetail Filter { get; set; }
        public int Dir { get; set; }
    }

    public class QueryAryBase<TAtom> : IQueryAry<TAtom> where TAtom : class
    {
        public QueryAryBase(OperatorType op, IList<IQueryCompunded<TAtom>> queries)
        {
            Operator = op;
            if (queries.Count == 0)
                throw new InvalidOperationException();
            if (queries.Count == 1)
                Filter1 = queries[0];
            if (queries.Count == 2)
            {
                Filter1 = queries[0];
                Filter2 = queries[1];
            }
            switch (op)
            {
                case OperatorType.Union:
                case OperatorType.Intersect:
                    Operator = op;
                    Filter1 = queries[0];
                    Filter2 = new QueryAryBase<TAtom>(op, queries.Skip(1).ToList());
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        public OperatorType Operator { get; private set; }
        public IQueryCompunded<TAtom> Filter1 { get; private set; }
        public IQueryCompunded<TAtom> Filter2 { get; private set; }
    }

    public class DetailQueryAryBase : QueryAryBase<IDetailQueryAtom>
    {
        public DetailQueryAryBase(OperatorType op, IList<IQueryCompunded<IDetailQueryAtom>> queries) : base(op, queries) { }

        public DetailQueryAryBase(IEnumerable<VoucherDetail> filters, bool useAnd, int dir = 0)
            : this(
                useAnd ? OperatorType.Intersect : OperatorType.Union,
                filters.Select(filter => new DetailQueryAtomBase(filter, dir))
                       .Cast<IQueryCompunded<IDetailQueryAtom>>()
                       .ToList()) { }
    }

    public struct VoucherQueryAtomBase : IVoucherQueryAtom
    {
        public VoucherQueryAtomBase(Voucher vfilter = null, VoucherDetail filter = null, DateFilter? rng = null,
                                    int dir = 0,
                                    bool forAll = false)
            : this()
        {
            VoucherFilter = vfilter;
            ForAll = forAll;
            Range = rng ?? DateFilter.Unconstrained;
            DetailFilter = new DetailQueryAtomBase(filter, dir);
        }

        public VoucherQueryAtomBase(Voucher vfilter = null, IEnumerable<VoucherDetail> filters = null,
                                    DateFilter? rng = null, bool useAnd = false, int dir = 0,
                                    bool forAll = false)
            : this()
        {
            VoucherFilter = vfilter;
            ForAll = forAll;
            Range = rng ?? DateFilter.Unconstrained;
            DetailFilter = new DetailQueryAryBase(filters, useAnd, dir);
        }

        public bool ForAll { get; set; }
        public Voucher VoucherFilter { get; set; }
        public DateFilter Range { get; set; }
        public IQueryCompunded<IDetailQueryAtom> DetailFilter { get; set; }
    }

    public class VoucherQueryAryBase : QueryAryBase<IVoucherQueryAtom>
    {
        public VoucherQueryAryBase(OperatorType op, IList<IQueryCompunded<IVoucherQueryAtom>> queries)
            : base(op, queries) { }
    }

    public struct VoucherDetailQueryBase : IVoucherDetailQuery
    {
        public IQueryCompunded<IVoucherQueryAtom> VoucherQuery { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public IEmit DetailEmitFilter { get; set; }
    }

    // ReSharper disable once UnusedMember.Global
    public struct EmitBase : IEmit
    {
        public IQueryCompunded<IDetailQueryAtom> DetailFilter { get; set; }
    }

    public struct SubtotalBase : ISubtotal
    {
        public bool NonZero { get; set; }
        public IReadOnlyList<SubtotalLevel> Levels { get; set; }
        public AggregationType AggrType { get; set; }
        public IDateRange EveryDayRange { get; set; }
    }

    public struct GroupedQueryBase : IGroupedQuery
    {
        public GroupedQueryBase(Voucher vfilter = null, VoucherDetail filter = null,
                                DateFilter? rng = null, int dir = 0,
                                bool forAll = false, ISubtotal subtotal = null)
            : this()
        {
            Subtotal = subtotal ?? new SubtotalBase { Levels = new SubtotalLevel[0], NonZero = false };
            var d = new DetailQueryAtomBase(filter, dir);
            var v = new VoucherQueryAtomBase
                        {
                            VoucherFilter = vfilter,
                            DetailFilter = d,
                            ForAll = forAll,
                            Range = rng ?? DateFilter.Unconstrained
                        };
            VoucherEmitQuery = new VoucherDetailQueryBase { VoucherQuery = v };
        }

        // ReSharper disable once UnusedMember.Global
        public GroupedQueryBase(Voucher vfilter = null, IEnumerable<VoucherDetail> filters = null,
                                DateFilter? rng = null, bool useAnd = false, int dir = 0,
                                bool forAll = false, ISubtotal subtotal = null)
            : this()
        {
            Subtotal = subtotal ?? new SubtotalBase { Levels = new SubtotalLevel[0], NonZero = false };
            var d = new DetailQueryAryBase(filters, useAnd, dir);
            var v = new VoucherQueryAtomBase
                        {
                            VoucherFilter = vfilter,
                            DetailFilter = d,
                            ForAll = forAll,
                            Range = rng ?? DateFilter.Unconstrained
                        };
            VoucherEmitQuery = new VoucherDetailQueryBase { VoucherQuery = v };
        }

        public IVoucherDetailQuery VoucherEmitQuery { get; set; }
        public ISubtotal Subtotal { get; set; }
    }
}
