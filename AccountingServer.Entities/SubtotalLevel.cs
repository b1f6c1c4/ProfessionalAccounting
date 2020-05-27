using System;
using System.Collections.Generic;

namespace AccountingServer.Entities
{
    /// <summary>
    ///     分类汇总层次
    /// </summary>
    [Flags]
    public enum SubtotalLevel
    {
        /// <summary>
        ///     不加分类
        /// </summary>
        None = 0b0000_0000_0000,

        /// <summary>
        ///     按一级科目分类
        /// </summary>
        Title = 0b0000_0000_0001,

        /// <summary>
        ///     按二级科目分类
        /// </summary>
        SubTitle = 0b0000_0000_0010,

        /// <summary>
        ///     按内容分类
        /// </summary>
        Content = 0b0000_0000_0100,

        /// <summary>
        ///     按备注分类
        /// </summary>
        Remark = 0b0000_0000_1000,

        /// <summary>
        ///     按币种分类
        /// </summary>
        Currency = 0b0000_0001_0000,

        /// <summary>
        ///     按用户分类
        /// </summary>
        User = 0b1000_0000_0000,

        /// <summary>
        ///     按日期分类
        /// </summary>
        Day = 0b0000_0010_0000,

        /// <summary>
        ///     按周分类
        /// </summary>
        Week = 0b0000_0110_0000,

        /// <summary>
        ///     按月分类
        /// </summary>
        Month = 0b0000_1110_0000,

        /// <summary>
        ///     按年分类
        /// </summary>
        Year = 0b0010_0110_0000,

        /// <summary>
        ///     分类汇总普通部分
        /// </summary>
        Subtotal = 0b0111_1111_1111_1111_1111_1111_1111_1111,

        /// <summary>
        ///     分类汇总特殊部分
        /// </summary>
        Flags = unchecked((int)0b1000_0000_0000_0000_0000_0000_0000_0000),

        /// <summary>
        ///     提取非零项
        /// </summary>
        NonZero = unchecked((int)0b1000_0000_0000_0000_0000_0000_0000_0000),
    }

    /// <summary>
    ///     分类汇总结果
    /// </summary>
    public interface ISubtotalResult
    {
        /// <summary>
        ///     值
        /// </summary>
        double Fund { get; }

        /// <summary>
        ///     二次分配
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="visitor">访问者</param>
        /// <returns>访问者返回值</returns>
        T Accept<T>(ISubtotalVisitor<T> visitor);
    }

    public interface ISubtotalResults { }

    public interface ISubtotalResults<out T> : IEnumerable<T>, ISubtotalResults
    {
        /// <summary>
        ///     二次分配
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="visitor">访问者</param>
        /// <returns>访问者返回值</returns>
        T Accept<T>(ISubtotalItemsVisitor<T> visitor);
    }

    public interface ISubtotalResult<out TC> : ISubtotalResult where TC : ISubtotalResult
    {
        /// <summary>
        ///     子项
        /// </summary>
        ISubtotalResults<TC> Items { get; }
    }

    public interface ISubtotalRoot<out TC> : ISubtotalResult<TC> where TC : ISubtotalResult { }

    public interface ISubtotalDate<out TC> : ISubtotalResult<TC> where TC : ISubtotalResult
    {
        SubtotalLevel Level { get; }

        DateTime? Date { get; }
    }

    public interface ISubtotalUser<out TC> : ISubtotalResult<TC> where TC : ISubtotalResult
    {
        string User { get; }
    }

    public interface ISubtotalCurrency<out TC> : ISubtotalResult<TC> where TC : ISubtotalResult
    {
        string Currency { get; }
    }

    public interface ISubtotalTitle<out TC> : ISubtotalResult<TC> where TC : ISubtotalResult
    {
        int? Title { get; }
    }

    public interface ISubtotalSubTitle<out TC> : ISubtotalResult<TC> where TC : ISubtotalResult
    {
        int? SubTitle { get; }
    }

    public interface ISubtotalContent<out TC> : ISubtotalResult<TC> where TC : ISubtotalResult
    {
        string Content { get; }
    }

    public interface ISubtotalRemark<out TC> : ISubtotalResult<TC> where TC : ISubtotalResult
    {
        string Remark { get; }
    }

    /// <summary>
    ///     分类汇总结果访问者
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    public interface ISubtotalVisitor<out T>
    {
        T Visit<TC>(ISubtotalRoot<TC> sub) where TC : ISubtotalResult;
        T Visit<TC>(ISubtotalDate<TC> sub) where TC : ISubtotalResult;
        T Visit<TC>(ISubtotalUser<TC> sub) where TC : ISubtotalResult;
        T Visit<TC>(ISubtotalCurrency<TC> sub) where TC : ISubtotalResult;
        T Visit<TC>(ISubtotalTitle<TC> sub) where TC : ISubtotalResult;
        T Visit<TC>(ISubtotalSubTitle<TC> sub) where TC : ISubtotalResult;
        T Visit<TC>(ISubtotalContent<TC> sub) where TC : ISubtotalResult;
        T Visit<TC>(ISubtotalRemark<TC> sub) where TC : ISubtotalResult;
    }

    public interface ISubtotalItemsVisitor<out T>
    {
        T Visit<TC>(IEnumerable<ISubtotalRoot<TC>> sub) where TC : ISubtotalResult;
        T Visit<TC>(IEnumerable<ISubtotalDate<TC>> sub) where TC : ISubtotalResult;
        T Visit<TC>(IEnumerable<ISubtotalUser<TC>> sub) where TC : ISubtotalResult;
        T Visit<TC>(IEnumerable<ISubtotalCurrency<TC>> sub) where TC : ISubtotalResult;
        T Visit<TC>(IEnumerable<ISubtotalTitle<TC>> sub) where TC : ISubtotalResult;
        T Visit<TC>(IEnumerable<ISubtotalSubTitle<TC>> sub) where TC : ISubtotalResult;
        T Visit<TC>(IEnumerable<ISubtotalContent<TC>> sub) where TC : ISubtotalResult;
        T Visit<TC>(IEnumerable<ISubtotalRemark<TC>> sub) where TC : ISubtotalResult;
    }
}
