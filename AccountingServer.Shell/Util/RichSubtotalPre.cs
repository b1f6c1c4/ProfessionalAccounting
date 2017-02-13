using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using static AccountingServer.Shell.Util.SubtotalPreHelper;

namespace AccountingServer.Shell.Util
{
    /// <summary>
    ///     分类汇总结果处理器
    /// </summary>
    internal class RichSubtotalPre : SubtotalTraver<object, Tuple<double, string>>, ISubtotalPre
    {
        private const int Ident = 4;

        private string Ts(double f) => SubtotalArgs.GatherType == GatheringType.Count
            ? f.ToString("N0")
            : f.AsCurrency();

        /// <inheritdoc />
        public string PresentSubtotal(IEnumerable<Balance> res)
        {
            var traversal = Traversal(null, res);

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
