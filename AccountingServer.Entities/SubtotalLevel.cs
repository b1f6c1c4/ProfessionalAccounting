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
        Year = 0b0010_0110_0000
    }


    public interface ISubtotalResult
    {
        double Fund { get; }

        IEnumerable<ISubtotalResult> Items { get; }

        void Accept(ISubtotalVisitor visitor);
        T Accept<T>(ISubtotalVisitor<T> visitor);
    }

    public interface ISubtotalRoot : ISubtotalResult { }

    public interface ISubtotalDate : ISubtotalResult
    {
        AggregationType Aggr { get; }

        SubtotalLevel Level { get; }

        DateTime? Date { get; }
    }

    public interface ISubtotalCurrency : ISubtotalResult
    {
        string Currency { get; }
    }

    public interface ISubtotalTitle : ISubtotalResult
    {
        int? Title { get; }
    }

    public interface ISubtotalSubTitle : ISubtotalResult
    {
        int? SubTitle { get; }
    }

    public interface ISubtotalContent : ISubtotalResult
    {
        string Content { get; }
    }

    public interface ISubtotalRemark : ISubtotalResult
    {
        string Remark { get; }
    }

    public interface ISubtotalVisitor
    {
        void Visit(ISubtotalRoot sub);
        void Visit(ISubtotalDate sub);
        void Visit(ISubtotalCurrency sub);
        void Visit(ISubtotalTitle sub);
        void Visit(ISubtotalSubTitle sub);
        void Visit(ISubtotalContent sub);
        void Visit(ISubtotalRemark sub);
    }

    public interface ISubtotalVisitor<out T>
    {
        T Visit(ISubtotalRoot sub);
        T Visit(ISubtotalDate sub);
        T Visit(ISubtotalCurrency sub);
        T Visit(ISubtotalTitle sub);
        T Visit(ISubtotalSubTitle sub);
        T Visit(ISubtotalContent sub);
        T Visit(ISubtotalRemark sub);
    }
}
