using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;

namespace AccountingServer.Shell.Plugins.YieldRate
{
    /// <summary>
    ///     实际收益率计算
    /// </summary>
    internal class YieldRate : PluginBase
    {
        public YieldRate(Accountant accountant) : base(accountant) { }

        /// <inheritdoc />
        public override IQueryResult Execute(string expr, IEntitiesSerializer serializer)
        {
            FacadeF.ParsingF.Eof(expr);

            var result = Accountant.RunGroupedQuery("T1101+T611102+T1501+T611106 G``cd");
            var resx = Accountant.RunGroupedQuery("T1101+T1501``c");
            var sb = new StringBuilder();
            foreach (
                var (grp, rte) in
                result.Items.Cast<ISubtotalContent>()
                    .Join(
                        resx.Items.Cast<ISubtotalContent>(),
                        grp => grp.Content,
                        rsx => rsx.Content,
                        (grp, bal) => (Group: grp,
                            Rate: GetRate(
                                grp.Items.Cast<ISubtotalDate>().OrderBy(b => b.Date, new DateComparer()).ToList(),
                                bal.Fund)))
                    .OrderByDescending(kvp => kvp.Rate))
                sb.AppendLine($"{grp.Content.CPadRight(30)} {$"{rte * 360:P2}".PadLeft(7)}");

            return new PlainText(sb.ToString());
        }

        /// <summary>
        ///     计算实际收益率
        /// </summary>
        /// <param name="lst">现金流</param>
        /// <param name="pv">现值</param>
        /// <returns>实际收益率</returns>
        private static double GetRate(IReadOnlyList<ISubtotalDate> lst, double pv)
        {
            // ReSharper disable PossibleInvalidOperationException
            if (!pv.IsZero())
                return
                    new YieldRateSolver(
                        lst.Select(b => ClientDateTime.Today.Subtract(b.Date.Value).TotalDays).Concat(new[] { 0D }),
                        lst.Select(b => b.Fund).Concat(new[] { -pv })).Solve();

            return
                new YieldRateSolver(
                    lst.Select(b => lst.Last().Date.Value.Subtract(b.Date.Value).TotalDays),
                    lst.Select(b => b.Fund)).Solve();
            // ReSharper restore PossibleInvalidOperationException
        }
    }
}
