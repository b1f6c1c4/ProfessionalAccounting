/* Copyright (C) 2020-2024 b1f6c1c4
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

using System.Text.RegularExpressions;

namespace AccountingServer.BLL.Util;

/// <summary>
///     字符串格式化
/// </summary>
public static class StringFormatter
{
    private static readonly Regex Reg = new(@"[\uFF00-\uFFFF\u4e00-\u9fa5￥]");

    /// <summary>
    ///     左对齐补至指定长度
    /// </summary>
    /// <param name="s">待格式化的字符串</param>
    /// <param name="length">长度</param>
    /// <param name="chr">补码</param>
    /// <returns>格式化的字符串</returns>
    public static string CPadRight(this string s, int length, char chr = ' ')
    {
        s ??= string.Empty;

        if (length - s.Length - Reg.Matches(s).Count < 0)
            length = s.Length + Reg.Matches(s).Count;

        return s + new string(chr, length - s.Length - Reg.Matches(s).Count);
    }


    /// <summary>
    ///     右对齐补至指定长度
    /// </summary>
    /// <param name="s">待格式化的字符串</param>
    /// <param name="length">长度</param>
    /// <param name="chr">补码</param>
    /// <returns>格式化的字符串</returns>
    public static string CPadLeft(this string s, int length, char chr = ' ')
    {
        s ??= string.Empty;

        if (length - s.Length - Reg.Matches(s).Count < 0)
            length = s.Length + Reg.Matches(s).Count;

        return new string(chr, length - s.Length - Reg.Matches(s).Count) + s;
    }
}
