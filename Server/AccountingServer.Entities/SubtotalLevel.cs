using System;

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
        None = 0x0,

        /// <summary>
        ///     按一级科目分类
        /// </summary>
        Title = 0x1,

        /// <summary>
        ///     按二级科目分类
        /// </summary>
        SubTitle = 0x2,

        /// <summary>
        ///     按内容分类
        /// </summary>
        Content = 0x4,

        /// <summary>
        ///     按备注分类
        /// </summary>
        Remark = 0x8,

        /// <summary>
        ///     按日期分类
        /// </summary>
        Day = 0x10,

        /// <summary>
        ///     按周分类
        /// </summary>
        Week = 0x30,

        /// <summary>
        ///     按月分类
        /// </summary>
        Month = 0x70,

        /// <summary>
        ///     按财月分类
        /// </summary>
        FinancialMonth = 0xB0,

        /// <summary>
        ///     按账单月分类
        /// </summary>
        BillingMonth = 0xF0,

        /// <summary>
        ///     按年分类
        /// </summary>
        Year = 0x130
    }
}
