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
                throw new ArgumentException("参与运算的检索式个数过少", nameof(queries));

            Filter1 = queries[0];
            switch (op)
            {
                case OperatorType.Identity:
                case OperatorType.Complement:
                    break;
                case OperatorType.Substract:
                    Filter2 = queries[1];
                    break;
                case OperatorType.Union:
                case OperatorType.Intersect:
                    if (queries.Count == 1)
                        Operator = OperatorType.Identity;
                    else if (queries.Count == 2)
                        Filter2 = queries[1];
                    else
                        Filter2 = new QueryAryBase<TAtom>(op, queries.Skip(1).ToList());
                    break;
                default:
                    throw new ArgumentException("运算类型未知", nameof(op));
            }
        }

        public OperatorType Operator { get; }
        public IQueryCompunded<TAtom> Filter1 { get; }
        public IQueryCompunded<TAtom> Filter2 { get; }
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
}
