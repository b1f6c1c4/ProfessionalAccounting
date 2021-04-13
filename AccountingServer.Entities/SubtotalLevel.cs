/* Copyright (C) 2020-2021 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

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
        ///     子项
        /// </summary>
        IEnumerable<ISubtotalResult> Items { get; }

        /// <summary>
        ///     二次分配
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="visitor">访问者</param>
        /// <returns>访问者返回值</returns>
        T Accept<T>(ISubtotalVisitor<T> visitor);
    }

    public interface ISubtotalRoot : ISubtotalResult { }

    public interface ISubtotalDate : ISubtotalResult
    {
        SubtotalLevel Level { get; }

        DateTime? Date { get; }
    }

    public interface ISubtotalUser : ISubtotalResult
    {
        string User { get; }
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

    /// <summary>
    ///     分类汇总结果访问者
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    public interface ISubtotalVisitor<out T>
    {
        T Visit(ISubtotalRoot sub);
        T Visit(ISubtotalDate sub);
        T Visit(ISubtotalUser sub);
        T Visit(ISubtotalCurrency sub);
        T Visit(ISubtotalTitle sub);
        T Visit(ISubtotalSubTitle sub);
        T Visit(ISubtotalContent sub);
        T Visit(ISubtotalRemark sub);
    }
}
