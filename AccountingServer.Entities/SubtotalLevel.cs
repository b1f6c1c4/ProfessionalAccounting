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
}
