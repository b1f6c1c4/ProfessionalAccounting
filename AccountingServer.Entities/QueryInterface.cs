using System;
using System.Collections.Generic;

namespace AccountingServer.Entities
{
    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once ConvertToStaticClass
    public sealed class Nothing
    {
        private Nothing() { }

        public static Nothing AtAll => null;
    }

    /// <summary>
    ///     运算符类型
    /// </summary>
    public enum OperatorType
    {
        /// <summary>
        ///     无运算
        /// </summary>
        None,

        /// <summary>
        ///     与第一个检索式相同
        /// </summary>
        Identity,

        /// <summary>
        ///     与第一个检索式互补
        /// </summary>
        Complement,

        /// <summary>
        ///     为两者并集
        /// </summary>
        Union,

        /// <summary>
        ///     为两者差集
        /// </summary>
        Subtract,

        /// <summary>
        ///     为两者交集
        /// </summary>
        Intersect,
    }

    /// <summary>
    ///     科目类型
    /// </summary>
    public enum TitleKind
    {
        /// <summary>
        ///     资产
        /// </summary>
        Asset,

        /// <summary>
        ///     负债
        /// </summary>
        Liability,

        /// <summary>
        ///     所有者权益
        /// </summary>
        Equity,

        /// <summary>
        ///     收入
        /// </summary>
        Revenue,

        /// <summary>
        ///     费用
        /// </summary>
        Expense,
    }

    /// <summary>
    ///     一般检索式
    /// </summary>
    /// <typeparam name="TAtom">原子检索式的类型</typeparam>
    public interface IQueryCompounded<TAtom> where TAtom : class
    {
        /// <summary>
        ///     是否包含弱检索式
        /// </summary>
        /// <returns>若包含则为<c>true</c>，否则为<c>false</c></returns>
        bool IsDangerous();

        /// <summary>
        ///     二次分配
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="visitor">访问者</param>
        /// <returns>访问者返回值</returns>
        T Accept<T>(IQueryVisitor<TAtom, T> visitor);
    }

    /// <summary>
    ///     一般复合检索式
    /// </summary>
    /// <typeparam name="TAtom">原子检索式的类型</typeparam>
    public interface IQueryAry<TAtom> : IQueryCompounded<TAtom> where TAtom : class
    {
        /// <summary>
        ///     运算符
        /// </summary>
        OperatorType Operator { get; }

        /// <summary>
        ///     第一个检索式
        /// </summary>
        IQueryCompounded<TAtom> Filter1 { get; }

        /// <summary>
        ///     第二个检索式
        /// </summary>
        IQueryCompounded<TAtom> Filter2 { get; }
    }

    /// <summary>
    ///     一般检索式访问者
    /// </summary>
    /// <typeparam name="TAtom">原子检索式的类型</typeparam>
    /// <typeparam name="T">返回值类型</typeparam>
    public interface IQueryVisitor<TAtom, out T> where TAtom : class
    {
        T Visit(TAtom query);

        T Visit(IQueryAry<TAtom> query);
    }

    /// <summary>
    ///     原子细目检索式
    /// </summary>
    public interface IDetailQueryAtom : IQueryCompounded<IDetailQueryAtom>
    {
        /// <summary>
        ///     科目类型
        /// </summary>
        TitleKind? Kind { get; }

        /// <summary>
        ///     细目过滤器
        /// </summary>
        VoucherDetail Filter { get; }

        /// <summary>
        ///     借贷方向
        /// </summary>
        int Dir { get; }
    }

    /// <summary>
    ///     日期检索式
    /// </summary>
    public interface IDateRange
    {
        /// <summary>
        ///     日期过滤器
        /// </summary>
        DateFilter Range { get; }
    }

    /// <summary>
    ///     原子记账凭证检索式
    /// </summary>
    public interface IVoucherQueryAtom : IQueryCompounded<IVoucherQueryAtom>
    {
        /// <summary>
        ///     对细目采用全称命题
        /// </summary>
        bool ForAll { get; }

        /// <summary>
        ///     记账凭证过滤器
        /// </summary>
        Voucher VoucherFilter { get; }

        /// <summary>
        ///     日期过滤器
        /// </summary>
        DateFilter Range { get; }

        /// <summary>
        ///     细目检索式
        /// </summary>
        IQueryCompounded<IDetailQueryAtom> DetailFilter { get; }
    }

    /// <summary>
    ///     细目映射检索式
    /// </summary>
    public interface IEmit
    {
        /// <summary>
        ///     细目检索式
        /// </summary>
        IQueryCompounded<IDetailQueryAtom> DetailFilter { get; }
    }

    /// <summary>
    ///     记账凭证细目映射检索式
    /// </summary>
    public interface IVoucherDetailQuery
    {
        /// <summary>
        ///     记账凭证检索式
        /// </summary>
        IQueryCompounded<IVoucherQueryAtom> VoucherQuery { get; }

        /// <summary>
        ///     细目映射检索式
        /// </summary>
        IEmit DetailEmitFilter { get; }
    }

    /// <summary>
    ///     汇总类型
    /// </summary>
    public enum GatheringType
    {
        /// <summary>
        ///     求和，不显示零汇总项
        /// </summary>
        NonZero,

        /// <summary>
        ///     求和，显示零汇总项
        /// </summary>
        Zero,

        /// <summary>
        ///     计数
        /// </summary>
        Count,

        /// <summary>
        ///     记账凭证计数
        /// </summary>
        VoucherCount,
    }

    /// <summary>
    ///     日期累加类型
    /// </summary>
    public enum AggregationType
    {
        /// <summary>
        ///     不累加
        /// </summary>
        None,

        /// <summary>
        ///     仅变动日累加
        /// </summary>
        ChangedDay,

        /// <summary>
        ///     每日累加
        /// </summary>
        EveryDay,
    }

    /// <summary>
    ///     分类汇总参数
    /// </summary>
    public interface ISubtotal
    {
        /// <summary>
        ///     汇总类型
        /// </summary>
        GatheringType GatherType { get; }

        /// <summary>
        ///     分类汇总层次
        /// </summary>
        IReadOnlyList<SubtotalLevel> Levels { get; }

        /// <summary>
        ///     是否进行日期累加
        /// </summary>
        AggregationType AggrType { get; }

        /// <summary>
        ///     日期累加的间隔
        /// </summary>
        SubtotalLevel AggrInterval { get; }

        /// <summary>
        ///     日期累加的范围
        /// </summary>
        IDateRange EveryDayRange { get; }

        /// <summary>
        ///     币种等值币种
        /// </summary>
        string EquivalentCurrency { get; }

        /// <summary>
        ///     币种等值日期
        /// </summary>
        DateTime? EquivalentDate { get; }
    }

    /// <summary>
    ///     分类汇总检索式
    /// </summary>
    public interface IGroupedQuery
    {
        /// <summary>
        ///     记账凭证细目映射检索式
        /// </summary>
        IVoucherDetailQuery VoucherEmitQuery { get; }

        /// <summary>
        ///     分类汇总参数
        /// </summary>
        ISubtotal Subtotal { get; }
    }

    /// <summary>
    ///     记账凭证分类汇总检索式
    /// </summary>
    public interface IVoucherGroupedQuery
    {
        /// <summary>
        ///     记账凭证检索式
        /// </summary>
        IQueryCompounded<IVoucherQueryAtom> VoucherQuery { get; }

        /// <summary>
        ///     分类汇总参数
        /// </summary>
        ISubtotal Subtotal { get; }
    }

    /// <summary>
    ///     分期检索式
    /// </summary>
    public interface IDistributedQueryAtom : IQueryCompounded<IDistributedQueryAtom>
    {
        /// <summary>
        ///     分期过滤器
        /// </summary>
        IDistributed Filter { get; }

        /// <summary>
        ///     日期过滤器
        /// </summary>
        DateFilter Range { get; }
    }
}
