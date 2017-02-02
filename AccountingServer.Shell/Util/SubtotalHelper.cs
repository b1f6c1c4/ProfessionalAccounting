using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Util
{
    /// <summary>
    ///     分类汇总结果处理器
    /// </summary>
    internal static class SubtotalHelper
    {
        /// <summary>
        ///     用换行回车连接非空字符串
        /// </summary>
        /// <param name="strings">字符串</param>
        /// <returns>新字符串，如无非空字符串则为空</returns>
        private static string NotNullJoin(IEnumerable<string> strings)
        {
            var flag = false;

            var sb = new StringBuilder();
            foreach (var s in strings)
            {
                if (s == null)
                    continue;

                if (sb.Length > 0)
                    sb.AppendLine();
                sb.Append(s);
                flag = true;
            }

            return flag ? sb.ToString() : null;
        }

        /// <summary>
        ///     呈现分类汇总
        /// </summary>
        /// <param name="res">分类汇总结果</param>
        /// <param name="args">分类汇总参数</param>
        /// <returns>分类汇总结果</returns>
        public static string PresentSubtotal(IEnumerable<Balance> res, ISubtotal args)
        {
            const int ident = 4;

            Func<double, string> ts = f => (args.GatherType == GatheringType.Count
                ? f.ToString("N0")
                : f.AsCurrency());

            var helper =
                new SubtotalTraver<object, Tuple<double, string>>
                    {
                        SubtotalArgs = args,
                        LeafNoneAggr =
                            (path, cat, depth, val) =>
                                new Tuple<double, string>(val, ts(val)),
                        LeafAggregated =
                            (path, cat, depth, bal) =>
                                new Tuple<double, string>(
                                    bal.Fund,
                                    $"{new string(' ', depth * ident)}{bal.Date.AsDate().CPadRight(38)}{ts(bal.Fund).CPadLeft(12 + 2 * depth)}"),
                        Map = (path, cat, depth, level) => null,
                        MapA = (path, cat, depth, type) => null,
                        MediumLevel =
                            (path, newPath, cat, depth, level, r) =>
                            {
                                string str;
                                switch (level)
                                {
                                    case SubtotalLevel.Title:
                                        str = $"{cat.Title.AsTitle()} {TitleManager.GetTitleName(cat.Title)}:";
                                        break;
                                    case SubtotalLevel.SubTitle:
                                        str =
                                            $"{cat.SubTitle.AsSubTitle()} {TitleManager.GetTitleName(cat.Title, cat.SubTitle)}:";
                                        break;
                                    case SubtotalLevel.Content:
                                        str = $"{cat.Content}:";
                                        break;
                                    case SubtotalLevel.Remark:
                                        str = $"{cat.Remark}:";
                                        break;
                                    case SubtotalLevel.Currency:
                                        str = $"@{cat.Currency}:";
                                        break;
                                    default:
                                        str = $"{cat.Date.AsDate(level)}:";
                                        break;
                                }

                                if (depth == args.Levels.Count - 1 &&
                                    args.AggrType == AggregationType.None)
                                    return new Tuple<double, string>(
                                        r.Item1,
                                        $"{new string(' ', depth * ident)}{str.CPadRight(38)}{r.Item2.CPadLeft(12 + 2 * depth)}");

                                return new Tuple<double, string>(
                                    r.Item1,
                                    $"{new string(' ', depth * ident)}{str.CPadRight(38)}{ts(r.Item1).CPadLeft(12 + 2 * depth)}{Environment.NewLine}{r.Item2}");
                            },
                        Reduce =
                            (path, cat, depth, level, results) =>
                            {
                                var r = results.ToList();
                                return new Tuple<double, string>(
                                    r.Sum(t => t.Item1),
                                    NotNullJoin(r.Select(t => t.Item2)));
                            },
                        ReduceA =
                            (path, newPath, cat, depth, level, results) =>
                            {
                                var r = results.ToList();
                                var last = r.LastOrDefault();
                                return new Tuple<double, string>(
                                    last?.Item1 ?? 0,
                                    NotNullJoin(r.Select(t => t.Item2)));
                            }
                    };

            var traversal = helper.Traversal(null, res);

            if (args.Levels.Count == 0 &&
                args.AggrType == AggregationType.None)
                return traversal.Item2;

            return ts(traversal.Item1) + ":" + Environment.NewLine + traversal.Item2;
        }
    }
}
