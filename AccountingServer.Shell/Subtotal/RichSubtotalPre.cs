using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Subtotal
{
    /// <summary>
    ///     分类汇总结果处理器
    /// </summary>
    internal class RichSubtotalPre : SubtotalTraver<object, (double Value, string Report)>, ISubtotalPre
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
                return traversal.Report;

            return Ts(traversal.Value) + ":" + Environment.NewLine + traversal.Report;
        }

        protected override (double Value, string Report) LeafNoneAggr(object path, Balance cat, int depth, double val)
            => (Value: val, Report: Ts(val));

        protected override (double Value, string Report) LeafAggregated(object path, Balance cat, int depth, Balance bal)
            => (Value: bal.Fund,
                Report:
                $"{new string(' ', depth * Ident)}{bal.Date.AsDate().CPadRight(38)}{Ts(bal.Fund).CPadLeft(12 + 2 * depth)}"
                );

        protected override object Map(object path, Balance cat, int depth, SubtotalLevel level) => null;
        protected override object MapA(object path, Balance cat, int depth, AggregationType type) => null;

        protected override (double Value, string Report) MediumLevel(object path, object newPath, Balance cat, int depth,
            SubtotalLevel level, (double Value, string Report) r)
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
                return (Value: r.Value,
                    Report: $"{new string(' ', depth * Ident)}{str.CPadRight(38)}{r.Report.CPadLeft(12 + 2 * depth)}");

            return (Value: r.Value,
                Report:
                $"{new string(' ', depth * Ident)}{str.CPadRight(38)}{Ts(r.Value).CPadLeft(12 + 2 * depth)}{Environment.NewLine}{r.Report}"
                );
        }

        protected override (double Value, string Report) Reduce(object path, Balance cat, int depth, SubtotalLevel level,
            IEnumerable<(double Value, string Report)> results)
        {
            var r = results.ToList();
            return (Value: r.Sum(t => t.Value),
                Report: SubtotalPreHelper.NotNullJoin(r.Select(t => t.Report)));
        }

        protected override (double Value, string Report) ReduceA(object path, object newPath, Balance cat, int depth,
            AggregationType type, IEnumerable<(double Value, string Report)> results)
        {
            var r = results.ToList();
            var last = r.LastOrDefault();
            return (Value: r.Any() ? last.Value : 0,
                Report: SubtotalPreHelper.NotNullJoin(r.Select(t => t.Report)));
        }
    }
}
