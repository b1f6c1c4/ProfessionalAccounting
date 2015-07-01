using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell.Parsing;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     报告表达式解释器
    /// </summary>
    internal class ReportShell
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public ReportShell(Accountant helper) { m_Accountant = helper; }

        /// <summary>
        ///     执行报告表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <param name="withSubtotal">是否包含分类汇总</param>
        /// <returns>执行结果</returns>
        public IQueryResult ExecuteReportQuery(ShellParser.ReportContext expr, bool withSubtotal = true)
        {
            var rng = expr.range() != null ? expr.range().Range : ShellParser.From("[0]").range().Range;

            var helper =
                new NamedQueryTraver<string, Tuple<double, string>>(m_Accountant, rng)
                    {
                        Leaf =
                            (path, query, coefficient) =>
                            PresentReport(
                                          path.Length == 0 ? query.Name : path + "/" + query.Name,
                                          query.GroupingQuery,
                                          coefficient * query.Coefficient,
                                          withSubtotal),
                        Map = (path, query, coefficient) => path.Length == 0 ? query.Name : path + "/" + query.Name,
                        Reduce = (path, newPath, query, coefficient, results) => Gather(path, results, withSubtotal),
                    };

            Tuple<double, string> result;
            if (expr.namedQuery() != null)
                result = helper.Traversal(String.Empty, expr.namedQuery());
            else if (expr.groupedQuery() != null)
                result = PresentReport(String.Empty, expr.groupedQuery(), 1, withSubtotal);
            else
                throw new ArgumentException("表达式类型未知", "expr");
            return new UnEditableText(result.Item2);
        }

        /// <summary>
        ///     呈现报告条目
        /// </summary>
        /// <param name="path0">路径</param>
        /// <param name="query">分类汇总检索式</param>
        /// <param name="coefficient">路径上累计的系数</param>
        /// <param name="withSubtotal">是否包含分类汇总</param>
        /// <returns>累计值和报告部分</returns>
        private Tuple<double, string> PresentReport(string path0, IGroupedQuery query, double coefficient,
                                                    bool withSubtotal = true)
        {
            var args = query.Subtotal;
            var res = m_Accountant.SelectVoucherDetailsGrouped(query);

            var helper =
                new SubtotalTraver<string, Tuple<double, string>>(args)
                    {
                        LeafNoneAggr =
                            (path, cat, depth, val) =>
                            new Tuple<double, string>(
                                val * coefficient,
                                String.Format("{0}\t{1:R}\t{2:R}\t{3:R}", path, val, coefficient, val * coefficient)),
                        LeafAggregated = (path, cat, depth, bal) =>
                                         new Tuple<double, string>(
                                             bal.Fund * coefficient,
                                             String.Format(
                                                           "{0}\t{1:R}\t{2:R}\t{3:R}",
                                                           path.Merge(bal.Date.AsDate()),
                                                           bal.Fund,
                                                           coefficient,
                                                           bal.Fund * coefficient)),
                        Map = (path, cat, depth, level) =>
                              {
                                  switch (level)
                                  {
                                      case SubtotalLevel.Title:
                                          return path.Merge(TitleManager.GetTitleName(cat.Title));
                                      case SubtotalLevel.SubTitle:
                                          return path.Merge(TitleManager.GetTitleName(cat.Title, cat.SubTitle));
                                      case SubtotalLevel.Content:
                                          return path.Merge(cat.Content);
                                      case SubtotalLevel.Remark:
                                          return path.Merge(cat.Remark);
                                      default:
                                          return path.Merge(cat.Date.AsDate(level));
                                  }
                              },
                        MapA = (path, cat, depth, type) => path,
                        MediumLevel = (path, newPath, cat, depth, level, r) => r,
                        Reduce = (path, cat, depth, level, results) => Gather(path, results, withSubtotal),
                        ReduceA = (path, newPath, cat, depth, type, results) => Gather(path, results, withSubtotal)
                    };

            var traversal = helper.Traversal(path0, res);

            return traversal;
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
            var report = SubtotalHelper.NotNullJoin(r.Select(t => t.Item2));
            if (!withSubtotal)
                return new Tuple<double, string>(val, report);
            return new Tuple<double, string>(
                val,
                String.Format("{0}\t\t\t{1:R}{2}{3}", path, val, Environment.NewLine, report));
        }
    }
}
