using System;
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class AccountingConsole
    {
        /// <summary>
        ///     执行报告表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteReportQuery(ConsoleParser.ReportContext expr)
        {
            var rng = expr.range() != null ? expr.range().Range : ConsoleParser.From("[0]").range().Range;

            var helper = new NamedQueryTraver<string, string>(m_Accountant, rng)
                             {
                                 Leaf =
                                     (path, query, coefficient) => PresentReport(path, query.GroupingQuery, coefficient),
                                 Map = (path, query, coefficient) =>
                                       path.Length == 0 ? query.Name : path + "-" + query.Name,
                                 Reduce = (path, newPath, query, coefficient, results) =>
                                          String.Join(Environment.NewLine, results),
                             };

            if (expr.namedQuery() != null)
                return new UnEditableText(helper.Traversal(String.Empty, expr.namedQuery()));
            if (expr.groupedQuery() != null)
                return new UnEditableText(PresentReport(String.Empty, expr.groupedQuery(), 1));

            throw new InvalidOperationException();
        }

        /// <summary>
        ///     呈现报告条目
        /// </summary>
        /// <param name="path0">路径</param>
        /// <param name="query">分类汇总检索式</param>
        /// <param name="coefficient">路径上累计的系数</param>
        private string PresentReport(string path0, IGroupedQuery query, double coefficient)
        {
            var args = query.Subtotal;

            if (args.AggrType != AggregationType.None)
                throw new InvalidOperationException();

            var res = m_Accountant.SelectVoucherDetailsGrouped(query);

            var helper =
                new SubtotalTraver<string, Tuple<double, string>>(args)
                    {
                        LeafNoneAggr =
                            (path, cat, depth, val) =>
                            new Tuple<double, string>(
                                val * coefficient,
                                String.Format("{0} {1:R} {2:R} {3:R}", path, val, coefficient, val * coefficient)),
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
                        MediumLevel = (path, newPath, cat, depth, level, r) => r,
                        Reduce =
                            (path, cat, depth, level, results) =>
                            results.Aggregate(
                                              (r1, r2) =>
                                              new Tuple<double, string>(
                                                  r1.Item1 + r2.Item1,
                                                  r1.Item2 + Environment.NewLine + r2.Item2)),
                    };

            var traversal = helper.Traversal(path0, res);

            return traversal.Item2;
        }
    }
}
