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
using System.Text;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Plugins.CashFlow;

/// <summary>
///     自动计算现金流
/// </summary>
internal class CashFlow : PluginBase
{
    static CashFlow()
        => Cfg.RegisterType<CashTemplates>("Cash");

    /// <inheritdoc />
    public override async IAsyncEnumerable<string> Execute(string expr, Session session)
    {
        var extraMonths = (int)(Parsing.Double(ref expr) ?? 6);
        var prefix = Parsing.Token(ref expr);
        Parsing.Eof(expr);

        var accts = Cfg.Get<CashTemplates>().Accounts
            .Where(a => string.IsNullOrWhiteSpace(a.User) || session.Client.User == a.User).ToList();
        var n = accts.Count;
        var until = session.Client.Today.AddMonths(extraMonths);

        var aggs = new double[n];
        var rst = new Dictionary<DateTime, double[,]>();

        for (var i = 0; i < n; i++)
        {
            aggs[i] = (await session.Accountant.RunGroupedQueryAsync(
                $"{accts[i].User.AsUser()} @{accts[i].Currency}*({accts[i].QuickAsset}) [~.]``v")).Fund;

            foreach (var (date, value) in GetItems(accts[i], session, until).ToEnumerable())
            {
                if (date <= session.Client.Today)
                    continue;
                if (date > until)
                    continue;
                if (value.IsZero())
                    continue;

                if (!rst.ContainsKey(date))
                    rst.Add(date, new double[n, 2]);

                if (value >= 0)
                    rst[date][i, 0] += value;
                else
                    rst[date][i, 1] += value;
            }
        }

        var sb = new StringBuilder();
        sb.Append(prefix);
        sb.Append("Date    ");
        for (var i = 0; i < n; i++)
        {
            var c = accts[i].Currency;
            sb.Append($"++++ {c} ++++".PadLeft(15));
            sb.Append($"---- {c} ----".PadLeft(15));
            sb.Append($"#### {c} ####".PadLeft(15));
        }

        sb.AppendLine("@@@@ All @@@@".PadLeft(15));
        yield return sb.ToString();
        sb.Clear();

        {
            sb.Append(prefix);
            sb.Append("Today   ");

            var sum = 0D;
            for (var i = 0; i < n; i++)
            {
                sum += aggs[i] *
                    await session.Accountant.Query(session.Client.Today, accts[i].Currency, BaseCurrency.Now);

                sb.Append("".PadLeft(15));
                sb.Append("".PadLeft(15));
                sb.Append(aggs[i].AsCurrency(accts[i].Currency).PadLeft(15));
            }

            sb.AppendLine(sum.AsCurrency(BaseCurrency.Now).PadLeft(15));
            yield return sb.ToString();
            sb.Clear();
        }

        foreach (var kvp in rst.OrderBy(static kvp => kvp.Key))
        {
            sb.Append(prefix);
            sb.Append($"{kvp.Key.AsDate()}");

            var sum = 0D;
            for (var i = 0; i < n; i++)
            {
                aggs[i] += kvp.Value[i, 0] + kvp.Value[i, 1];

                sum += aggs[i] * await session.Accountant.Query(kvp.Key, accts[i].Currency, BaseCurrency.Now);

                if (!kvp.Value[i, 0].IsZero())
                    sb.Append(kvp.Value[i, 0].AsCurrency(accts[i].Currency).PadLeft(15));
                else
                    sb.Append("".PadLeft(15));

                if (!kvp.Value[i, 1].IsZero())
                    sb.Append(kvp.Value[i, 1].AsCurrency(accts[i].Currency).PadLeft(15));
                else
                    sb.Append("".PadLeft(15));

                sb.Append(aggs[i].AsCurrency(accts[i].Currency).PadLeft(15));
            }

            sb.AppendLine(sum.AsCurrency(BaseCurrency.Now).PadLeft(15));
            yield return sb.ToString();
            sb.Clear();
        }
    }

    private DateTime NextDate(Session session, int day, DateTime? reference = null, bool inclusive = false)
    {
        var d = reference ?? session.Client.Today;
        var v = new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var last = v.AddMonths(1).AddDays(-1).Day;
        var targ = day <= last ? day : last;
        if (!inclusive)
        {
            if (d.Day >= targ)
                v = v.AddMonths(1);
        }
        else
        {
            if (d.Day > targ)
                v = v.AddMonths(1);
        }

        last = v.AddMonths(1).AddDays(-1).Day;
        targ = day <= last ? day : last;

        return v.AddDays(targ - 1);
    }

    private async IAsyncEnumerable<(DateTime Date, double Value)> GetItems(CashAccount account, Session session,
        DateTime until)
    {
        var user = account.User.AsUser();
        var curr = $"@{account.Currency}";

        if (account.Reimburse != null)
        {
            var rb = new Composite.Composite();
            var tmp = Composite.Composite.GetTemplate(account.Reimburse);
            var rng = Composite.Composite.DateRange(tmp.Day, session.Client.Today);
            var (_, rbVal) = await rb.DoInquiry(rng, tmp, BaseCurrency.Now, session);
            var rbF = rng.EndDate!.Value;
            yield return (rbF, rbVal);
        }

        foreach (var debt in account.Items)
            switch (debt)
            {
                case OnceItem oi:
                    yield return (oi.Date, oi.Fund);

                    break;

                case OnceQueryItem oqi:
                    yield return (oqi.Date,
                        (await session.Accountant.RunGroupedQueryAsync($"{user} {curr}*({oqi.Query})``v")).Fund);

                    break;

                case MonthlyItem mn:
                    for (var d = NextDate(session, mn.Day); d <= until; d = NextDate(session, mn.Day, d))
                    {
                        if (mn.Since != default && d < mn.Since)
                            continue;
                        if (mn.Till != default && d > mn.Till)
                            continue;

                        yield return (d, mn.Fund);
                    }

                    break;

                case SimpleCreditCard cc:
                    var rng = $"[{session.Client.Today.AddMonths(-3).AsDate()}~]";
                    var mv = $"{{({user}*{cc.Query})+{user} T3999+{user} T6603 A {rng}}}";
                    var mos = new Dictionary<DateTime, double>();
                    foreach (var grpC in (await session.Accountant.RunGroupedQueryAsync(
                                 $"{{{user}*({cc.Query})*({user} <+(-{user} {curr})) {rng}}}+{mv}:{user}*({cc.Query})`Cd"))
                             .Items
                             .Cast<ISubtotalCurrency>())
                    foreach (var b in grpC.Items.Cast<ISubtotalDate>())
                    {
                        var mo = NextDate(session, cc.RepaymentDay, NextDate(session, cc.BillDay, b.Date.Value), true);
                        var cob = await session.Accountant.Query(mo, grpC.Currency, account.Currency) * b.Fund;
                        if (mos.ContainsKey(mo))
                            mos[mo] += cob;
                        else
                            mos[mo] = cob;
                    }

                    foreach (var b in (await session.Accountant
                                 .RunGroupedQueryAsync(
                                     $"{{{user}*({cc.Query})*({user} {curr}>) {rng}}}-{mv}:{user}*({cc.Query})`d"))
                             .Items
                             .Cast<ISubtotalDate>())
                    {
                        var mo = NextDate(session, cc.RepaymentDay, b.Date.Value, true);
                        if (mos.ContainsKey(mo))
                            mos[mo] += b.Fund;
                        else
                            mos[mo] = b.Fund;
                    }

                    for (var d = session.Client.Today; d <= until; d = NextDate(session, cc.BillDay, d))
                    {
                        var mo = NextDate(session, cc.RepaymentDay, NextDate(session, cc.BillDay, d), true);
                        var cob = -(NextDate(session, cc.BillDay, d) - d).TotalDays * cc.MonthlyFee / (365.2425 / 12);
                        if (mos.ContainsKey(mo))
                            mos[mo] += cob;
                        else
                            mos[mo] = cob;
                    }

                    foreach (var (key, value) in mos)
                        yield return (key, value);

                    break;

                case ComplexCreditCard cc:
                    var stmt = -(await session.Accountant.RunGroupedQueryAsync(
                        $"({cc.Query})*({user} {curr})-{user} \"\"``v")).Fund;
                    var pmt = (await session.Accountant.RunGroupedQueryAsync($"({cc.Query})*({user} {curr} \"\" >)``v"))
                        .Fund;
                    var nxt =
                        -(await session.Accountant.RunGroupedQueryAsync($"({cc.Query})*({user} {curr} \"\" <)``v"))
                            .Fund;
                    if (pmt < stmt)
                    {
                        if (NextDate(session, cc.BillDay, NextDate(session, cc.RepaymentDay)) ==
                            NextDate(session, cc.BillDay))
                            yield return (NextDate(session, cc.RepaymentDay), pmt - stmt);
                        else
                            nxt += stmt - pmt; // Not paid in full
                    }
                    else
                        nxt -= pmt - stmt; // Paid too much

                    for (var d = session.Client.Today; d <= until; d = NextDate(session, cc.BillDay, d))
                    {
                        nxt += (NextDate(session, cc.BillDay, d) - d).TotalDays * cc.MonthlyFee / (365.2425 / 12);
                        if (cc.MaximumUtility >= 0 && cc.MaximumUtility < nxt)
                        {
                            yield return (NextDate(session, cc.BillDay, d), cc.MaximumUtility - nxt);
                            nxt = cc.MaximumUtility;
                        }

                        yield return (NextDate(session, cc.RepaymentDay, d).AddMonths(1), -nxt);
                        nxt = 0;
                    }

                    break;

                default:
                    throw new InvalidOperationException();
            }
    }
}
