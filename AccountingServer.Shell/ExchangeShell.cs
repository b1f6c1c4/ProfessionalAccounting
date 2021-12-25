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
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell;

/// <summary>
///     汇率
/// </summary>
internal class ExchangeShell : IShellComponent
{
    /// <summary>
    ///     基本会计业务处理类
    /// </summary>
    private readonly Accountant m_Accountant;

    public ExchangeShell(Accountant helper) => m_Accountant = helper;

    public IQueryResult Execute(string expr, IEntitiesSerializer serializer)
    {
        expr = expr.Rest();
        var isAccurate = expr.Initial() == "acc";
        if (isAccurate)
            expr = expr.Rest();
        var from = Parsing.Token(ref expr).ToUpperInvariant();
        var val = Parsing.DoubleF(ref expr);
        var to = Parsing.Token(ref expr)?.ToUpperInvariant() ?? BaseCurrency.Now;

        var sb = new StringBuilder();

        if (Parsing.UniqueTime(ref expr) is var date && date.HasValue)
            Inquiry(sb, date.Value, from, to, val, isAccurate);
        else if (Parsing.Range(ref expr) is var rng && rng != null)
            for (var dt = rng.StartDate!.Value; dt <= rng.EndDate; dt = dt.AddMonths(1))
                Inquiry(sb, AccountantHelper.LastDayOfMonth(dt.Year, dt.Month), from, to, val, isAccurate);
        else
            Inquiry(sb, null, from, to, val, isAccurate);
        Parsing.Eof(expr);

        return new PlainText(sb.ToString());
    }

    private void Inquiry(StringBuilder sb, DateTime? dt, string from, string to, double value, bool isAccurate)
    {
        var rate = isAccurate ? m_Accountant.SaveHistoricalRate(dt!.Value, from, to) : m_Accountant.Query(dt, from, to);
        var v = value * rate;
        sb.AppendLine($"{dt.AsDate()} @{from} {value.AsCurrency(from)} = @{to} {v.AsCurrency(to)} ({v:R})");
    }

    public bool IsExecutable(string expr) => expr.Initial() == "?e";
}
