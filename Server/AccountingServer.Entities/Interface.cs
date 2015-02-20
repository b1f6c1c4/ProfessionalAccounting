using System.Collections.Generic;

namespace AccountingServer.Entities
{
    public enum UnaryOperatorType
    {
        Identity,
        Complement,
    }

    public enum BinaryOperatorType
    {
        Union,
        Substract,
        Interect,
    }

    public interface IDetailQueryCompounded { }

    public interface IDetailQueryAtom : IDetailQueryCompounded
    {
        VoucherDetail Filter { get; }

        int Dir { get; }
    }

    public interface IDetailQueryUnary : IDetailQueryCompounded
    {
        UnaryOperatorType Operator { get; }

        IDetailQueryCompounded Filter1 { get; }
    }

    public interface IDetailQueryBinary : IDetailQueryCompounded
    {
        BinaryOperatorType Operator { get; }
        IDetailQueryCompounded Filter1 { get; }
        IDetailQueryCompounded Filter2 { get; }
    }


    public interface IDateRange
    {
        DateFilter Range { get; }
    }

    public interface IVoucherQueryCompounded { }

    public interface IVoucherQueryAtom : IVoucherQueryCompounded
    {
        bool ForAll { get; }

        Voucher VoucherFilter { get; }

        DateFilter Range { get; }

        IDetailQueryCompounded DetailFilter { get; }
    }

    public interface IVoucherQueryUnary : IVoucherQueryCompounded
    {
        UnaryOperatorType Operator { get; }

        IVoucherQueryCompounded Filter1 { get; }
    }

    public interface IVoucherQueryBinary : IVoucherQueryCompounded
    {
        BinaryOperatorType Operator { get; }
        IVoucherQueryCompounded Filter1 { get; }
        IVoucherQueryCompounded Filter2 { get; }
    }

    public interface IEmit
    {
        IDetailQueryCompounded DetailFilter { get; }
    }

    public interface IVoucherDetailQuery
    {
        IVoucherQueryCompounded VoucherQuery { get; }

        IEmit DetailEmitFilter { get; }
    }

    public enum AggregationType
    {
        None,
        ChangedDay,
        EveryDay,
        ExtendedEveryDay,
    }

    public interface ISubtotal
    {
        bool NonZero { get; }
        IList<SubtotalLevel> Levels { get; }
        AggregationType AggrType { get; }
    }

    public interface IGroupedQuery
    {
        IVoucherDetailQuery VoucherEmitQuery { get; }

        ISubtotal Subtotal { get; }
    }
}
