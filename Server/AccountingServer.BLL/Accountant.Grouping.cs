using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    public partial class Accountant
    {
        public static IEnumerable<IGrouping<int, Balance>> GroupByTitle(IEnumerable<Balance> source)
        {
            return source.GroupBy(b => b.Title.Value);
        }

        public static IEnumerable<IGrouping<int, Balance>> GroupBySubTitle(IEnumerable<Balance> source)
        {
            return source.GroupBy(b => b.SubTitle.Value);
        }

        public static IEnumerable<IGrouping<string, Balance>> GroupByContent(IEnumerable<Balance> source)
        {
            return source.GroupBy(b => b.Content);
        }

        public static IEnumerable<IGrouping<string, Balance>> GroupByRemark(IEnumerable<Balance> source)
        {
            return source.GroupBy(b => b.Remark);
        }

        public static IEnumerable<IGrouping<DateTime?, Balance>> GroupByDate(IEnumerable<Balance> source)
        {
            return source.GroupBy(b => b.Date);
        }


        /// <summary>
        ///     计算每日累计发生额
        /// </summary>
        /// <param name="source">每日发生额</param>
        /// <returns>每日余额</returns>
        public static IEnumerable<Balance> GroupByDateAggr(IEnumerable<Balance> source)
        {
            var resx =
                GroupByDate(source)
                    .Select(grp => new KeyValuePair<DateTime?, double>(grp.Key, grp.Sum(b => b.Fund)))
                    .ToList();
            resx.Sort((d1, d2) => DateHelper.CompareDate(d1.Key, d2.Key));

            var fund = 0D;
            foreach (var kvp in resx)
            {
                fund += kvp.Value;
                yield return new Balance
                                 {
                                     Date = kvp.Key,
                                     Fund = fund
                                 };
            }
        }

        /// <summary>
        ///     计算每日余额
        /// </summary>
        /// <param name="source">每日发生额</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>每日余额</returns>
        public static IEnumerable<Balance> GroupByDateBal(IEnumerable<Balance> source, DateFilter rng)
        {
            var resx =
                GroupByDate(source)
                    .Select(grp => new KeyValuePair<DateTime?, double>(grp.Key, grp.Sum(b => b.Fund)))
                    .ToList();
            resx.Sort((d1, d2) => DateHelper.CompareDate(d1.Key, d2.Key));

            var id = 0;
            DateTime dt;
            if (rng.StartDate.HasValue)
                dt = rng.StartDate.Value;
            else if (resx.Any(b => b.Key.HasValue))
                // ReSharper disable once PossibleInvalidOperationException
                dt = resx.First(b => b.Key.HasValue).Key.Value;
            else
            {
                if (resx.Any())
                    yield return new Balance { Date = null, Fund = resx.Sum(b => b.Value) };
                yield break;
            }

            // ReSharper disable once PossibleInvalidOperationException
            var last = rng.EndDate ?? resx.Last().Key.Value;

            var fund = 0D;
            for (; dt <= last; dt = dt.AddDays(1))
            {
                while (id < resx.Count &&
                       DateHelper.CompareDate(resx[id].Key, dt) <= 0)
                    fund += resx[id++].Value;

                yield return
                    new Balance
                        {
                            Date = dt,
                            Fund = fund
                        };
            }
        }
    }
}
