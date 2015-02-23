using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     分类汇总结果遍历器
    /// </summary>
    /// <typeparam name="TResult">输出类型</typeparam>
    public class SubtotalTraver<TResult>
    {
        /// <summary>
        ///     原子汇总项处理器
        /// </summary>
        /// <param name="cat">分类项</param>
        /// <param name="depth">深度</param>
        /// <param name="val">汇总金额</param>
        /// <returns>输出</returns>
        public delegate TResult LeafNoneAggrFunc(Balance cat, int depth, double val);

        /// <summary>
        ///     原子累加汇总项处理器
        /// </summary>
        /// <param name="cat">分类项</param>
        /// <param name="depth">深度</param>
        /// <param name="bal">累加汇总项</param>
        /// <returns>输出</returns>
        public delegate TResult LeafAggregatedFunc(Balance cat, int depth, Balance bal);

        /// <summary>
        ///     中间层汇总项处理器
        /// </summary>
        /// <param name="cat">分类项</param>
        /// <param name="depth">深度</param>
        /// <param name="level">新增分类项</param>
        /// <param name="res">子汇总项输出汇聚</param>
        /// <returns>输出</returns>
        public delegate TResult MediumLevelFunc(Balance cat, int depth, SubtotalLevel level, TResult res);

        /// <summary>
        ///     中间层汇总项汇聚器
        /// </summary>
        /// <param name="cat">分类项</param>
        /// <param name="depth">深度</param>
        /// <param name="level">新增分类项</param>
        /// <param name="results">子汇总项输出</param>
        /// <returns>输出</returns>
        public delegate TResult ReduceFunc(Balance cat, int depth, SubtotalLevel level, IEnumerable<TResult> results);

        /// <summary>
        ///     累加汇总项汇聚器
        /// </summary>
        /// <param name="cat">分类项</param>
        /// <param name="depth">深度</param>
        /// <param name="type">累加类型</param>
        /// <param name="results">累加汇总项输出</param>
        /// <returns>输出</returns>
        public delegate TResult ReduceAFunc(Balance cat, int depth, AggregationType type, IEnumerable<TResult> results);

        /// <summary>
        ///     原子汇总项处理器
        /// </summary>
        public LeafNoneAggrFunc LeafNoneAggr { get; set; }

        /// <summary>
        ///     原子累加汇总项处理器
        /// </summary>
        public LeafAggregatedFunc LeafAggregated { get; set; }

        /// <summary>
        ///     中间层汇总项处理器
        /// </summary>
        public MediumLevelFunc MediumLevel { get; set; }

        /// <summary>
        ///     中间层汇总项汇聚器
        /// </summary>
        public ReduceFunc Reduce { get; set; }

        /// <summary>
        ///     累加汇总项汇聚器
        /// </summary>
        public ReduceAFunc ReduceA { get; set; }

        /// <summary>
        ///     分类汇总参数
        /// </summary>
        public ISubtotal SubtotalArgs { get; set; }

        public SubtotalTraver(ISubtotal args) { SubtotalArgs = args; }

        /// <summary>
        ///     遍历分类汇总结果
        /// </summary>
        /// <param name="res">分类汇总结果</param>
        /// <returns>输出</returns>
        public TResult Traversal(IEnumerable<Balance> res) { return TraversalSubtotal(res, new Balance(), 0); }

        /// <summary>
        ///     遍历某层分类汇总结果
        /// </summary>
        /// <param name="res">分类汇总结果</param>
        /// <param name="cat">分类项</param>
        /// <param name="depth">深度</param>
        /// <returns>输出</returns>
        private TResult TraversalSubtotal(IEnumerable<Balance> res, Balance cat, int depth)
        {
            if (depth >= SubtotalArgs.Levels.Count)
            {
                if (SubtotalArgs.AggrType == AggregationType.None)
                    return LeafNoneAggr(cat, depth, res.Sum(b => b.Fund));
                if (SubtotalArgs.AggrType == AggregationType.ChangedDay)
                    return ReduceA(
                                   cat,
                                   depth + 1,
                                   SubtotalArgs.AggrType,
                                   Accountant.AggregateChangedDay(res)
                                             .Where(b => !SubtotalArgs.NonZero || !Accountant.IsZero(b.Fund))
                                             .Select(b => LeafAggregated(cat, depth, b)));
                if (SubtotalArgs.AggrType == AggregationType.EveryDay)
                    return ReduceA(
                                   cat,
                                   depth + 1,
                                   SubtotalArgs.AggrType,
                                   Accountant.AggregateEveryDay(res, SubtotalArgs.EveryDayRange.Range)
                                             .Where(b => !SubtotalArgs.NonZero || !Accountant.IsZero(b.Fund))
                                             .Select(b => LeafAggregated(cat, depth, b)));
                throw new InvalidOperationException();
            }

            IEnumerable<TResult> resx;
            switch (SubtotalArgs.Levels[depth])
            {
                case SubtotalLevel.Title:
                    resx = Accountant
                        .GroupByTitle(res)
                        .Select(
                                grp =>
                                {
                                    var newCat = new Balance
                                                     {
                                                         Date = cat.Date,
                                                         Title = grp.Key,
                                                         SubTitle = cat.SubTitle,
                                                         Content = cat.Content,
                                                         Remark = cat.Remark
                                                     };
                                    return MediumLevel(
                                                       newCat,
                                                       depth,
                                                       SubtotalArgs.Levels[depth],
                                                       TraversalSubtotal(grp, newCat, depth + 1));
                                });
                    break;
                case SubtotalLevel.SubTitle:
                    resx = Accountant
                        .GroupBySubTitle(res)
                        .Select(
                                grp =>
                                {
                                    var newCat = new Balance
                                                     {
                                                         Date = cat.Date,
                                                         Title = cat.Title,
                                                         SubTitle = grp.Key,
                                                         Content = cat.Content,
                                                         Remark = cat.Remark
                                                     };
                                    return MediumLevel(
                                                       newCat,
                                                       depth,
                                                       SubtotalArgs.Levels[depth],
                                                       TraversalSubtotal(grp, newCat, depth + 1));
                                });
                    break;
                case SubtotalLevel.Content:
                    resx = Accountant
                        .GroupByContent(res)
                        .Select(
                                grp =>
                                {
                                    var newCat = new Balance
                                                     {
                                                         Date = cat.Date,
                                                         Title = cat.Title,
                                                         SubTitle = cat.SubTitle,
                                                         Content = grp.Key,
                                                         Remark = cat.Remark
                                                     };
                                    return MediumLevel(
                                                       newCat,
                                                       depth,
                                                       SubtotalArgs.Levels[depth],
                                                       TraversalSubtotal(grp, newCat, depth + 1));
                                });
                    break;
                case SubtotalLevel.Remark:
                    resx = Accountant
                        .GroupByRemark(res)
                        .Select(
                                grp =>
                                {
                                    var newCat = new Balance
                                                     {
                                                         Date = cat.Date,
                                                         Title = cat.Title,
                                                         SubTitle = cat.SubTitle,
                                                         Content = cat.Content,
                                                         Remark = grp.Key
                                                     };
                                    return MediumLevel(
                                                       newCat,
                                                       depth,
                                                       SubtotalArgs.Levels[depth],
                                                       TraversalSubtotal(grp, newCat, depth + 1));
                                });
                    break;
                case SubtotalLevel.Day:
                case SubtotalLevel.Week:
                case SubtotalLevel.FinancialMonth:
                case SubtotalLevel.Month:
                case SubtotalLevel.BillingMonth:
                case SubtotalLevel.Year:
                    resx = Accountant
                        .GroupByDate(res)
                        .Select(
                                grp =>
                                {
                                    var newCat = new Balance
                                                     {
                                                         Date = grp.Key,
                                                         Title = cat.Title,
                                                         SubTitle = cat.SubTitle,
                                                         Content = cat.Content,
                                                         Remark = cat.Remark
                                                     };
                                    return MediumLevel(
                                                       newCat,
                                                       depth,
                                                       SubtotalArgs.Levels[depth],
                                                       TraversalSubtotal(grp, newCat, depth + 1));
                                });
                    break;
                default:
                    throw new InvalidOperationException();
            }
            return Reduce(cat, depth, SubtotalArgs.Levels[depth], resx);
        }
    }
}
