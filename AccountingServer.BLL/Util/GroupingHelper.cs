using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.BLL.Util
{
    public static class GroupingHelper
    {
        /// <summary>
        ///     按一级科目分类
        /// </summary>
        /// <param name="source">待分类的余额表条目</param>
        /// <returns>类</returns>
        public static IEnumerable<IGrouping<int?, Balance>> GroupByTitle(this IEnumerable<Balance> source)
            => source.GroupBy(b => b.Title);

        /// <summary>
        ///     按二级科目分类
        /// </summary>
        /// <param name="source">待分类的余额表条目</param>
        /// <returns>类</returns>
        public static IEnumerable<IGrouping<int?, Balance>> GroupBySubTitle(this IEnumerable<Balance> source)
            => source.GroupBy(b => b.SubTitle);

        /// <summary>
        ///     按内容分类
        /// </summary>
        /// <param name="source">待分类的余额表条目</param>
        /// <returns>类</returns>
        public static IEnumerable<IGrouping<string, Balance>> GroupByContent(this IEnumerable<Balance> source)
            => source.GroupBy(b => b.Content);

        /// <summary>
        ///     按备注分类
        /// </summary>
        /// <param name="source">待分类的余额表条目</param>
        /// <returns>类</returns>
        public static IEnumerable<IGrouping<string, Balance>> GroupByRemark(this IEnumerable<Balance> source)
            => source.GroupBy(b => b.Remark);

        /// <summary>
        ///     按币种分类
        /// </summary>
        /// <param name="source">待分类的余额表条目</param>
        /// <returns>类</returns>
        public static IEnumerable<IGrouping<string, Balance>> GroupByCurrency(this IEnumerable<Balance> source)
            => source.GroupBy(b => b.Currency);

        /// <summary>
        ///     按日期分类
        /// </summary>
        /// <param name="source">待分类的余额表条目</param>
        /// <returns>类</returns>
        public static IEnumerable<IGrouping<DateTime?, Balance>> GroupByDate(this IEnumerable<Balance> source)
            => source.GroupBy(b => b.Date);


        /// <summary>
        ///     计算变动日累计发生额
        /// </summary>
        /// <param name="source">变动日发生额</param>
        /// <returns>变动日余额</returns>
        public static IEnumerable<Balance> AggregateChangedDay(this IEnumerable<Balance> source)
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
        ///     计算每日累计发生额
        /// </summary>
        /// <param name="source">变动日发生额</param>
        /// <param name="rng">返回区间</param>
        /// <returns>每日余额</returns>
        public static IEnumerable<Balance> AggregateEveryDay(this IEnumerable<Balance> source, DateFilter rng)
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
