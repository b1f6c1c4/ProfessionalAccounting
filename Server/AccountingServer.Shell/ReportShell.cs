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
                        Pre =
                            (preVouchers, query) =>
                            preVouchers?.Where(v => v.IsMatch(query)).ToList() ??
                            m_Accountant.SelectVouchers(query).ToList(),
                        Leaf =
                            (path, query, coefficient, preVouchers) =>
                            {
                                var res = preVouchers == null
                                              ? m_Accountant.SelectVoucherDetailsGrouped(query.GroupingQuery)
                                              : preVouchers.SelectVoucherDetailsGrouped(query.GroupingQuery);
                                return PresentReport(
                                                     path.Length == 0 ? query.Name : path + "/" + query.Name,
                                                     coefficient * query.Coefficient,
                                                     query.GroupingQuery.Subtotal,
                                                     res,
                                                     withSubtotal);
                            },
                        Map = (path, query, coefficient) => path.Length == 0 ? query.Name : path + "/" + query.Name,
                        Reduce = (path, newPath, query, coefficient, results) => Gather(path, results, withSubtotal)
                    };

            Tuple<double, string> result;
            if (expr.namedQuery() != null)
                result = helper.Traversal(string.Empty, expr.namedQuery());
            else if (expr.groupedQuery() != null)
            {
                IGroupedQuery query = expr.groupedQuery();
                result = PresentReport(
                                       string.Empty,
                                       1,
                                       query.Subtotal,
                                       m_Accountant.SelectVoucherDetailsGrouped(query),
                                       withSubtotal);
            }
            else
                throw new ArgumentException("表达式类型未知", nameof(expr));
            return new UnEditableText(result.Item2);
        }

        /// <summary>
        ///     呈现报告条目
        /// </summary>
        /// <param name="path0">路径</param>
        /// <param name="coefficient">路径上累计的系数</param>
        /// <param name="args"></param>
        /// <param name="res"></param>
        /// <param name="withSubtotal">是否包含分类汇总</param>
        /// <returns>累计值和报告部分</returns>
        private Tuple<double, string> PresentReport(string path0, double coefficient, ISubtotal args,
                                                    IEnumerable<Balance> res,
                                                    bool withSubtotal = true)
        {
            var helper =
                new SubtotalTraver<string, Tuple<double, string>>(args)
                    {
                        LeafNoneAggr =
                            (path, cat, depth, val) =>
                            new Tuple<double, string>(
                                val * coefficient,
                                $"{path}\t{val:R}\t{coefficient:R}\t{val * coefficient:R}"),
                        LeafAggregated = (path, cat, depth, bal) =>
                                         new Tuple<double, string>(
                                             bal.Fund * coefficient,
                                             $"{path.Merge(bal.Date.AsDate())}\t{bal.Fund:R}\t{coefficient:R}\t{bal.Fund * coefficient:R}"),
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
                $"{path}\t\t\t{val:R}{Environment.NewLine}{report}");
        }
    }
}
