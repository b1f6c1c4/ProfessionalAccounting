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

namespace AccountingServer.BLL.Util;

/// <summary>
///     带引号的字符串辅助类
/// </summary>
public static class QuotedStringHelper
{
    /// <summary>
    ///     给字符串添加引号
    /// </summary>
    /// <param name="unquoted">原字符串</param>
    /// <param name="chr">引号</param>
    /// <returns>字符串</returns>
    public static string Quotation(this string unquoted, char chr) =>
        $"{chr}{(unquoted ?? "").Replace(new(chr, 1), new string(chr, 2))}{chr}";

    /// <summary>
    ///     给字符串解除引号
    /// </summary>
    /// <param name="quoted">字符串</param>
    /// <returns>原字符串</returns>
    public static string Dequotation(this string quoted)
    {
        switch (quoted?.Length)
        {
            case null:
                return null;
            case 0:
                return quoted;
            case 1:
                throw new ArgumentException("格式错误", nameof(quoted));
        }

        var chr = quoted[0];
        if (quoted[^1] != chr)
            throw new ArgumentException("格式错误", nameof(quoted));

        var s = quoted.Substring(1, quoted.Length - 2);
        return s.Replace($"{chr}{chr}", $"{chr}");
    }
}
