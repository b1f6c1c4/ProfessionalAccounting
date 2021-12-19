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
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Plugins.CashFlow;

/// <summary>
///     自动计算现金流
/// </summary>
internal class CashFlow : PluginBase
{
    public CashFlow(Accountant accountant) : base(accountant) { }

    public static IConfigManager<CashTemplates> Templates { private get; set; } =
        new ConfigManager<CashTemplates>("Cash.xml");

    /// <inheritdoc />
    public override IQueryResult Execute(string expr, IEntitiesSerializer serializer)
    {
        var extraMonths = (int)(Parsing.Double(ref expr) ?? 6);
        var prefix = Parsing.Token(ref expr);
        Parsing.Eof(expr);

        var accts = Templates.Config.Accounts
            .Where(a => string.IsNullOrWhiteSpace(a.User) || ClientUser.Name == a.User).ToList();
        var n = accts.Count;
        var until = ClientDateTime.Today.AddMonths(extraMonths);

        var aggs = new double[n];
        var rst = new Dictionary<DateTime, double[,]>();

        for (var i = 0; i < n; i++)
        {
            aggs[i] = Accountant
                .RunGroupedQuery($"U{accts[i].User.AsUser()} @{accts[i].Currency}*({accts[i].QuickAsset}) [~.]``v")
                .Fund;

            foreach (var (date, value) in GetItems(accts[i], serializer, until))
            {
                if (date <= ClientDateTime.Today)
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

        {
            sb.Append(prefix);
            sb.Append("Today   ");

            var sum = 0D;
            for (var i = 0; i < n; i++)
            {
                sum += aggs[i] * Accountant.Query(ClientDateTime.Today, accts[i].Currency, BaseCurrency.Now);

                sb.Append("".PadLeft(15));
                sb.Append("".PadLeft(15));
                sb.Append(aggs[i].AsCurrency(accts[i].Currency).PadLeft(15));
            }

            sb.AppendLine(sum.AsCurrency(BaseCurrency.Now).PadLeft(15));
        }

        foreach (var kvp in rst.OrderBy(kvp => kvp.Key))
        {
            sb.Append(prefix);
            sb.Append($"{kvp.Key.AsDate()}");

            var sum = 0D;
            for (var i = 0; i < n; i++)
            {
                aggs[i] += kvp.Value[i, 0] + kvp.Value[i, 1];

                sum += aggs[i] * Accountant.Query(kvp.Key, accts[i].Currency, BaseCurrency.Now);

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
        }

        return new PlainText(sb.ToString());
    }

    private DateTime NextDate(int day, DateTime? reference = null, bool inclusive = false)
    {
        var d = reference ?? ClientDateTime.Today;
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

    private IEnumerable<(DateTime Date, double Value)> GetItems(CashAccount account, IEntitiesSerializer serializer,
        DateTime until)
    {
        var user = $"U{account.User.AsUser()}";
        var curr = $"@{account.Currency}";

        if (account.Reimburse != null)
        {
            var rb = new Composite.Composite(Accountant);
            var tmp = Composite.Composite.GetTemplate(account.Reimburse);
            var rng = Composite.Composite.DateRange(tmp.Day);
            rb.DoInquiry(rng, tmp, out var rbVal, BaseCurrency.Now, serializer);
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
                    yield return (oqi.Date, Accountant.RunGroupedQuery($"{user} {curr}*({oqi.Query})``v").Fund);

                    break;

                case MonthlyItem mn:
                    for (var d = NextDate(mn.Day); d <= until; d = NextDate(mn.Day, d))
                    {
                        if (mn.Since != default && d < mn.Since)
                            continue;
                        if (mn.Till != default && d > mn.Till)
                            continue;

                        yield return (d, mn.Fund);
                    }

                    break;

                case SimpleCreditCard cc:
                    var rng = $"[{ClientDateTime.Today.AddMonths(-3).AsDate()}~]";
                    var mv = $"{{({user}*{cc.Query})+{user} T3999+{user} T6603 A {rng}}}";
                    var mos = new Dictionary<DateTime, double>();
                    foreach (var grpC in Accountant.RunGroupedQuery(
                                     $"{{{user}*({cc.Query})*({user} <+(-{user} {curr})) {rng}}}+{mv}:{user}*({cc.Query})`Cd")
                                 .Items
                                 .Cast<ISubtotalCurrency>())
                    foreach (var b in grpC.Items.Cast<ISubtotalDate>())
                    {
                        var mo = NextDate(cc.RepaymentDay, NextDate(cc.BillDay, b.Date.Value), true);
                        var cob = Accountant.Query(mo, grpC.Currency, account.Currency)
                            * b.Fund;
                        if (mos.ContainsKey(mo))
                            mos[mo] += cob;
                        else
                            mos[mo] = cob;
                    }

                    foreach (var b in Accountant
                                 .RunGroupedQuery(
                                     $"{{{user}*({cc.Query})*({user} {curr}>) {rng}}}-{mv}:{user}*({cc.Query})`d")
                                 .Items
                                 .Cast<ISubtotalDate>())
                    {
                        var mo = NextDate(cc.RepaymentDay, b.Date.Value, true);
                        if (mos.ContainsKey(mo))
                            mos[mo] += b.Fund;
                        else
                            mos[mo] = b.Fund;
                    }

                    for (var d = ClientDateTime.Today; d <= until; d = NextDate(cc.BillDay, d))
                    {
                        var mo = NextDate(cc.RepaymentDay, NextDate(cc.BillDay, d), true);
                        var cob = -(NextDate(cc.BillDay, d) - d).TotalDays * cc.MonthlyFee / (365.2425 / 12);
                        if (mos.ContainsKey(mo))
                            mos[mo] += cob;
                        else
                            mos[mo] = cob;
                    }

                    foreach (var (key, value) in mos)
                        yield return (key, value);

                    break;

                case ComplexCreditCard cc:
                    var stmt = -Accountant.RunGroupedQuery($"({cc.Query})*({user} {curr})-{user} \"\"``v").Fund;
                    var pmt = Accountant.RunGroupedQuery($"({cc.Query})*({user} {curr} \"\" >)``v").Fund;
                    var nxt = -Accountant.RunGroupedQuery($"({cc.Query})*({user} {curr} \"\" <)``v").Fund;
                    if (pmt < stmt)
                    {
                        if (NextDate(cc.BillDay, NextDate(cc.RepaymentDay)) == NextDate(cc.BillDay))
                            yield return (NextDate(cc.RepaymentDay), pmt - stmt);
                        else
                            nxt += stmt - pmt; // Not paid in full
                    }
                    else
                        nxt -= pmt - stmt; // Paid too much

                    for (var d = ClientDateTime.Today; d <= until; d = NextDate(cc.BillDay, d))
                    {
                        nxt += (NextDate(cc.BillDay, d) - d).TotalDays * cc.MonthlyFee / (365.2425 / 12);
                        if (cc.MaximumUtility >= 0 && cc.MaximumUtility < nxt)
                        {
                            yield return (NextDate(cc.BillDay, d), cc.MaximumUtility - nxt);
                            nxt = cc.MaximumUtility;
                        }

                        yield return (NextDate(cc.RepaymentDay, d).AddMonths(1), -nxt);
                        nxt = 0;
                    }

                    break;

                default:
                    throw new InvalidOperationException();
            }
    }
}