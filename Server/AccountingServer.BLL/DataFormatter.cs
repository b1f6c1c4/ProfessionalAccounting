using System;
using System.Globalization;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     格式化数据
    /// </summary>
    public static class DataFormatter
    {
        /// <summary>
        ///     格式化金额，用空格代替末尾的零
        /// </summary>
        /// <param name="value">金额</param>
        /// <returns>格式化后的金额</returns>
        public static string AsCurrency(this double value)
        {
            var s = String.Format("{0:0.0000}", value);
            return "￥" + s.TrimEnd('0').CPadRight(s.Length);
        }

        /// <summary>
        ///     格式化金额，用空格代替末尾的零
        /// </summary>
        /// <param name="value">金额</param>
        /// <returns>格式化后的金额</returns>
        public static string AsCurrency(this double? value)
        {
            return value.HasValue ? AsCurrency(value.Value) : String.Empty;
        }

        /// <summary>
        ///     格式化一级科目编号
        /// </summary>
        /// <param name="value">一级科目编号</param>
        /// <returns>格式化后的编号</returns>
        public static string AsTitle(this int value)
        {
            return String.Format("{0:0000}", value);
        }

        /// <summary>
        ///     格式化一级科目编号
        /// </summary>
        /// <param name="value">一级科目编号</param>
        /// <returns>格式化后的编号</returns>
        public static string AsTitle(this int? value)
        {
            return value.HasValue ? AsTitle(value.Value) : String.Empty;
        }

        /// <summary>
        ///     格式化二级科目编号
        /// </summary>
        /// <param name="value">二级科目编号</param>
        /// <returns>格式化后的编号</returns>
        public static string AsSubTitle(this int value)
        {
            return String.Format("{0:00}", value);
        }

        /// <summary>
        ///     格式化二级科目编号
        /// </summary>
        /// <param name="value">二级科目编号</param>
        /// <returns>格式化后的编号</returns>
        public static string AsSubTitle(this int? value)
        {
            return value.HasValue ? AsSubTitle(value.Value) : String.Empty;
        }


        /// <summary>
        ///     格式化日期
        /// </summary>
        /// <param name="value">日期</param>
        /// <returns>格式化后的日期</returns>
        public static string AsDate(this DateTime value)
        {
            return String.Format("{0:yyyyMMdd}", value);
        }

        /// <summary>
        ///     格式化日期
        /// </summary>
        /// <param name="value">日期</param>
        /// <returns>格式化后的日期</returns>
        public static string AsDate(this DateTime? value)
        {
            return value.HasValue ? AsDate(value.Value) : "[null]";
        }

        /// <summary>
        ///     格式化日期
        /// </summary>
        /// <param name="value">日期</param>
        /// <param name="level">分类层次</param>
        /// <returns>格式化后的日期</returns>
        public static string AsDate(this DateTime? value, SubtotalLevel level)
        {
            if (!value.HasValue)
                return "[null]";

            switch (level)
            {
                case SubtotalLevel.Day:
                case SubtotalLevel.Week:
                    return value.AsDate();
                case SubtotalLevel.Month:
                    return String.Format("@{0:D4}{1:D2}", value.Value.Year, value.Value.Month);
                case SubtotalLevel.FinancialMonth:
                    return String.Format("{0:D4}{1:D2}", value.Value.Year, value.Value.Month);
                case SubtotalLevel.BillingMonth:
                    return String.Format("#{0:D4}{1:D2}", value.Value.Year, value.Value.Month);
                case SubtotalLevel.Year:
                    return String.Format("{0:D4}", value.Value.Year);
                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        ///     解析日期
        /// </summary>
        /// <param name="value">格式化后的日期</param>
        /// <returns>日期</returns>
        public static DateTime? AsDate(this string value)
        {
            DateTime val;
            if (DateTime.TryParseExact(value, "yyyyMMdd", null, DateTimeStyles.AllowWhiteSpaces, out val))
                return val;
            return null;
        }

        /// <summary>
        ///     解析金额
        /// </summary>
        /// <param name="value">格式化后的金额</param>
        /// <returns>金额</returns>
        public static double? AsCurrency(this string value)
        {
            double val;
            if (double.TryParse(value, out val))
                return val;
            return null;
        }

        /// <summary>
        ///     解析科目编号
        /// </summary>
        /// <param name="value">格式化后的编号</param>
        /// <returns>编号</returns>
        public static int? AsTitleOrSubTitle(this string value)
        {
            int val;
            if (Int32.TryParse(value, out val))
                return val;
            return null;
        }
    }
}
