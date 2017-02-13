using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using static AccountingServer.Shell.Util.SubtotalPreHelper;

namespace AccountingServer.Shell.Util
{
    /// <summary>
    ///     报告结果处理器
    /// </summary>
    internal class PreciseSubtotalPre : SubtotalTraver<string, Tuple<double, string>>, ISubtotalPre
    {
        /// <summary>
        ///     是否包含分类汇总
        /// </summary>
        private readonly bool m_WithSubtotal;

        /// <inheritdoc />
        public string PresentSubtotal(IEnumerable<Balance> res)
        {
            var traversal = Traversal("", res);
            return traversal.Item2;
        }

        public PreciseSubtotalPre(bool withSubtotal = true)
        {
            m_WithSubtotal = withSubtotal;
        }

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
        private static Tuple<double, string> Gather(string path, IEnumerable<Tuple<double, string>> results,
            bool withSubtotal = true)
        {
            var r = results.ToList();
            var val = r.Sum(t => t.Item1);
            var report = NotNullJoin(r.Select(t => t.Item2));
            return new Tuple<double, string>(
                val,
                withSubtotal ? $"{path}\t{val:R}{Environment.NewLine}{report}" : report);
        }

        protected override Tuple<double, string> LeafNoneAggr(string path, Balance cat, int depth, double val) =>
            new Tuple<double, string>(val, $"{path}\t{val:R}");

        protected override Tuple<double, string> LeafAggregated(string path, Balance cat, int depth, Balance bal) =>
            new Tuple<double, string>(bal.Fund, $"{Merge(path, bal.Date.AsDate())}\t{bal.Fund:R}");

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

        protected override Tuple<double, string> MediumLevel(string path, string newPath, Balance cat, int depth,
            SubtotalLevel level, Tuple<double, string> r) => r;

        protected override Tuple<double, string> Reduce(string path, Balance cat, int depth, SubtotalLevel level,
            IEnumerable<Tuple<double, string>> results) => Gather(path, results, m_WithSubtotal);

        protected override Tuple<double, string> ReduceA(string path, string newPath, Balance cat, int depth,
            AggregationType type, IEnumerable<Tuple<double, string>> results)
            => Gather(path, results, m_WithSubtotal);
    }
}
