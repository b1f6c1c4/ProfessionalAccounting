using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL.Util
{
    [Serializable]
    [XmlRoot("Symbols")]
    public class CurrencySymbols
    {
        [XmlElement("Symbol")] public List<CurrencySymbol> Symbols;
    }

    [Serializable]
    public class CurrencySymbol
    {
        [XmlAttribute("currency")]
        public string Currency { get; set; }

        [XmlText]
        public string Symbol { get; set; }
    }

    /// <summary>
    ///     格式化数据
    /// </summary>
    public static class DataFormatter
    {
        private static readonly Regex Reg = new Regex(@"^[A-Za-z0-9_]+(&[A-Za-z0-9_]+)*$");

        /// <summary>
        ///     记账本位币信息文档
        /// </summary>
        public static IConfigManager<CurrencySymbols> CurrencySymbols { private get; set; } =
            new ConfigManager<CurrencySymbols>("Symbol.xml");

        /// <summary>
        ///     格式化金额，用空格代替末尾的零
        /// </summary>
        /// <param name="value">金额</param>
        /// <param name="curr">币种</param>
        /// <returns>格式化后的金额</returns>
        public static string AsCurrency(this double value, string curr = null)
        {
            var sym = curr == null
                ? ""
                : CurrencySymbols.Config.Symbols.SingleOrDefault(cs => cs.Currency == curr)?.Symbol ?? curr + " ";
            var s = $"{sym}{value:N4}";
            return s.TrimEnd('0').CPadRight(s.Length);
        }

        /// <summary>
        ///     格式化用户
        /// </summary>
        /// <param name="value">金额</param>
        /// <param name="curr">币种</param>
        /// <returns>格式化后的金额</returns>
        public static string AsUser(this string value)
        {
            if (value == null)
                return string.Empty;

            if (Reg.IsMatch(value))
                return value;

            return value.Quotation('\'');
        }

        /// <summary>
        ///     格式化金额，用空格代替末尾的零
        /// </summary>
        /// <param name="value">金额</param>
        /// <param name="curr">币种</param>
        /// <returns>格式化后的金额</returns>
        public static string AsCurrency(this double? value, string curr = null) =>
            value.HasValue ? AsCurrency(value.Value, curr) : string.Empty;

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
                case SubtotalLevel.None:
                case SubtotalLevel.Day:
                case SubtotalLevel.Week:
                    return value.AsDate();
                case SubtotalLevel.Month:
                    return $"{value.Value.Year:D4}{value.Value.Month:D2}";
                case SubtotalLevel.Year:
                    return $"{value.Value.Year:D4}";
                default:
                    throw new ArgumentException("分类层次并非基于日期", nameof(level));
            }
        }

        /// <summary>
        ///     格式化日期过滤器
        /// </summary>
        /// <param name="value">日期过滤器</param>
        /// <returns>格式化后的日期过滤器</returns>
        public static string AsDateRange(this DateFilter value)
        {
            if (value.NullOnly)
                return "[null]";

            if (value.EndDate.HasValue)
            {
                if (value.StartDate.HasValue)
                    return value.Nullable
                        ? $"[{value.StartDate.AsDate()}~~{value.EndDate.AsDate()}]"
                        : $"[{value.StartDate.AsDate()}~{value.EndDate.AsDate()}]";

                return value.Nullable
                    ? $"[~{value.EndDate.AsDate()}]"
                    : $"[={value.EndDate.AsDate()}]";
            }

            if (value.StartDate.HasValue)
                return value.Nullable
                    ? $"[{value.StartDate.AsDate()}~~]"
                    : $"[{value.StartDate.AsDate()}~]";

            return value.Nullable ? "[]" : "[~null]";
        }
    }
}
