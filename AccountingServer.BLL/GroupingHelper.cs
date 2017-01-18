using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.BLL
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

        /// <summary>
        ///     按检索式对记账凭证执行分类汇总
        /// </summary>
        /// <param name="vouchers">记账凭证</param>
        /// <param name="query">检索式</param>
        /// <returns>分类汇总结果</returns>
        public static IEnumerable<Balance> SelectVoucherDetailsGrouped(this IEnumerable<Voucher> vouchers,
            IGroupedQuery query)
        {
            SubtotalLevel level;
            if (query.Subtotal.AggrType != AggregationType.None)
                level = query.Subtotal.Levels.Aggregate(SubtotalLevel.None, (total, l) => total | l) | SubtotalLevel.Day;
            else
                level = query.Subtotal.Levels.Aggregate(SubtotalLevel.None, (total, l) => total | l);

            Func<Voucher, VoucherDetail, Balance> fullSelector =
                (v, d) =>
                    new Balance
                        {
                            Date = v.Date,
                            Title = d.Title,
                            SubTitle = d.SubTitle,
                            Content = d.Content,
                            Remark = d.Remark,
                            // ReSharper disable once PossibleInvalidOperationException
                            Fund = d.Fund.Value
                        };
            Func<Balance, Balance> keySelector =
                b =>
                    new Balance
                        {
                            Date = level.HasFlag(SubtotalLevel.Day) ? b.Date : null,
                            Title = level.HasFlag(SubtotalLevel.Title) ? b.Title : null,
                            SubTitle = level.HasFlag(SubtotalLevel.SubTitle) ? b.SubTitle : null,
                            Content = level.HasFlag(SubtotalLevel.Content) ? b.Content : null,
                            Remark = level.HasFlag(SubtotalLevel.Remark) ? b.Remark : null
                        };
            Func<Balance, IEnumerable<double>, Balance> reducer =
                (b, bs) =>
                    new Balance
                        {
                            Date = b.Date,
                            Title = b.Title,
                            SubTitle = b.SubTitle,
                            Content = b.Content,
                            Remark = b.Remark,
                            Fund = bs.Sum()
                        };
            if (query.VoucherEmitQuery.DetailEmitFilter == null)
            {
                var ff = query.VoucherEmitQuery.VoucherQuery as IVoucherQueryAtom;
                if (ff == null)
                    throw new ArgumentException("不指定细目映射检索式时记账凭证检索式为复合检索式", nameof(query));

                return
                    vouchers.Where(v => v.IsMatch(ff))
                        .SelectMany(
                            v =>
                                v.Details.Where(d => d.IsMatch(ff.DetailFilter))
                                    .Select(d => fullSelector(v, d)))
                        .GroupBy(keySelector, b => b.Fund, reducer, new BalanceComparer());
            }

            return
                vouchers.Where(v => v.IsMatch(query.VoucherEmitQuery.VoucherQuery))
                    .SelectMany(
                        v =>
                            v.Details.Where(
                                    d =>
                                        d.IsMatch(query.VoucherEmitQuery.DetailEmitFilter.DetailFilter))
                                .Select(d => fullSelector(v, d)))
                    .GroupBy(keySelector, b => b.Fund, reducer, new BalanceComparer());
        }
    }
}
