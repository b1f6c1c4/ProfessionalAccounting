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
            var s = $"{value:C4}";
            return s.TrimEnd('0').CPadRight(s.Length);
        }

        /// <summary>
        ///     格式化金额，用空格代替末尾的零
        /// </summary>
        /// <param name="value">金额</param>
        /// <returns>格式化后的金额</returns>
        public static string AsCurrency(this double? value) => value.HasValue ? AsCurrency(value.Value) : string.Empty;

        /// <summary>
        ///     格式化一级科目编号
        /// </summary>
        /// <param name="value">一级科目编号</param>
        /// <returns>格式化后的编号</returns>
        public static string AsTitle(this int value) => $"{value:0000}";

        /// <summary>
        ///     格式化一级科目编号
        /// </summary>
        /// <param name="value">一级科目编号</param>
        /// <returns>格式化后的编号</returns>
        public static string AsTitle(this int? value) => value.HasValue ? AsTitle(value.Value) : string.Empty;

        /// <summary>
        ///     格式化二级科目编号
        /// </summary>
        /// <param name="value">二级科目编号</param>
        /// <returns>格式化后的编号</returns>
        public static string AsSubTitle(this int value) => $"{value:00}";

        /// <summary>
        ///     格式化二级科目编号
        /// </summary>
        /// <param name="value">二级科目编号</param>
        /// <returns>格式化后的编号</returns>
        public static string AsSubTitle(this int? value) => value.HasValue ? AsSubTitle(value.Value) : string.Empty;


        /// <summary>
        ///     格式化日期
        /// </summary>
        /// <param name="value">日期</param>
        /// <returns>格式化后的日期</returns>
        public static string AsDate(this DateTime value) => $"{value:yyyyMMdd}";

        /// <summary>
        ///     格式化日期
        /// </summary>
        /// <param name="value">日期</param>
        /// <returns>格式化后的日期</returns>
        public static string AsDate(this DateTime? value) => value.HasValue ? AsDate(value.Value) : "[null]";

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
                    return $"@{value.Value.Year:D4}{value.Value.Month:D2}";
                case SubtotalLevel.FinancialMonth:
                    return $"{value.Value.Year:D4}{value.Value.Month:D2}";
                case SubtotalLevel.BillingMonth:
                    return $"#{value.Value.Year:D4}{value.Value.Month:D2}";
                case SubtotalLevel.Year:
                    return $"{value.Value.Year:D4}";
                default:
                    throw new ArgumentException("分类层次并非基于日期", nameof(level));
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
            if (int.TryParse(value, out val))
                return val;
            return null;
        }
    }
}
