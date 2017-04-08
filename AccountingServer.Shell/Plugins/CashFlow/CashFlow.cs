using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.BLL.Util;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Carry;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;

namespace AccountingServer.Shell.Plugins.CashFlow
{
    /// <summary>
    ///     自动计算利息收入和还款
    /// </summary>
    internal class CashFlow : PluginBase
    {
        private static readonly ConfigManager<CashTemplates> Templates =
            new ConfigManager<CashTemplates>("Cash.xml");

        public CashFlow(Accountant accountant, IEntitySerializer serializer) : base(accountant, serializer) { }

        /// <inheritdoc />
        public override IQueryResult Execute(string expr)
        {
            var lst = new List<(DateTime Date, double Value)>();

            var init = Accountant.RunGroupedQuery($"{Templates.Config.QuickAsset} [~.]``v").Single().Fund;
            lst.Add((DateTime.Today, init));

            var rb = new Reimburse.Reimburse(Accountant, Serializer);
            rb.DoReimbursement(Reimburse.Reimburse.DateRange, out var rbVal);
            // ReSharper disable once PossibleInvalidOperationException
            var rbF = Reimburse.Reimburse.DateRange.EndDate.Value;
            lst.Add((rbF, rbVal));

            foreach (var debt in Templates.Config.Debts)
                switch (debt)
                {
                    case SimpleDebt sd:
                        lst.Add((sd.Day, Accountant.RunGroupedQuery($"{sd.Query}``v").Single().Fund));
                        break;

                    case CreditCard cd:
                        foreach (var b in Accountant.RunGroupedQuery(
                            $"({cd.Query})*(<+(-@@)) [{DateTime.Today.AddMonths(-3).AsDate()}~]`Cd"))
                        {
                            // ReSharper disable once PossibleInvalidOperationException
                            var d = b.Date.Value;
                            var mo = new DateTime(d.Year, d.Month, 1);
                            if (d.Day >= cd.BillDay)
                                mo = mo.AddMonths(1);
                            if (cd.RepaymentDay <= cd.BillDay)
                                mo = mo.AddMonths(1);
                            mo = mo.AddDays(cd.RepaymentDay - 1);
                            if (mo <= DateTime.Today)
                                continue;

                            var ratio = ExchangeFactory.Instance.From(mo, b.Currency);
                            lst.Add((mo, b.Fund * ratio));
                        }

                        break;

                    default:
                        throw new InvalidOperationException();
                }

            var sb = new StringBuilder();
            var agg = 0D;
            foreach (var item in lst.GroupBy(
                    item => item.Date,
                    item => item.Value,
                    (d, vs) => (Date:d, Value:vs.Sum()))
                .OrderBy(item => item.Date))
            {
                agg += item.Value;
                sb.AppendLine($"{item.Date.AsDate()}\t{item.Value:R}\t{agg:R}");
            }

            return new UnEditableText(sb.ToString());
        }
    }
}
