using System;
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.Entities;
using Antlr4.Runtime;

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
            DateFilter rng;
            if (expr.range() != null)
                rng = expr.range().Range;
            else
            {
                var parser = new ConsoleParser(new CommonTokenStream(new ConsoleLexer(new AntlrInputStream("[0]"))));
                rng = parser.range().Range;
            }

            var helper = new NamedQueryTraver<string, string>(m_Accountant, rng)
                             {
                                 Leaf = PresentReport,
                                 Map = (path, query, coefficient) =>
                                       path.Length == 0 ? query.Name : path + "-" + query.Name,
                                 Reduce = (path, query, coefficient, results) =>
                                          String.Join(Environment.NewLine, results),
                             };

            INamedQuery q;

            if (expr.name() != null)
                q = helper.Dereference(expr.name().DollarQuotedString().Dequotation());
            else if (expr.namedQ() != null)
                q = expr.namedQ();
            else if (expr.namedQueries() != null)
                q = expr.namedQueries();
            else
                throw new InvalidOperationException();

            return new UnEditableText(helper.Traversal(String.Empty, q));
        }

        /// <summary>
        ///     呈现报告条目
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="query">命名查询</param>
        /// <param name="coefficient">路径上累计的系数</param>
        private string PresentReport(string path, INamedQ query, double coefficient)
        {
            var args = query.GroupingQuery.Subtotal;

            if (args.AggrType != AggregationType.None)
                throw new InvalidOperationException();

            var res = m_Accountant.SelectVoucherDetailsGrouped(query.GroupingQuery);

            var helper =
                new SubtotalTraver<Tuple<double, string>>(args)
                    {
                        LeafNoneAggr =
                            (cat, depth, val) =>
                            new Tuple<double, string>(
                                val * coefficient,
                                String.Format("{0} {1:R} {2:R} {3:R}", path, val, coefficient, val * coefficient)),
                        // TODO: 显示分类汇总时的路径
                        MediumLevel = (cat, depth, level, r) => r,
                        Reduce =
                            (cat, depth, level, results) =>
                            results.Aggregate(
                                              (r1, r2) =>
                                              new Tuple<double, string>(
                                                  r1.Item1 + r2.Item1,
                                                  r1.Item2 + Environment.NewLine + r2.Item2)),
                    };

            var traversal = helper.Traversal(res);

            return traversal.Item2;
        }
    }
}
