using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms.DataVisualization.Charting;
using AccountingServer.BLL;
using AccountingServer.Entities;
using Antlr4.Runtime;

namespace AccountingServer.Console
{
    public partial class AccountingConsole
    {
        /// <summary>
        ///     执行图表表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteChartQuery(ConsoleParser.ChartContext expr)
        {
            DateFilter rng;
            if (expr.range() != null)
                rng = expr.range().Range;
            else
            {
                var parser = new ConsoleParser(new CommonTokenStream(new ConsoleLexer(new AntlrInputStream("[0]"))));
                rng = parser.range().Range;
            }

            var helper = new NamedQueryTraver<Tuple<string, string>, ChartData>(m_Accountant, rng)
                             {
                                 Leaf = PresentChart,
                                 Map = (path, query, coefficient) =>
                                 {
                                     switch (query.Remark)
                                     {
                                         case "ignore":
                                             return path;
                                         case "chartArea":
                                             return
                                                 new Tuple<string, string>(
                                                     path.Item1 == null ? query.Name : path.Item1 + "-" + query.Name,
                                                     path.Item2);
                                         case "series":
                                             return
                                                 new Tuple<string, string>(
                                                     path.Item1,
                                                     path.Item2 == null ? query.Name : path.Item2 + "-" + query.Name);
                                         default:
                                             throw new InvalidOperationException();
                                     }
                                 },
                                 Reduce = (path, query, coefficient, results) =>
                                          {
                                              var datas = results as IList<ChartData> ?? results.ToList();
                                              return new ChartData
                                                         {
                                                             ChartAreas = datas.SelectMany(cd => cd.ChartAreas).ToList(),
                                                             Series = datas.SelectMany(cd => cd.Series).ToList()
                                                         };
                                          }
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

            return helper.Traversal(new Tuple<string, string>(null, null), q);
        }

        private ChartData PresentChart(Tuple<string, string> path, INamedQ query, double coefficient)
        {
            var lvs = query.GroupingQuery.Subtotal.Levels.Count;
            if (query.GroupingQuery.Subtotal.AggrType != AggregationType.None)
                lvs++;

            var sp = query.Remark.Split(';');
            
            throw new NotImplementedException();
        }
    }
}
