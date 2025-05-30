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
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
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

    public async IAsyncEnumerable<string> Execute(string expr, Context ctx, string term)
    {
        ctx.Identity.WillInvoke("?e");
        expr = expr.Rest();
        var isAccurate = expr.Initial() == "acc";
        if (isAccurate)
        {
            ctx.Identity.WillInvoke("?e-acc");
            expr = expr.Rest();
        }
        var from = Parsing.Token(ref expr).ToUpperInvariant();
        var val = Parsing.DoubleF(ref expr);
        var to = Parsing.Token(ref expr)?.ToUpperInvariant() ?? BaseCurrency.Now;

        if (Parsing.UniqueTime(ref expr, ctx.Client) is var date && date.HasValue)
            yield return await Inquiry(ctx, date.Value, from, to, val, isAccurate);
        else if (Parsing.Range(ref expr, ctx.Client) is var rng && rng != null)
            for (var dt = rng.StartDate!.Value; dt <= rng.EndDate; dt = dt.AddMonths(1))
                yield return await Inquiry(ctx, DateHelper.LastDayOfMonth(dt.Year, dt.Month), from, to, val,
                    isAccurate);
        else if (isAccurate)
            yield return await Inquiry(ctx, null, from, to, val, true);
        else
            yield return await Inquiry(ctx, DateTime.UtcNow.AddMinutes(-30), from, to, val, false);
        Parsing.Eof(expr);
    }

    public bool IsExecutable(string expr) => expr.Initial() == "?e";

    private async ValueTask<string> Inquiry(Context ctx, DateTime? dt, string from, string to, double value,
        bool isAccurate)
    {
        var rate = isAccurate
            ? await ctx.Accountant.SaveHistoricalRate(dt!.Value, from, to)
            : await ctx.Accountant.Query(dt, from, to);
        var v = value * rate;
        return $"{dt.AsDate()} {from.AsCurrency()} {value.AsFund(from)} = {to.AsCurrency()} {v.AsFund(to)} ({v:R})\n";
    }

    /// <summary>
    ///     启动定时登记汇率
    /// </summary>
    public void EnableTimer(DbSession db)
    {
        m_TimerSession = db;
 #pragma warning disable CS4014
        ImmediateExchange();
 #pragma warning restore CS4014
        m_Timer.Enabled = true;
    }

    /// <summary>
    ///     立即登记汇率
    /// </summary>
    public Task ImmediateExchange(DbSession db)
    {
        m_TimerSession = db;
        return ImmediateExchange();
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

    // ReSharper disable once AsyncVoidMethod
    private void OnTimedEvent(object source, ElapsedEventArgs e)
 #pragma warning disable CS4014
        => ImmediateExchange();
 #pragma warning restore CS4014

    private async Task ImmediateExchange()
    {
        var date = CriticalTime(DateTime.UtcNow);
        var log = $"{DateTime.UtcNow:o} Ensuring exchange rates exist since {date:o}";
        if (m_TimerSession.ExchangeLogger != null)
            m_TimerSession.ExchangeLogger(log, false);
        else
            Console.WriteLine(log);
        var ctx = new Context(m_TimerSession);
        try
        {
            Task.WhenAll((await ctx.Accountant.RunGroupedQueryAsync("U - U Revenue - U Expense !C"))
                .Items.Cast<ISubtotalCurrency>()
                .Where(static grpC => !grpC.Currency.EndsWith('#'))
                .Concat((await ctx.Accountant.RunGroupedQueryAsync("U Revenue + U Expense 0 !C"))
                    .Items.Cast<ISubtotalCurrency>())
                .Select(grpC =>
                    ctx.Accountant.Query(date, grpC.Currency, BaseCurrency.Now).AsTask())).Wait();
        }
        catch (Exception err)
        {
            if (m_TimerSession.ExchangeLogger != null)
                m_TimerSession.ExchangeLogger(err.ToString(), true);
            else
                Console.WriteLine(err);
        }
    }
}
