/* Copyright (C) 2020-2025 b1f6c1c4
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
using System.Text.RegularExpressions;
using AccountingServer.Entities;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell.Plugins.Statement;

/// <summary>
///     CSV解析
/// </summary>
internal class CsvParser
{
    private readonly int m_Dir;
    public List<BankItem> Items;

    public CsvParser(bool reversed)
        => m_Dir = reversed ? -1 : +1;

    private static string Next(ref string expr)
    {
        if (expr[0] == '"')
        {
            var tmp = ParsingF.Quoted(ref expr);
            if (string.IsNullOrWhiteSpace(expr))
            {
                expr = null;
                return tmp;
            }

            if (expr[0] == ',')
            {
                expr = expr[1..];
                return tmp;
            }

            throw new ApplicationException("Csv格式错误");
        }

        var id = expr.IndexOf(',');
        if (id < 0)
        {
            var tmp = expr;
            expr = null;
            return tmp;
        }

        var f = expr[..id];
        expr = expr[(id + 1)..];
        return f;
    }

    public void Parse(ref string csv)
    {
        var expr = csv;
        var headerB = ParsingF.Line(ref expr);
        if (headerB.StartsWith("\x0c", StringComparison.Ordinal)) // skip the region guarded by ^L
        {
            while (!ParsingF.Line(ref expr).StartsWith("\x0c", StringComparison.Ordinal)) { }

            csv = expr;
            headerB = ParsingF.Line(ref expr);
        }

        var header = headerB;

        var dateId = -1;
        var currId = -1;
        var fundId = -1;
        var debitId = -1;
        var creditId = -1;

        var dateReg = new Regex(@"^trans(?:\.|action)\s+date$", RegexOptions.IgnoreCase);
        var currReg = new Regex(@"^trans(?:\.|action)\s+curr(?:\.|ency)$", RegexOptions.IgnoreCase);
        var fundReg = new Regex(@"^(?:amount|fund|value)$", RegexOptions.IgnoreCase);
        var debitReg = new Regex(@"^debit$", RegexOptions.IgnoreCase);
        var creditReg = new Regex(@"^credit$", RegexOptions.IgnoreCase);
        for (var i = 0; !string.IsNullOrWhiteSpace(header); i++)
        {
            var f = Next(ref header);
            if (dateReg.IsMatch(f))
                dateId = i;
            else if (currReg.IsMatch(f))
                currId = i;
            else if (fundReg.IsMatch(f))
                fundId = i;
            else if (debitReg.IsMatch(f))
                debitId = i;
            else if (creditReg.IsMatch(f))
                creditId = i;
        }

        if (dateId < 0)
        {
            header = headerB;
            var dateReg2 = new Regex(@"^date$", RegexOptions.IgnoreCase);
            for (var i = 0; !string.IsNullOrWhiteSpace(header); i++)
            {
                var f = Next(ref header);
                if (i == fundId || i == currId)
                    continue;
                if (dateReg2.IsMatch(f))
                    dateId = i;
            }

            if (dateId < 0)
                throw new ApplicationException("找不到日期字段");
        }

        if (fundId < 0 && (debitId < 0 || creditId < 0))
            throw new ApplicationException("找不到金额字段");
        if (fundId >= 0 && (debitId >= 0 && creditId >= 0))
            throw new ApplicationException("多于一个金额字段");

        Items = new();
        while (!string.IsNullOrWhiteSpace(expr))
        {
            var l = ParsingF.Line(ref expr);
            var item = new BankItem { Raw = l };
            for (var i = 0; !string.IsNullOrWhiteSpace(l); i++)
            {
                var f = Next(ref l);
                if (i == dateId)
                    item.Date = DateTimeParser.Parse(f);
                else if (i == currId)
                    item.Currency = f;
                else if (i == fundId)
                    item.Fund = m_Dir * Convert.ToDouble(f);
                else if (i == debitId && !string.IsNullOrWhiteSpace(f))
                    item.Fund = m_Dir * Convert.ToDouble(f);
                else if (i == creditId && !string.IsNullOrWhiteSpace(f))
                    item.Fund = m_Dir * Convert.ToDouble(f);
            }

            Items.Add(item);
        }
    }
}

internal class BankItem
{
    public string Currency;
    public DateTime Date;
    public double Fund;
    public string Raw;
}
