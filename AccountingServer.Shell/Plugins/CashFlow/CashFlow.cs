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
        public static IConfigManager<CashTemplates> Templates { private get; set; } =
            new ConfigManager<CashTemplates>("Cash.xml");

        public CashFlow(Accountant accountant, IEntitySerializer serializer) : base(accountant, serializer) { }

        /// <inheritdoc />
        public override IQueryResult Execute(string expr)
        {
            var rst = new Dictionary<DateTime, double[]>();

            var n = Templates.Config.Accounts.Count;
            for (var i = 0; i < n; i++)
                foreach (var item in GetItems(Templates.Config.Accounts[i]))
                {
                    if (!rst.ContainsKey(item.Date))
                        rst.Add(item.Date, new double[n]);

                    rst[item.Date][i] += item.Value;
                }

            var aggs = new double[n];

            var sb = new StringBuilder();
            sb.Append("Date");
            for (var i = 0; i < n; i++)
                sb.Append($"\t{Templates.Config.Accounts[i].Currency}\tSum");

            sb.AppendLine("\tAll");

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
                    sb.Append($"\t{kvp.Value[i]:R}\t{aggs[i]:R}");
                }

                sb.AppendLine($"\t{sum:R}");
            }

            return new UnEditableText(sb.ToString());
        }

        private IEnumerable<(DateTime Date, double Value)> GetItems(CashAccount account)
        {
            var curr = string.IsNullOrEmpty(account.Currency) ? "@@" : $"@{account.Currency}";
            var init = Accountant.RunGroupedQuery($"{curr}*({account.QuickAsset}) [~.]``v").Fund;
            yield return (DateTime.Today.CastUtc(), init);

            if (account.Reimburse != null)
            {
                var rb = new Composite.Composite(Accountant, Serializer);
                var tmp = Composite.Composite.GetTemplate(account.Reimburse);
                var rng = Composite.Composite.DateRange(tmp.Day);
                rb.DoInquiry(rng, tmp, out var rbVal);
                // ReSharper disable once PossibleInvalidOperationException
                var rbF = rng.EndDate.Value;
                yield return (rbF, rbVal);
            }

            foreach (var debt in account.Items)
                switch (debt)
                {
                    case FixedItem fi:
                        yield return (fi.Day, fi.Fund);

                        break;

                    case SimpleItem sd:
                        yield return (sd.Day, Accountant.RunGroupedQuery($"{curr}*({sd.Query})``v").Fund);

                        break;

                    case CreditCard cd:
                        foreach (var grpC in Accountant.RunGroupedQuery(
                                $"({cd.Query})*(<+(-@@)) [{DateTime.Today.CastUtc().AddMonths(-3).AsDate()}~]`Cd").Items
                            .Cast<ISubtotalCurrency>())
                        foreach (var b in grpC.Items.Cast<ISubtotalDate>())
                        {
                            // ReSharper disable once PossibleInvalidOperationException
                            var d = b.Date.Value;
                            var mo = new DateTime(d.Year, d.Month, 1).CastUtc();
                            if (d.Day >= cd.BillDay)
                                mo = mo.AddMonths(1);
                            if (cd.RepaymentDay <= cd.BillDay)
                                mo = mo.AddMonths(1);
                            mo = mo.AddDays(cd.RepaymentDay - 1);
                            if (mo <= DateTime.Today.CastUtc())
                                continue;

                            var ratio = ExchangeFactory.Instance.From(mo, grpC.Currency);
                            yield return (mo, b.Fund * ratio);
                        }

                        foreach (var b in Accountant.RunGroupedQuery(
                                $"({cd.Query})*(@@>) [{DateTime.Today.CastUtc().AddMonths(-3).AsDate()}~]`d").Items
                            .Cast<ISubtotalDate>())
                        {
                            // ReSharper disable once PossibleInvalidOperationException
                            var d = b.Date.Value;
                            var mo = new DateTime(d.Year, d.Month, 1).CastUtc();
                            if (d.Day > cd.RepaymentDay)
                                mo = mo.AddMonths(1);
                            mo = mo.AddDays(cd.RepaymentDay - 1);
                            if (mo <= DateTime.Today.CastUtc())
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
