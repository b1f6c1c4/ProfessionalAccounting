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
    ///     自动计算利息收入和还款
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
            Parsing.Eof(expr);

            var rst = new Dictionary<DateTime, double[,]>();

            var n = Templates.Config.Accounts.Count;
            var until = ClientDateTime.Today.AddMonths(extraMonths);
            for (var i = 0; i < n; i++)
                foreach (var (date, value) in GetItems(Templates.Config.Accounts[i], serializer, until))
                {
                    if (date > until)
                        continue;

                    if (!rst.ContainsKey(date))
                        rst.Add(date, new double[n, 2]);

                    if (value >= 0)
                        rst[date][i, 0] += value;
                    else
                        rst[date][i, 1] += value;
                }

            var aggs = new double[n];

            var sb = new StringBuilder();
            sb.Append("Date    ");
            for (var i = 0; i < n; i++)
            {
                var c = Templates.Config.Accounts[i].Currency;
                sb.Append($" {$"++ {c} ++".PadRight(15)}");
                sb.Append($" {$"-- {c} --".PadRight(15)}");
                sb.Append($" {$"@@ {c} @@".PadRight(15)}");
            }

            sb.AppendLine(" All");

            foreach (var kvp in rst.OrderBy(kvp => kvp.Key))
            {
                sb.Append($"{kvp.Key.AsDate()}");

                var sum = 0D;
                for (var i = 0; i < n; i++)
                {
                    aggs[i] += kvp.Value[i, 0] + kvp.Value[i, 1];

                    sum += aggs[i] * ExchangeFactory.Instance.From(
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

        private DateTime NextDate(int day, DateTime? reference = null)
        {
            var d = reference ?? ClientDateTime.Today;
            var v = new DateTime(d.Year, d.Month, day, 0, 0, 0, DateTimeKind.Utc);
            return d.Day >= day ? v.AddMonths(1) : v;
        }

        private IEnumerable<(DateTime Date, double Value)> GetItems(CashAccount account, IEntitiesSerializer serializer, DateTime until)
        {
            var curr = $"@{account.Currency}";
            var init = Accountant.RunGroupedQuery($"{curr}*({account.QuickAsset}) [~.]``v").Fund;
            yield return (ClientDateTime.Today, init);

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
                        for (var d = NextDate(mn.Day); d <= until; d = d.AddMonths(1))
                            yield return (d, mn.Fund);

                        break;

                    case SimpleCreditCard cc:
                        foreach (var grpC in Accountant.RunGroupedQuery(
                                $"{{{{({cc.Query})*(<+(-{curr}))}}+{{({cc.Query})+T3999+T6603 A}}}}*{{[{ClientDateTime.Today.AddMonths(-3).AsDate()}~]}}:{cc.Query}`Cd")
                            .Items
                            .Cast<ISubtotalCurrency>())
                        foreach (var b in grpC.Items.Cast<ISubtotalDate>())
                        {
                            // ReSharper disable once PossibleInvalidOperationException
                            var d = b.Date.Value;
                            var mo = new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                            if (d.Day >= cc.BillDay)
                                mo = mo.AddMonths(1);
                            if (cc.RepaymentDay <= cc.BillDay)
                                mo = mo.AddMonths(1);
                            mo = mo.AddDays(cc.RepaymentDay - 1);
                            if (mo <= ClientDateTime.Today)
                                continue;

                            var cob = ExchangeFactory.Instance.From(mo, grpC.Currency)
                                * ExchangeFactory.Instance.To(mo, account.Currency)
                                * b.Fund;
                            yield return (mo, cob);
                        }

                        foreach (var b in Accountant.RunGroupedQuery(
                                $"{{({cc.Query})*({curr}>) [{ClientDateTime.Today.AddMonths(-3).AsDate()}~]}}-{{({cc.Query})+T3999+T6603 A}}:{cc.Query}`d")
                            .Items
                            .Cast<ISubtotalDate>())
                        {
                            // ReSharper disable once PossibleInvalidOperationException
                            var d = b.Date.Value;
                            var mo = new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                            if (d.Day > cc.RepaymentDay)
                                mo = mo.AddMonths(1);
                            mo = mo.AddDays(cc.RepaymentDay - 1);
                            if (mo <= ClientDateTime.Today)
                                continue;

                            yield return (mo, b.Fund);
                        }

                        for (var d = ClientDateTime.Today; d <= until; d = NextDate(cc.BillDay, d))
                        {
                            yield return (
                                    NextDate(cc.RepaymentDay, NextDate(cc.BillDay, d)),
                                    (NextDate(cc.BillDay, d) - d).TotalDays * cc.MonthlyFee / (365.2425 / 12)
                                );
                        }

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
