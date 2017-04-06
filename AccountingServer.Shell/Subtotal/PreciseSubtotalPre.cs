using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Subtotal
{
    /// <summary>
    ///     报告结果处理器
    /// </summary>
    internal class PreciseSubtotalPre : SubtotalTraver<string, (double Value, string Report)>, ISubtotalPre
    {
        /// <summary>
        ///     是否包含分类汇总
        /// </summary>
        private readonly bool m_WithSubtotal;

        /// <inheritdoc />
        public string PresentSubtotal(IEnumerable<Balance> res)
        {
            var traversal = Traversal("", res);
            return traversal.Report;
        }

        public PreciseSubtotalPre(bool withSubtotal = true) => m_WithSubtotal = withSubtotal;

        /// <summary>
        ///     使用分隔符连接字符串
        /// </summary>
        /// <param name="path">原字符串</param>
        /// <param name="token">要连接上的字符串</param>
        /// <param name="interval">分隔符</param>
        /// <returns>新字符串</returns>
        private static string Merge(string path, string token, string interval = "-")
        {
            if (path.Length == 0)
                return token;

            return path + interval + token;
        }

        /// <summary>
        ///     汇聚报告
        /// </summary>
        /// <param name="path">当前路径</param>
        /// <param name="results">次级查询输出</param>
        /// <param name="withSubtotal">是否包含分类汇总</param>
        /// <returns>报告</returns>
        private static (double Value, string Report) Gather(string path,
            IEnumerable<(double Value, string Report)> results,
            bool withSubtotal = true)
        {
            var r = results.ToList();
            var val = r.Sum(t => t.Value);
            var report = SubtotalPreHelper.NotNullJoin(r.Select(t => t.Report));
            return (Value: val,
                Report: withSubtotal ? $"{path}\t{val:R}{Environment.NewLine}{report}" : report);
        }

        protected override (double Value, string Report) LeafNoneAggr(string path, Balance cat, int depth,
            double val) =>
            (Value: val, Report: $"{path}\t{val:R}");

        protected override (double Value, string Report) LeafAggregated(string path, Balance cat, int depth,
            Balance bal) =>
            (Value: bal.Fund, Report: $"{Merge(path, bal.Date.AsDate())}\t{bal.Fund:R}");

        protected override string Map(string path, Balance cat, int depth, SubtotalLevel level)
        {
            switch (level)
            {
                case SubtotalLevel.Title:
                    return Merge(path, TitleManager.GetTitleName(cat.Title));
                case SubtotalLevel.SubTitle:
                    return Merge(path, TitleManager.GetTitleName(cat.Title, cat.SubTitle));
                case SubtotalLevel.Content:
                    return Merge(path, cat.Content);
                case SubtotalLevel.Remark:
                    return Merge(path, cat.Remark);
                case SubtotalLevel.Currency:
                    return Merge(path, $"@{cat.Currency}");
                default:
                    return Merge(path, cat.Date.AsDate(level));
            }
        }

        protected override string MapA(string path, Balance cat, int depth, AggregationType type) => path;

        protected override (double Value, string Report) MediumLevel(string path, string newPath, Balance cat,
            int depth,
            SubtotalLevel level, (double Value, string Report) r) => r;

        protected override (double Value, string Report) Reduce(string path, Balance cat, int depth,
            SubtotalLevel level,
            IEnumerable<(double Value, string Report)> results) => Gather(path, results, m_WithSubtotal);

        protected override (double Value, string Report) ReduceA(string path, string newPath, Balance cat, int depth,
            AggregationType type, IEnumerable<(double Value, string Report)> results)
            => Gather(path, results, m_WithSubtotal);
    }
}
