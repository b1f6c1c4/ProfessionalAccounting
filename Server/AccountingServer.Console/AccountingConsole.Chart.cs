using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using AccountingServer.BLL;
using AccountingServer.Entities;

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
            var rng = expr.range() != null ? expr.range().Range : ConsoleParser.From("[0]").range().Range;

            var helper =
                new NamedQueryTraver<object, object>(m_Accountant, rng)
                    {
                        Leaf = PresentChart,
                        Map = (path, query, coefficient) =>
                              Fork(ConsoleParser.From(query.Remark).chartLevel(), path, query.Name),
                        Reduce = (path, newPath, query, coefficient, results) =>
                                 Reduce(path, results, newPath is Series)
                    };

            return (ChartData)helper.Traversal(null, expr.namedQuery());
        }

        private object PresentChart(object path0, INamedQ query, double coefficient)
        {
            var lvs = (String.IsNullOrWhiteSpace(query.Remark))
                          ? null
                          : ConsoleParser.From(query.Remark).chartLevels().chartLevel();

            var helper =
                new SubtotalTraver<object, object>(query.GroupingQuery.Subtotal)
                    {
                        LeafNoneAggr = (path, cat, depth, val) => val * coefficient,
                        LeafAggregated = (path, cat, depth, bal) => new Balance(bal) { Fund = bal.Fund * coefficient },
                        Map = (path, cat, depth, level) => lvs == null ||
                                                           depth >= lvs.Count
                                                               ? path
                                                               : Fork(lvs[depth], path, GetName(level, cat)),
                        MapA = (path, cat, depth, type) => null,
                        MediumLevel =
                            (path, newPath, cat, depth, level, r) =>
                            {
                                if (r is double)
                                {
                                    var series = (Series)newPath;
                                    var val = (double)r;
                                    series.Points.AddXY(GetXValue(level, cat), val);
                                    return String.Empty;
                                }
                                if (path is ChartArea &&
                                    newPath is Series)
                                {
                                    var lst = new List<Series>();
                                    if (!Reduce(r, lst))
                                        throw new InvalidOperationException();
                                    return new ChartData { ChartAreas = new List<ChartArea>(), Series = lst };
                                }
                                return r;
                            },
                        Reduce = (path, cat, depth, level, results) =>
                                 {
                                     var lst = results.ToList();

                                     if (lst.Count > 0 && lst[0] is string)
                                     {
                                         var series = path as Series;
                                         if (series != null)
                                         {
                                             series.ChartType = SeriesChartType.StackedColumn;
                                             return series;
                                         }
                                     }

                                     if (lvs == null)
                                         throw new InvalidOperationException();

                                     var lv = lvs[depth];
                                     return Reduce(path, lst, lv.Series() != null);
                                 },
                        ReduceA = (path, newPath, cat, depth, type, results) =>
                                  {
                                      var series = path as Series;
                                      if (series == null)
                                          throw new InvalidOperationException();

                                      series.ChartType = SeriesChartType.StackedArea;
                                      foreach (var o in results)
                                      {
                                          var bal = (Balance)o;
                                          if (bal.Date.HasValue)
                                              series.Points.AddXY(bal.Date.Value, bal.Fund);
                                      }
                                      return path;
                                  }
                    };

            return helper.Traversal(path0, m_Accountant.SelectVoucherDetailsGrouped(query.GroupingQuery));
        }

        private static object GetXValue(SubtotalLevel level, Balance cat)
        {
            switch (level)
            {
                case SubtotalLevel.Title:
                    return cat.Title.AsTitle();
                case SubtotalLevel.SubTitle:
                    return cat.SubTitle.AsSubTitle();
                case SubtotalLevel.Content:
                    return cat.Content ?? String.Empty;
                case SubtotalLevel.Remark:
                    return cat.Remark ?? String.Empty;
                case SubtotalLevel.Day:
                case SubtotalLevel.Week:
                case SubtotalLevel.Month:
                case SubtotalLevel.FinancialMonth:
                case SubtotalLevel.BillingMonth:
                case SubtotalLevel.Year:
                    return cat.Date;
                default:
                    throw new InvalidOperationException();
            }
        }

        private static string GetName(SubtotalLevel level, Balance cat)
        {
            switch (level)
            {
                case SubtotalLevel.Title:
                    return cat.Title.AsTitle() + TitleManager.GetTitleName(cat.Title);
                case SubtotalLevel.SubTitle:
                    return cat.SubTitle.AsSubTitle() + TitleManager.GetTitleName(cat.Title, cat.SubTitle);
                case SubtotalLevel.Content:
                    return cat.Content ?? String.Empty;
                case SubtotalLevel.Remark:
                    return cat.Remark ?? String.Empty;
                default:
                    return cat.Date.AsDate(level);
            }
        }

        private static object Fork(ConsoleParser.ChartLevelContext lv, object path, string name)
        {
            if (lv.Ignore() != null)
                return path;

            if (lv.ChartArea() != null)
            {
                if (path == null)
                    return new ChartArea(name);

                var area = path as ChartArea;
                if (area == null)
                    throw new InvalidOperationException();

                return new ChartArea(area.Name.Merge(name));
            }

            if (lv.Series() != null)
            {
                if (path == null)
                    throw new InvalidOperationException();

                var area = path as ChartArea;
                if (area != null)
                    return new Series(name) { ChartArea = area.Name };

                var series = path as Series;
                if (series != null)
                    return new Series(series.Name.Merge(name)) { ChartArea = series.ChartArea };

                throw new InvalidOperationException();
            }
            throw new InvalidOperationException();
        }

        private static bool Reduce(object o, ICollection<Series> data)
        {
            var series = o as Series;
            if (series != null)
            {
                data.Add(series);
                return true;
            }
            var seriess = o as IEnumerable<Series>;
            if (seriess != null)
            {
                foreach (var s in seriess)
                    data.Add(s);
                return true;
            }
            return false;
        }

        private static object Reduce(object path, IEnumerable<object> lst, bool edge)
        {
            var lout = new List<Series>();
            if (path is Series)
            {
                foreach (var o in lst)
                    if (!Reduce(o, lout))
                        throw new InvalidOperationException();
                return lout;
            }

            if (path != null &&
                !(path is ChartArea))
                throw new InvalidOperationException();

            var aout = new List<ChartArea>();
            foreach (var o in lst)
            {
                if (Reduce(o, lout))
                    continue;

                var d = o as ChartData;
                if (d == null)
                    throw new InvalidOperationException();

                aout.AddRange(d.ChartAreas);
                lout.AddRange(d.Series);
            }
            if (edge)
            {
                var area = path as ChartArea;
                if (area == null)
                    throw new InvalidOperationException();

                if (lout.Any(s => s.ChartType == SeriesChartType.StackedColumn))
                {
                    area.AxisX.LabelStyle.Interval = 1;
                    area.AxisX.IsLabelAutoFit = true;
                    area.AxisX.LabelAutoFitStyle = LabelAutoFitStyles.DecreaseFont |
                                                   LabelAutoFitStyles.IncreaseFont |
                                                   LabelAutoFitStyles.WordWrap;
                }
                aout.Add(area);
            }
            return new ChartData { ChartAreas = aout.Distinct().ToList(), Series = lout.Distinct().ToList() };
        }
    }
}
