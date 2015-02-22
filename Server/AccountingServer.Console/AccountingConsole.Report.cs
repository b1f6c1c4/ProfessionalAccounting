using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            AutoConnect();

            DateFilter rng;
            if (expr.range() != null)
                rng = expr.range().Range;
            else
            {
                var parser = new ConsoleParser(new CommonTokenStream(new ConsoleLexer(new AntlrInputStream("[0]"))));
                rng = parser.range().Range;
            }

            INamedQuery q;

            if (expr.name() != null)
            {
                var s = expr.name().DollarQuotedString().GetText();
                q = Dereference(s.Substring(1, s.Length - 2).Replace("$$", "$"), rng);
            }
            else if (expr.namedQ() != null)
                q = expr.namedQ();
            else if (expr.namedQueries() != null)
                q = expr.namedQueries();
            else
                throw new InvalidOperationException();

            var sb = new StringBuilder();
            PresentReport(String.Empty, q, rng, sb, 1);
            return new UnEditableText(sb.ToString());
        }

        /// <summary>
        ///     呈现报告条目
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="query">命名查询</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="sb">输出</param>
        /// <param name="coefficient">路径上累计的系数</param>
        private void PresentReport(string path, INamedQuery query, DateFilter rng, StringBuilder sb, double coefficient)
        {
            while (query is ConsoleParser.NamedQueryContext)
                query = (query as ConsoleParser.NamedQueryContext).InnerQuery;
            while (query is INamedQueryReference)
                query = Dereference(query as INamedQueryReference, rng);

            var q = query as INamedQueryConcrete;
            if (q == null)
                throw new InvalidOperationException();

            var newPath = (path.Length == 0 ? String.Empty : (path + "-")) + q.Name;
            var newCoefficient = coefficient * q.Coefficient;
            if (q is INamedQ)
            {
                var nq = q as INamedQ;
                var res = m_Accountant.SelectVoucherDetailsGrouped(nq.GroupingQuery);
                PresentReport(res, newPath, 0, nq.GroupingQuery.Subtotal, sb, newCoefficient);
                return;
            }
            if (q is INamedQueries)
            {
                foreach (var namedQuery in (q as INamedQueries).Items)
                    PresentReport(newPath, namedQuery, rng, sb, newCoefficient);
                return;
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        ///     在报告上呈现分类汇总条目
        /// </summary>
        /// <param name="res">分类汇总结果</param>
        /// <param name="path">路径</param>
        /// <param name="depth">深度</param>
        /// <param name="args">分类汇总参数</param>
        /// <param name="sb">输出</param>
        /// <param name="coefficient">路径上累计的系数</param>
        private static void PresentReport(IEnumerable<Balance> res, string path, int depth, ISubtotal args,
                                          StringBuilder sb, double coefficient)
        {
            if (depth >= args.Levels.Count)
            {
                if (args.AggrType == AggregationType.None)
                {
                    var val = res.Sum(b => b.Fund);
                    var fVal = val * coefficient;
                    sb.AppendFormat("{0:R} {1:R} {2:R}", val, coefficient, fVal);
                    return;
                }
                throw new InvalidOperationException();
            }

            if (depth > 0)
                sb.AppendLine();
            switch (args.Levels[depth])
            {
                case SubtotalLevel.Title:
                    foreach (var grp in Accountant.GroupByTitle(res))
                    {
                        var newPath = path + "-" + grp.Key.AsTitle();
                        sb.Append(newPath);
                        PresentReport(grp, newPath, depth + 1, args, sb, coefficient);
                    }
                    break;
                case SubtotalLevel.SubTitle:
                    foreach (var grp in Accountant.GroupBySubTitle(res))
                    {
                        var newPath = path + "-" + grp.Key.AsSubTitle();
                        sb.Append(newPath);
                        PresentReport(grp, newPath, depth + 1, args, sb, coefficient);
                    }
                    break;
                case SubtotalLevel.Content:
                    foreach (var grp in Accountant.GroupByContent(res))
                    {
                        var newPath = path + "-" + grp.Key;
                        sb.Append(newPath);
                        PresentReport(grp, newPath, depth + 1, args, sb, coefficient);
                    }
                    break;
                case SubtotalLevel.Remark:
                    foreach (var grp in Accountant.GroupByRemark(res))
                    {
                        var newPath = path + "-" + grp.Key;
                        sb.Append(newPath);
                        PresentReport(grp, newPath, depth + 1, args, sb, coefficient);
                    }
                    break;
                case SubtotalLevel.Day:
                case SubtotalLevel.Week:
                    foreach (var grp in Accountant.GroupByDate(res))
                    {
                        var newPath = path + "-" + grp.Key.AsDate();
                        sb.Append(newPath);
                        PresentReport(grp, newPath, depth + 1, args, sb, coefficient);
                    }
                    break;
                case SubtotalLevel.FinancialMonth:
                    foreach (var grp in Accountant.GroupByDate(res))
                    {
                        var newPath = path + "-" +
                                      (grp.Key.HasValue
                                           ? String.Format("{0:D4}{1:D2}", grp.Key.Value.Year, grp.Key.Value.Month)
                                           : "-[null]");
                        sb.Append(newPath);
                        PresentReport(grp, newPath, depth + 1, args, sb, coefficient);
                    }
                    break;
                case SubtotalLevel.Month:
                    foreach (var grp in Accountant.GroupByDate(res))
                    {
                        var newPath = path + "-" +
                                      (grp.Key.HasValue
                                           ? String.Format("@{0:D4}{1:D2}", grp.Key.Value.Year, grp.Key.Value.Month)
                                           : "-[null]");
                        sb.Append(newPath);
                        PresentReport(grp, newPath, depth + 1, args, sb, coefficient);
                    }
                    break;
                case SubtotalLevel.BillingMonth:
                    foreach (var grp in Accountant.GroupByDate(res))
                    {
                        var newPath = path + "-" +
                                      (grp.Key.HasValue
                                           ? String.Format("#{0:D4}{1:D2}", grp.Key.Value.Year, grp.Key.Value.Month)
                                           : "-[null]");
                        sb.Append(newPath);
                        PresentReport(grp, newPath, depth + 1, args, sb, coefficient);
                    }
                    break;
                case SubtotalLevel.Year:
                    foreach (var grp in Accountant.GroupByDate(res))
                    {
                        var newPath = path + "-" +
                                      (grp.Key.HasValue
                                           ? String.Format("{0:D4}", grp.Key.Value.Year)
                                           : "-[null]");
                        sb.Append(newPath);
                        PresentReport(grp, newPath, depth + 1, args, sb, coefficient);
                    }
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
