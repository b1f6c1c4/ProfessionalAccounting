using System.Collections.Generic;

namespace AccountingServer.Entities
{
    public interface IDetailQueryCompounded { }

    public interface IDetailQueryAtom : IDetailQueryCompounded
    {
        VoucherDetail Filter { get; }

        int Dir { get; }
    }

    public enum UnaryOperatorType
    {
        Complement,
    }

    public interface IDetailQueryUnary : IDetailQueryCompounded
    {
        UnaryOperatorType Operator { get; }

        IDetailQueryCompounded Filter1 { get; }
    }

    public enum BinaryOperatorType
    {
        Union,
        Substract,
        Interect,
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

    public interface IVoucherQuery : IDateRange
    {
        Voucher VoucherFilter { get; }

        IDetailQueryCompounded DetailFilter { get; }
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
        bool All { get; }
        IList<SubtotalLevel> Levels { get; }
        AggregationType AggrType { get; }
    }

    public interface IGroupingQuery : IVoucherQuery, ISubtotal { }
}
