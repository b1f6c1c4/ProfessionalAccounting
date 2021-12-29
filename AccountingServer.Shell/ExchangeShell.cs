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
using System.Linq;
using System.Text;
using System.Timers;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
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
    ///     定时登记汇率
    /// </summary>
    private readonly Timer m_Timer = new(60 * 60 * 1000) { AutoReset = true }; // hourly

    private DbSession m_TimerSession;

    public ExchangeShell()
        => m_Timer.Elapsed += OnTimedEvent;

    public IQueryResult Execute(string expr, Session session)
    {
        expr = expr.Rest();
        var isAccurate = expr.Initial() == "acc";
        if (isAccurate)
            expr = expr.Rest();
        var from = Parsing.Token(ref expr).ToUpperInvariant();
        var val = Parsing.DoubleF(ref expr);
        var to = Parsing.Token(ref expr)?.ToUpperInvariant() ?? BaseCurrency.Now;

        var sb = new StringBuilder();

        if (Parsing.UniqueTime(ref expr, session.Client) is var date && date.HasValue)
            Inquiry(session, sb, date.Value, from, to, val, isAccurate);
        else if (Parsing.Range(ref expr, session.Client) is var rng && rng != null)
            for (var dt = rng.StartDate!.Value; dt <= rng.EndDate; dt = dt.AddMonths(1))
                Inquiry(session, sb, AccountantHelper.LastDayOfMonth(dt.Year, dt.Month), from, to, val, isAccurate);
        else if (isAccurate)
            Inquiry(session, sb, null, from, to, val, true);
        else
            Inquiry(session, sb, DateTime.UtcNow.AddMinutes(-30), from, to, val, false);
        Parsing.Eof(expr);

        return new PlainText(sb.ToString());
    }

    private void Inquiry(Session session, StringBuilder sb, DateTime? dt, string from, string to, double value, bool isAccurate)
    {
        var rate = isAccurate ? session.Accountant.SaveHistoricalRate(dt!.Value, from, to).Result : session.Accountant.Query(dt, from, to).Result; // TODO
        var v = value * rate;
        sb.AppendLine($"{dt.AsDate()} @{from} {value.AsCurrency(from)} = @{to} {v.AsCurrency(to)} ({v:R})");
    }

    public bool IsExecutable(string expr) => expr.Initial() == "?e";

    /// <summary>
    ///     启动定时登记汇率
    /// </summary>
    public void EnableTimer(DbSession db)
    {
        m_TimerSession = db;
        OnTimedEvent(null, null);
        m_Timer.Enabled = true;
    }

    private static DateTime CriticalTime(DateTime now)
    {
        // every T00:00:00+Z of base currency change day
        if (BaseCurrency.History.Any(bci => bci.Date == now.Date))
            return now.Date.AddMinutes(-5);

        // every T00:00:00+Z of early Sunday
        if (now.DayOfWeek == DayOfWeek.Sunday)
            return now.Date.AddMinutes(-5);

        // every T00:00:00+Z of last day of month (year)
        if (now.AddDays(1).Month != now.Month)
            return now.Date.AddMinutes(-5);

        // every 3.5 days
        return now.AddDays(-3.5).AddMinutes(-35);
    }

    private void OnTimedEvent(object source, ElapsedEventArgs e)
    {
        var date = CriticalTime(DateTime.UtcNow);
        Console.WriteLine($"{DateTime.UtcNow:o} Ensuring exchange rates exist since {date:o}");
        var session = new Session(m_TimerSession);
        try
        {
            foreach (var grpC in session.Accountant.RunGroupedQuery("U - U Revenue - U Expense !C").Items.Cast<ISubtotalCurrency>())
                session.Accountant.Query(date, grpC.Currency, BaseCurrency.Now);
            foreach (var grpC in session.Accountant.RunGroupedQuery("U Revenue + U Expense 0 !C").Items.Cast<ISubtotalCurrency>())
                session.Accountant.Query(date, grpC.Currency, BaseCurrency.Now);
        }
        catch (Exception err)
        {
            Console.WriteLine(err);
        }
    }
}
