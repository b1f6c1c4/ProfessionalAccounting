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

namespace AccountingServer.Shell.Plugins.CashFlow
{
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

            var n = Templates.Config.Accounts.Count;
            var until = ClientDateTime.Today.AddMonths(extraMonths);

            var aggs = new double[n];
            var rst = new Dictionary<DateTime, double[,]>();

            for (var i = 0; i < n; i++)
            {
                var account = Templates.Config.Accounts[i];
                aggs[i] = Accountant.RunGroupedQuery($"@{account.Currency}*({account.QuickAsset}) [~.]``v").Fund;

                foreach (var (date, value) in GetItems(account, serializer, until))
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
                var c = Templates.Config.Accounts[i].Currency;
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
                    sum += aggs[i] * Accountant.From(
                        ClientDateTime.Today,
                        Templates.Config.Accounts[i].Currency);

                    sb.Append("".PadLeft(15));
                    sb.Append("".PadLeft(15));
                    sb.Append(aggs[i].AsCurrency(Templates.Config.Accounts[i].Currency).PadLeft(15));
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

                    sum += aggs[i] * Accountant.From(
                        kvp.Key,
                        Templates.Config.Accounts[i].Currency);

                    if (!kvp.Value[i, 0].IsZero())
                        sb.Append(kvp.Value[i, 0].AsCurrency(Templates.Config.Accounts[i].Currency).PadLeft(15));
                    else
                        sb.Append("".PadLeft(15));

                    if (!kvp.Value[i, 1].IsZero())
                        sb.Append(kvp.Value[i, 1].AsCurrency(Templates.Config.Accounts[i].Currency).PadLeft(15));
                    else
                        sb.Append("".PadLeft(15));

                    sb.Append(aggs[i].AsCurrency(Templates.Config.Accounts[i].Currency).PadLeft(15));
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

        private IEnumerable<(DateTime Date, double Value)> GetItems(CashAccount account, IEntitiesSerializer serializer, DateTime until)
        {
            var curr = $"@{account.Currency}";

            if (account.Reimburse != null)
            {
                var rb = new Composite.Composite(Accountant);
                var tmp = Composite.Composite.GetTemplate(account.Reimburse);
                var rng = Composite.Composite.DateRange(tmp.Day);
                rb.DoInquiry(rng, tmp, out var rbVal, BaseCurrency.Now, serializer);
                // ReSharper disable once PossibleInvalidOperationException
                var rbF = rng.EndDate.Value;
                yield return (rbF, rbVal);
            }

            foreach (var debt in account.Items)
                switch (debt)
                {
                    case OnceItem oi:
                        yield return (oi.Date, oi.Fund);

                        break;

                    case OnceQueryItem oqi:
                        yield return (oqi.Date, Accountant.RunGroupedQuery($"{curr}*({oqi.Query})``v").Fund);

                        break;

                    case MonthlyItem mn:
                        for (var d = NextDate(mn.Day); d <= until; d = NextDate(mn.Day, d))
                        {
                            if (mn.Since != default(DateTime) && d < mn.Since)
                                continue;
                            if (mn.Till != default(DateTime) && d > mn.Till)
                                continue;

                            yield return (d, mn.Fund);
                        }

                        break;

                    case SimpleCreditCard cc:
                        var rng = $"[{ClientDateTime.Today.AddMonths(-3).AsDate()}~]";
                        var mv = $"{{({cc.Query})+T3999+T6603 A {rng}}}";
                        var mos = new Dictionary<DateTime, double>();
                        foreach (var grpC in Accountant.RunGroupedQuery( $"{{({cc.Query})*(<+(-{curr})) {rng}}}+{mv}:{cc.Query}`Cd")
                            .Items
                            .Cast<ISubtotalCurrency>())
                        foreach (var b in grpC.Items.Cast<ISubtotalDate>())
                        {
                            var mo = NextDate(cc.RepaymentDay, NextDate(cc.BillDay, b.Date.Value), true);
                            var cob = Accountant.From(mo, grpC.Currency)
                                * Accountant.To(mo, account.Currency)
                                * b.Fund;
                            if (mos.ContainsKey(mo))
                                mos[mo] += cob;
                            else
                                mos[mo] = cob;
                        }

                        foreach (var b in Accountant.RunGroupedQuery($"{{({cc.Query})*({curr}>) {rng}}}-{mv}:{cc.Query}`d")
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

                        foreach (var kvp in mos)
                            yield return (kvp.Key, kvp.Value);

                        break;

                    case ComplexCreditCard cc:
                        var stmt = -Accountant.RunGroupedQuery($"({cc.Query})*({curr})-\"\"``v").Fund;
                        var pmt = Accountant.RunGroupedQuery($"({cc.Query})*({curr} \"\" >)``v").Fund;
                        var nxt = -Accountant.RunGroupedQuery($"({cc.Query})*({curr} \"\" <)``v").Fund;
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
}
