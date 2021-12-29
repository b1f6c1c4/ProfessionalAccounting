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
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Util;

/// <summary>
///     扩展的字符串匹配
/// </summary>
[SuppressMessage("ReSharper", "UnusedParameter.Global")]
public static class ParseHelper
{
    /// <summary>
    ///     匹配EOF
    /// </summary>
    /// <param name="facade">占位符</param>
    /// <param name="expr">表达式</param>
    // ReSharper disable once UnusedParameter.Global
    public static void Eof(this FacadeBase facade, string expr)
    {
        if (!string.IsNullOrWhiteSpace(expr))
            throw new ArgumentException("语法错误", nameof(expr));
    }

    public static IAsyncEnumerable<Voucher> RunVoucherQueryAsync(this Accountant acc, string str)
    {
        var res = FacadeF.ParsingF.VoucherQuery(ref str, acc.Client);
        FacadeF.ParsingF.Eof(str);
        return acc.SelectVouchersAsync(res);
    }

    public static ValueTask<long> DeleteVouchersAsync(this Accountant acc, string str)
    {
        var res = FacadeF.ParsingF.VoucherQuery(ref str, acc.Client);
        FacadeF.ParsingF.Eof(str);
        return acc.DeleteVouchersAsync(res);
    }

    public static ValueTask<ISubtotalResult> RunGroupedQueryAsync(this Accountant acc, string str)
    {
        var res = FacadeF.ParsingF.GroupedQuery(ref str, acc.Client);
        FacadeF.ParsingF.Eof(str);
        return acc.SelectVoucherDetailsGroupedAsync(res);
    }

    public static ValueTask<ISubtotalResult> RunVoucherGroupedQueryAsync(this Accountant acc, string str)
    {
        var res = FacadeF.ParsingF.VoucherGroupedQuery(ref str, acc.Client);
        FacadeF.ParsingF.Eof(str);
        return acc.SelectVouchersGroupedAsync(res);
    }

    /// <summary>
    ///     忽略空白和注释
    /// </summary>
    /// <param name="facade">占位符</param>
    /// <param name="expr">表达式</param>
    public static void TrimStartComment(this FacadeBase facade, ref string expr)
    {
        expr = expr.TrimStart();
        var regex = new Regex(@"[^\r\n]*(\r\n|\n|\n\r)");
        while (expr.Length > 2 &&
               expr[0] == '/' &&
               expr[1] == '/')
        {
            var m = regex.Match(expr);
            if (!m.Success)
            {
                expr = string.Empty;
                return;
            }

            expr = expr[m.Length..];
            expr = expr.TrimStart();
        }
    }

    /// <summary>
    ///     匹配行
    /// </summary>
    /// <param name="facade">占位符</param>
    /// <param name="expr">表达式</param>
    /// <returns>字符串</returns>
    public static string Line(this FacadeBase facade, ref string expr)
    {
        var id = expr.IndexOf('\n');
        if (id < 0)
        {
            var tmp = expr;
            expr = null;
            return tmp;
        }

        var line = expr[..id];
        expr = expr[(id + 1)..];
        return line;
    }

    /// <summary>
    ///     匹配带括号和连续字符串
    /// </summary>
    /// <param name="facade">占位符</param>
    /// <param name="expr">表达式</param>
    /// <param name="allow">允许括号</param>
    /// <param name="predicate">是否有效</param>
    /// <returns>字符串</returns>
    public static string Token(this FacadeBase facade, ref string expr, bool allow = true,
        Func<string, bool> predicate = null)
    {
        expr = expr.TrimStart();
        if (expr.Length == 0)
            return null;

        if (allow)
            if (expr[0] == '\'' ||
                expr[0] == '"')
            {
                var tmp = expr;
                var res = facade.Quoted(ref expr);
                if (!predicate?.Invoke(res) != true)
                    return res;

                expr = tmp;
                return null;
            }

        var id = 1;
        while (id < expr.Length)
        {
            if (char.IsWhiteSpace(expr[id]))
                break;

            id++;
        }

        var t = expr[..id];
        if (!predicate?.Invoke(t) == true)
            return null;

        expr = expr[id..];
        return t;
    }

    /// <summary>
    ///     匹配可选的数
    /// </summary>
    /// <param name="facade">占位符</param>
    /// <param name="expr">表达式</param>
    /// <returns>数</returns>
    public static double? Double(this FacadeBase facade, ref string expr)
    {
        var d = double.NaN;
        if (facade.Token(ref expr, false, t => double.TryParse(t, out d)) != null)
            return d;

        return null;
    }

    /// <summary>
    ///     匹配数
    /// </summary>
    /// <param name="facade">占位符</param>
    /// <param name="expr">表达式</param>
    /// <returns>数</returns>
    public static double DoubleF(this FacadeBase facade, ref string expr) => Double(facade, ref expr) ??
        throw new FormatException();

    /// <summary>
    ///     匹配可选的非零长度字符串
    /// </summary>
    /// <param name="facade">占位符</param>
    /// <param name="expr">表达式</param>
    /// <param name="opt">字符串</param>
    /// <returns>是否匹配</returns>
    // ReSharper disable once UnusedParameter.Global
    public static bool Optional(this FacadeBase facade, ref string expr, string opt)
    {
        expr = expr.TrimStart();
        if (!expr.StartsWith(opt, StringComparison.Ordinal))
            return false;

        expr = expr[opt.Length..];
        return true;
    }

    /// <summary>
    ///     匹配带引号的字符串
    /// </summary>
    /// <param name="facade">占位符</param>
    /// <param name="expr">表达式</param>
    /// <param name="c">引号（若为空表示任意）</param>
    // ReSharper disable once UnusedParameter.Global
    public static string Quoted(this FacadeBase facade, ref string expr, char? c = null)
    {
        expr = expr.TrimStart();
        if (expr.Length < 1)
            return null;

        var ch = expr[0];
        if (c != null &&
            ch != c)
            return null;

        var id = -1;
        while (true)
        {
            id = expr.IndexOf(ch, id + 2);
            if (id < 0)
                throw new ArgumentException("语法错误", nameof(expr));

            if (id == expr.Length - 1)
                break;
            if (expr[id + 1] != ch)
                break;
        }

        var s = expr[..(id + 1)];
        expr = expr[(id + 1)..];
        return s.Dequotation();
    }

    /// <summary>
    ///     匹配可选的冒号开始的记账凭证检索式
    /// </summary>
    /// <param name="facade">占位符</param>
    /// <param name="expr">表达式</param>
    /// <param name="client">客户端</param>
    /// <returns>记账凭证检索式</returns>
    public static IQueryCompounded<IVoucherQueryAtom> OptColVouchers(this FacadeBase facade, ref string expr,
        Client client)
        => Optional(facade, ref expr, ":") ? facade.VoucherQuery(ref expr, client) : null;
}
