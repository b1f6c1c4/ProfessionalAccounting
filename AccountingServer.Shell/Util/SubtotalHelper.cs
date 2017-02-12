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
    internal class SubtotalHelper : SubtotalTraver<object, Tuple<double, string>>
    {
        private const int Ident = 4;

        private readonly IEnumerable<Balance> m_Res;

        /// <summary>
        ///     呈现分类汇总
        /// </summary>
        /// <param name="res">分类汇总结果</param>
        /// <param name="args">分类汇总参数</param>
        public SubtotalHelper(IEnumerable<Balance> res, ISubtotal args)
        {
            m_Res = res;
            SubtotalArgs = args;
        }

        private string Ts(double f) => SubtotalArgs.GatherType == GatheringType.Count
            ? f.ToString("N0")
            : f.AsCurrency();

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
        ///     执行分类汇总
        /// </summary>
        /// <returns>分类汇总结果</returns>
        public string PresentSubtotal()
        {
            var traversal = Traversal(null, m_Res);

            if (SubtotalArgs.Levels.Count == 0 &&
                SubtotalArgs.AggrType == AggregationType.None)
                return traversal.Item2;

            return Ts(traversal.Item1) + ":" + Environment.NewLine + traversal.Item2;
        }

        protected override Tuple<double, string> LeafNoneAggr(object path, Balance cat, int depth, double val)
            => new Tuple<double, string>(val, Ts(val));

        protected override Tuple<double, string> LeafAggregated(object path, Balance cat, int depth, Balance bal) =>
            new Tuple<double, string>(
                bal.Fund,
                $"{new string(' ', depth * Ident)}{bal.Date.AsDate().CPadRight(38)}{Ts(bal.Fund).CPadLeft(12 + 2 * depth)}");

        protected override object Map(object path, Balance cat, int depth, SubtotalLevel level) => null;
        protected override object MapA(object path, Balance cat, int depth, AggregationType type) => null;

        protected override Tuple<double, string> MediumLevel(object path, object newPath, Balance cat, int depth,
            SubtotalLevel level, Tuple<double, string> r)
        {
            string str;
            switch (level)
            {
                case SubtotalLevel.Title:
                    str = $"{cat.Title.AsTitle()} {TitleManager.GetTitleName(cat.Title)}:";
                    break;
                case SubtotalLevel.SubTitle:
                    str = $"{cat.SubTitle.AsSubTitle()} {TitleManager.GetTitleName(cat.Title, cat.SubTitle)}:";
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

            if (depth == SubtotalArgs.Levels.Count - 1 &&
                SubtotalArgs.AggrType == AggregationType.None)
                return new Tuple<double, string>(
                    r.Item1,
                    $"{new string(' ', depth * Ident)}{str.CPadRight(38)}{r.Item2.CPadLeft(12 + 2 * depth)}");

            return new Tuple<double, string>(
                r.Item1,
                $"{new string(' ', depth * Ident)}{str.CPadRight(38)}{Ts(r.Item1).CPadLeft(12 + 2 * depth)}{Environment.NewLine}{r.Item2}");
        }

        protected override Tuple<double, string> Reduce(object path, Balance cat, int depth, SubtotalLevel level,
            IEnumerable<Tuple<double, string>> results)
        {
            var r = results.ToList();
            return new Tuple<double, string>(
                r.Sum(t => t.Item1),
                NotNullJoin(r.Select(t => t.Item2)));
        }

        protected override Tuple<double, string> ReduceA(object path, object newPath, Balance cat, int depth,
            AggregationType type, IEnumerable<Tuple<double, string>> results)
        {
            var r = results.ToList();
            var last = r.LastOrDefault();
            return new Tuple<double, string>(
                last?.Item1 ?? 0,
                NotNullJoin(r.Select(t => t.Item2)));
        }
    }
}
