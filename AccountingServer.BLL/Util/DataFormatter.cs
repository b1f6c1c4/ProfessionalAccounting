/* Copyright (C) 2020-2022 b1f6c1c4
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
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL.Util;

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
    private static readonly Regex Reg = new(@"^[A-Za-z0-9_]+(&[A-Za-z0-9_]+)*$");

    /// <summary>
    ///     记账本位币信息文档
    /// </summary>
    static DataFormatter()
        => Cfg.RegisterType<CurrencySymbols>("Symbol");

    /// <summary>
    ///     解析用户过滤器
    /// </summary>
    /// <param name="value">格式化后的用户</param>
    /// <param name="client">客户端用户</param>
    /// <returns>用户过滤器</returns>
    public static string ParseUserSpec(this string value, Client client)
    {
        if (string.IsNullOrEmpty(value))
            return client.User;
        if (value == "U")
            return null;
        if (value.StartsWith("U'", StringComparison.Ordinal))
            return value[1..].Dequotation();
        return value[1..];
    }

    /// <summary>
    ///     格式化用户
    /// </summary>
    /// <param name="value">用户</param>
    /// <returns>格式化后的用户</returns>
    public static string AsUser(this string value)
    {
        if (value == null)
            return "";

        if (Reg.IsMatch(value))
            return $"U{value}";

        return $"U{value.Quotation('\'')}";
    }

    /// <summary>
    ///     解析金额
    /// </summary>
    /// <param name="value">格式化后的金额</param>
    /// <returns>金额</returns>
    public static string ParseCurrency(this string value)
    {
        if (value == null)
            return null;
        if (!value.StartsWith("@", StringComparison.Ordinal))
            throw new MemberAccessException("表达式错误");
        if (value == "@@")
            return BaseCurrency.Now;
        return value[1..].ToUpperInvariant();
    }

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
            : Cfg.Get<CurrencySymbols>().Symbols.SingleOrDefault(cs => cs.Currency == curr)?.Symbol ?? $"{curr} ";
        var s = $"{sym}{value:N4}";
        return s.TrimEnd('0').CPadRight(s.Length);
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
    public static string AsTitle(this int value) => $"T{value:0000}";

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
    /// <param name="level">分类层次</param>
    /// <returns>格式化后的日期</returns>
    public static string AsDate(this DateTime value, SubtotalLevel level)
        => (level & SubtotalLevel.Subtotal) switch
            {
                SubtotalLevel.None => value.AsDate(),
                SubtotalLevel.Day => value.AsDate(),
                SubtotalLevel.Week => value.AsDate(),
                SubtotalLevel.Month => $"{value.Year:D4}{value.Month:D2}",
                SubtotalLevel.Year => $"{value.Year:D4}",
                _ => throw new ArgumentException("分类层次并非基于日期", nameof(level)),
            };

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
        => !value.HasValue ? "[null]" : value.Value.AsDate(level);

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
                : $"[~~{value.EndDate.AsDate()}]";
        }

        if (value.StartDate.HasValue)
            return value.Nullable
                ? $"[{value.StartDate.AsDate()}~~]"
                : $"[{value.StartDate.AsDate()}~]";

        return value.Nullable ? "[]" : "[~null]";
    }
}
