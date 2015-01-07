using System;
using System.Collections.Generic;

namespace AccountingServer.Entities
{
    /// <summary>
    ///     余额表项目
    /// </summary>
    public class Balance
    {
        /// <summary>
        ///     日期
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        ///     一级科目编号
        /// </summary>
        public int? Title { get; set; }

        /// <summary>
        ///     二级科目编号
        /// </summary>
        public int? SubTitle { get; set; }

        /// <summary>
        ///     内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        ///     余额
        /// </summary>
        public double Fund { get; set; }
    }

    public class BalanceEqualityComparer : EqualityComparer<Balance>
    {
        public override bool Equals(Balance x, Balance y)
        {
            return x.Title == y.Title && x.SubTitle == y.SubTitle && x.Content == y.Content;
        }

        public override int GetHashCode(Balance obj)
        {
            var t = obj.Title ?? Int32.MinValue;
            var s = obj.SubTitle ?? Int32.MaxValue;
            var c = obj.Content == null ? Int32.MinValue : obj.Content.GetHashCode();
            return t ^ (s << 3) ^ c;
        }
    }

    public class BalanceComparer : Comparer<Balance>
    {
        /// <summary>
        ///     比较两日期（可以为无日期）的早晚
        /// </summary>
        /// <param name="b1Date">第一个日期</param>
        /// <param name="b2Date">第二个日期</param>
        /// <returns>相等为0，第一个早为-1，第二个早为1（无日期按无穷长时间以前考虑）</returns>
        public static int CompareDate(DateTime? b1Date, DateTime? b2Date)
        {
            if (b1Date.HasValue &&
                b2Date.HasValue)
                return b1Date.Value.CompareTo(b2Date.Value);
            if (b1Date.HasValue)
                return 1;
            if (b2Date.HasValue)
                return -1;
            return 0;
        }

        public override int Compare(Balance x, Balance y)
        {
            if (x != null &&
                y != null)
            {
                switch (CompareDate(x.Date, y.Date))
                {
                    case 1:
                        return 1;
                    case -1:
                        return -1;
                }

                if (x.Title.HasValue &&
                    y.Title.HasValue)
                {
                    if (x.Title < y.Title)
                        return -1;
                    if (x.Title > y.Title)
                        return 1;
                }
                else if (x.Title.HasValue)
                    return 1;
                else if (y.Title.HasValue)
                    return -1;

                if (x.SubTitle.HasValue &&
                    y.SubTitle.HasValue)
                {
                    if (x.SubTitle < y.SubTitle)
                        return -1;
                    if (x.SubTitle > y.SubTitle)
                        return 1;
                }
                else if (x.SubTitle.HasValue)
                    return 1;
                else if (y.SubTitle.HasValue)
                    return -1;

                if (x.Content != null &&
                    y.Content != null)
                    return String.Compare(x.Content, y.Content, StringComparison.Ordinal);
                if (x.Content != null)
                    return 1;
                if (y.Content != null)
                    return -1;
            }
            if (x != null)
                return -1;
            if (y != null)
                return 1;
            return 0;
        }
    }
}
