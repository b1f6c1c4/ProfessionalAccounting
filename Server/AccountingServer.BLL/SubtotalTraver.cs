using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     分类汇总结果遍历器
    /// </summary>
    /// <typeparam name="TMedium">中间类型</typeparam>
    /// <typeparam name="TResult">输出类型</typeparam>
    public class SubtotalTraver<TMedium, TResult>
    {
        /// <summary>
        ///     原子汇总项处理器
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="cat">分类项</param>
        /// <param name="depth">深度</param>
        /// <param name="val">汇总金额</param>
        /// <returns>输出</returns>
        public delegate TResult LeafNoneAggrFunc(TMedium path, Balance cat, int depth, double val);

        /// <summary>
        ///     原子累加汇总项处理器
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="cat">分类项</param>
        /// <param name="depth">深度</param>
        /// <param name="bal">累加汇总项</param>
        /// <returns>输出</returns>
        public delegate TResult LeafAggregatedFunc(TMedium path, Balance cat, int depth, Balance bal);

        /// <summary>
        ///     路径映射器
        /// </summary>
        /// <param name="path">当前路径</param>
        /// <param name="cat">当前分类项</param>
        /// <param name="depth">当前深度</param>
        /// <param name="level">新增分类项</param>
        /// <returns>新路径</returns>
        public delegate TMedium MapFunc(TMedium path, Balance cat, int depth, SubtotalLevel level);

        /// <summary>
        ///     累加路径映射器
        /// </summary>
        /// <param name="path">当前路径</param>
        /// <param name="cat">当前分类项</param>
        /// <param name="depth">当前深度</param>
        /// <param name="type">累加类型</param>
        /// <returns>新路径</returns>
        public delegate TMedium MapAFunc(TMedium path, Balance cat, int depth, AggregationType type);

        /// <summary>
        ///     中间层汇总项处理器
        /// </summary>
        /// <param name="path">当前路径</param>
        /// <param name="cat">当前分类项</param>
        /// <param name="depth">当前深度</param>
        /// <param name="level">新增分类项</param>
        /// <param name="r">子汇总项输出汇聚</param>
        /// <returns>输出</returns>
        public delegate TResult MediumLevelFunc(
            TMedium path, TMedium newPath, Balance cat, int depth, SubtotalLevel level, TResult r);

        /// <summary>
        ///     中间层汇总项汇聚器
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="cat">分类项</param>
        /// <param name="depth">深度</param>
        /// <param name="level">新增分类项</param>
        /// <param name="results">子汇总项输出</param>
        /// <returns>输出</returns>
        public delegate TResult ReduceFunc(
            TMedium path, Balance cat, int depth, SubtotalLevel level, IEnumerable<TResult> results);

        /// <summary>
        ///     累加汇总项汇聚器
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="cat">分类项</param>
        /// <param name="depth">深度</param>
        /// <param name="type">累加类型</param>
        /// <param name="results">累加汇总项输出</param>
        /// <returns>输出</returns>
        public delegate TResult ReduceAFunc(
            TMedium path, TMedium newPath, Balance cat, int depth, AggregationType type, IEnumerable<TResult> results);

        /// <summary>
        ///     原子汇总项处理器
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public LeafNoneAggrFunc LeafNoneAggr { get; set; }

        /// <summary>
        ///     原子累加汇总项处理器
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public LeafAggregatedFunc LeafAggregated { get; set; }

        /// <summary>
        ///     路径映射器
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public MapFunc Map { get; set; }

        /// <summary>
        ///     累加路径映射器
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public MapAFunc MapA { get; set; }

        /// <summary>
        ///     中间层汇总项处理器
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public MediumLevelFunc MediumLevel { get; set; }

        /// <summary>
        ///     中间层汇总项汇聚器
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public ReduceFunc Reduce { get; set; }

        /// <summary>
        ///     累加汇总项汇聚器
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public ReduceAFunc ReduceA { get; set; }

        /// <summary>
        ///     分类汇总参数
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public ISubtotal SubtotalArgs { get; set; }

        public SubtotalTraver(ISubtotal args) { SubtotalArgs = args; }

        /// <summary>
        ///     遍历分类汇总结果
        /// </summary>
        /// <param name="initialPath">初始路径</param>
        /// <param name="res">分类汇总结果</param>
        /// <returns>输出</returns>
        public TResult Traversal(TMedium initialPath, IEnumerable<Balance> res)
        {
            return TraversalSubtotal(initialPath, res, new Balance(), 0);
        }

        /// <summary>
        ///     遍历某层分类汇总结果
        /// </summary>
        /// <param name="path">当前路径</param>
        /// <param name="res">分类汇总结果</param>
        /// <param name="cat">当前分类项</param>
        /// <param name="depth">当前深度</param>
        /// <returns>输出</returns>
        private TResult TraversalSubtotal(TMedium path, IEnumerable<Balance> res, Balance cat, int depth)
        {
            if (depth >= SubtotalArgs.Levels.Count)
            {
                if (SubtotalArgs.AggrType == AggregationType.None)
                    return LeafNoneAggr(path, cat, depth, res.Sum(b => b.Fund));
                var newPath = MapA(path, cat, depth, SubtotalArgs.AggrType);
                if (SubtotalArgs.AggrType == AggregationType.ChangedDay)
                    return ReduceA(
                                   path,
                                   newPath,
                                   cat,
                                   depth + 1,
                                   SubtotalArgs.AggrType,
                                   Accountant.AggregateChangedDay(res)
                                             .Where(b => !SubtotalArgs.NonZero || !Accountant.IsZero(b.Fund))
                                             .Select(b => LeafAggregated(newPath, cat, depth, b)));
                if (SubtotalArgs.AggrType == AggregationType.EveryDay)
                    return ReduceA(
                                   path,
                                   newPath,
                                   cat,
                                   depth + 1,
                                   SubtotalArgs.AggrType,
                                   Accountant.AggregateEveryDay(res, SubtotalArgs.EveryDayRange.Range)
                                             .Where(b => !SubtotalArgs.NonZero || !Accountant.IsZero(b.Fund))
                                             .Select(b => LeafAggregated(newPath, cat, depth, b)));
                throw new InvalidOperationException();
            }
            // else
            {
                IEnumerable<TResult> resx;
                switch (SubtotalArgs.Levels[depth])
                {
                    case SubtotalLevel.Title:
                        resx = Accountant
                            .GroupByTitle(res)
                            .Select(
                                    grp =>
                                    {
                                        var newCat = new Balance(cat) { Title = grp.Key };
                                        var newPath = Map(path, newCat, depth, SubtotalArgs.Levels[depth]);
                                        return MediumLevel(
                                                           path,
                                                           newPath,
                                                           newCat,
                                                           depth,
                                                           SubtotalArgs.Levels[depth],
                                                           TraversalSubtotal(newPath, grp, newCat, depth + 1));
                                    });
                        break;
                    case SubtotalLevel.SubTitle:
                        resx = Accountant
                            .GroupBySubTitle(res)
                            .Select(
                                    grp =>
                                    {
                                        var newCat = new Balance(cat) { SubTitle = grp.Key };
                                        var newPath = Map(path, newCat, depth, SubtotalArgs.Levels[depth]);
                                        return MediumLevel(
                                                           path,
                                                           newPath,
                                                           newCat,
                                                           depth,
                                                           SubtotalArgs.Levels[depth],
                                                           TraversalSubtotal(newPath, grp, newCat, depth + 1));
                                    });
                        break;
                    case SubtotalLevel.Content:
                        resx = Accountant
                            .GroupByContent(res)
                            .Select(
                                    grp =>
                                    {
                                        var newCat = new Balance(cat) { Content = grp.Key };
                                        var newPath = Map(path, newCat, depth, SubtotalArgs.Levels[depth]);
                                        return MediumLevel(
                                                           path,
                                                           newPath,
                                                           newCat,
                                                           depth,
                                                           SubtotalArgs.Levels[depth],
                                                           TraversalSubtotal(newPath, grp, newCat, depth + 1));
                                    });
                        break;
                    case SubtotalLevel.Remark:
                        resx = Accountant
                            .GroupByRemark(res)
                            .Select(
                                    grp =>
                                    {
                                        var newCat = new Balance(cat) { Remark = grp.Key };
                                        var newPath = Map(path, newCat, depth, SubtotalArgs.Levels[depth]);
                                        return MediumLevel(
                                                           path,
                                                           newPath,
                                                           newCat,
                                                           depth,
                                                           SubtotalArgs.Levels[depth],
                                                           TraversalSubtotal(newPath, grp, newCat, depth + 1));
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
                                        var newCat = new Balance(cat) { Date = grp.Key };
                                        var newPath = Map(path, newCat, depth, SubtotalArgs.Levels[depth]);
                                        return MediumLevel(
                                                           path,
                                                           newPath,
                                                           newCat,
                                                           depth,
                                                           SubtotalArgs.Levels[depth],
                                                           TraversalSubtotal(newPath, grp, newCat, depth + 1));
                                    });
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                return Reduce(path, cat, depth, SubtotalArgs.Levels[depth], resx);
            }
        }
    }
}
