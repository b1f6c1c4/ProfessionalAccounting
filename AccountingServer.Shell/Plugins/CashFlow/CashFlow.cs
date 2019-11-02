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
            var rst = new Dictionary<DateTime, double[]>();

            var n = Templates.Config.Accounts.Count;
            for (var i = 0; i < n; i++)
                foreach (var (date, value) in GetItems(Templates.Config.Accounts[i], serializer))
                {
                    if (!rst.ContainsKey(date))
                        rst.Add(date, new double[n]);

                    rst[date][i] += value;
                }

            var aggs = new double[n];

            var sb = new StringBuilder();
            sb.Append("Date    ");
            for (var i = 0; i < n; i++)
                sb.Append($" {Templates.Config.Accounts[i].Currency.PadRight(15)} Sum            ");

            sb.AppendLine(" All");

            foreach (var kvp in rst.OrderBy(kvp => kvp.Key))
            {
                sb.Append($"{kvp.Key.AsDate()}");

                var sum = 0D;
                for (var i = 0; i < n; i++)
                {
                    aggs[i] += kvp.Value[i];
                    sum += aggs[i] * ExchangeFactory.Instance.From(
                        kvp.Key,
                        Templates.Config.Accounts[i].Currency);
                    sb.Append(kvp.Value[i].AsCurrency(Templates.Config.Accounts[i].Currency).PadLeft(15));
                    sb.Append(aggs[i].AsCurrency(Templates.Config.Accounts[i].Currency).PadLeft(15));
                }

                sb.AppendLine(sum.AsCurrency(BaseCurrency.Now).PadLeft(15));
            }

            return new PlainText(sb.ToString());
        }

        private IEnumerable<(DateTime Date, double Value)> GetItems(CashAccount account, IEntitiesSerializer serializer)
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
                        var d = new DateTime(ClientDateTime.Today.Year, ClientDateTime.Today.Month, mn.Day, 0, 0, 0, DateTimeKind.Utc)
                            .AddMonth(ClientDateTime.Today.Day >= mn.Day ? 1 : 0);
                        for (var i = 0; i < 6; i++)
                        {
                            yield return d;
                            d = d.AddMonth(1);
                        }
                        break;

                    case CreditCard cd:
                        foreach (var grpC in Accountant.RunGroupedQuery(
                                $"{{{{({cd.Query})*(<+(-{curr}))}}+{{({cd.Query})+T3999+T6603 A}}}}*{{[{ClientDateTime.Today.AddMonths(-3).AsDate()}~]}}:{cd.Query}`Cd")
                            .Items
                            .Cast<ISubtotalCurrency>())
                        foreach (var b in grpC.Items.Cast<ISubtotalDate>())
                        {
                            // ReSharper disable once PossibleInvalidOperationException
                            var d = b.Date.Value;
                            var mo = new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                            if (d.Day >= cd.BillDay)
                                mo = mo.AddMonths(1);
                            if (cd.RepaymentDay <= cd.BillDay)
                                mo = mo.AddMonths(1);
                            mo = mo.AddDays(cd.RepaymentDay - 1);
                            if (mo <= ClientDateTime.Today)
                                continue;

                            var cob = ExchangeFactory.Instance.From(mo, grpC.Currency)
                                * ExchangeFactory.Instance.To(mo, account.Currency)
                                * b.Fund;
                            yield return (mo, cob);
                        }

                        foreach (var b in Accountant.RunGroupedQuery(
                                $"{{({cd.Query})*({curr}>) [{ClientDateTime.Today.AddMonths(-3).AsDate()}~]}}-{{({cd.Query})+T3999+T6603 A}}:{cd.Query}`d")
                            .Items
                            .Cast<ISubtotalDate>())
                        {
                            // ReSharper disable once PossibleInvalidOperationException
                            var d = b.Date.Value;
                            var mo = new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                            if (d.Day > cd.RepaymentDay)
                                mo = mo.AddMonths(1);
                            mo = mo.AddDays(cd.RepaymentDay - 1);
                            if (mo <= ClientDateTime.Today)
                                continue;

                            yield return (mo, b.Fund);
                        }

                        break;

                    default:
                        throw new InvalidOperationException();
                }
        }
    }
}
