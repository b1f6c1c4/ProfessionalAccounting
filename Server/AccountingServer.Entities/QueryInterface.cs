using System.Collections.Generic;

namespace AccountingServer.Entities
{
    public enum OperatorType
    {
        None,
        Identity,
        Complement,
        Union,
        Substract,
        Intersect,
    }

    // ReSharper disable once UnusedTypeParameter
    public interface IQueryCompunded<TAtom> where TAtom : class { }

    public interface IQueryAry<TAtom> : IQueryCompunded<TAtom> where TAtom : class
    {
        OperatorType Operator { get; }
        IQueryCompunded<TAtom> Filter1 { get; }
        IQueryCompunded<TAtom> Filter2 { get; }
    }

    public interface IDetailQueryAtom : IQueryCompunded<IDetailQueryAtom>
    {
        VoucherDetail Filter { get; }

        int Dir { get; }
    }

    public interface IDateRange
    {
        DateFilter Range { get; }
    }


    public interface IVoucherQueryAtom : IQueryCompunded<IVoucherQueryAtom>
    {
        bool ForAll { get; }

        Voucher VoucherFilter { get; }

        DateFilter Range { get; }

        IQueryCompunded<IDetailQueryAtom> DetailFilter { get; }
    }

    public interface IEmit
    {
        IQueryCompunded<IDetailQueryAtom> DetailFilter { get; }
    }

    public interface IVoucherDetailQuery
    {
        IQueryCompunded<IVoucherQueryAtom> VoucherQuery { get; }

        IEmit DetailEmitFilter { get; }
    }

    public enum AggregationType
    {
        None,
        ChangedDay,
        EveryDay,
    }

    public interface ISubtotal
    {
        bool NonZero { get; }
        IReadOnlyList<SubtotalLevel> Levels { get; }
        AggregationType AggrType { get; }
        IDateRange EveryDayRange { get; }
    }

    public interface IGroupedQuery
    {
        IVoucherDetailQuery VoucherEmitQuery { get; }

        ISubtotal Subtotal { get; }
    }

    public interface IDistributedQueryAtom : IQueryCompunded<IDistributedQueryAtom>
    {
        IDistributed Filter { get; }
    }
}
