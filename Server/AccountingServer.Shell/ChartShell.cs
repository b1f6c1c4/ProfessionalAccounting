using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell.Parsing;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     图表表达式解释器
    /// </summary>
    internal class ChartShell
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public ChartShell(Accountant helper) { m_Accountant = helper; }

        /// <summary>
        ///     执行图表表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        public IQueryResult ExecuteChartQuery(ShellParser.ChartContext expr)
        {
            var rng = expr.range() != null ? expr.range().Range : ShellParser.From("[0]").range().Range;

            var helper =
                new NamedQueryTraver<object, object>(m_Accountant, rng)
                    {
                        Pre =
                            (preVouchers, query) =>
                            preVouchers?.Where(v => v.IsMatch(query)).ToList() ??
                            m_Accountant.SelectVouchers(query).ToList(),
                        Leaf = PresentChart,
                        Map = (path, query, coefficient) =>
                              Fork(ShellParser.From(query.Remark).chartLevel(), path, query.Name),
                        Reduce = (path, newPath, query, coefficient, results) =>
                                 Reduce(path, results, newPath is Series)
                    };

            return (ChartData)helper.Traversal(null, expr.namedQuery());
        }

        /// <summary>
        ///     呈现图表
        /// </summary>
        /// <param name="path0">路径</param>
        /// <param name="query">分类汇总检索式</param>
        /// <param name="coefficient">路径上累计的系数</param>
        /// <param name="preVouchers">公共记账凭证</param>
        /// <returns></returns>
        private object PresentChart(object path0, INamedQ query, double coefficient, IReadOnlyList<Voucher> preVouchers)
        {
            var lvs = (string.IsNullOrWhiteSpace(query.Remark))
                          ? null
                          : ShellParser.From(query.Remark).chartLevels().chartLevel();

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
                                    return string.Empty;
                                }
                                if (path is ChartArea &&
                                    newPath is Series)
                                {
                                    var lst = new List<Series>();
                                    if (!Reduce(r, lst))
                                        throw new ArgumentException("同一层级的路径之间不符", nameof(query));
                                    return new ChartData { ChartAreas = new List<ChartArea>(), Series = lst };
                                }
                                return r;
                            },
                        Reduce = (path, cat, depth, level, results) =>
                                 {
                                     var lst = results.ToList();

                                     if (lst.Count > 0 && lst[0] is string ||
                                         lvs == null)
                                     {
                                         var series = path as Series;
                                         if (series != null)
                                         {
                                             series.ChartType = SeriesChartType.StackedColumn;
                                             return series;
                                         }
                                     }

                                     if (lvs == null)
                                         throw new ArgumentException("汇总错误");
                                     var lv = lvs[depth];
                                     return Reduce(path, lst, lv.Series() != null);
                                 },
                        ReduceA = (path, newPath, cat, depth, type, results) =>
                                  {
                                      var series = path as Series;
                                      if (series == null)
                                          throw new ArgumentException("底层路径不正确", nameof(query));

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

            return helper.Traversal(
                                    path0,
                                    preVouchers == null
                                        ? m_Accountant.SelectVoucherDetailsGrouped(query.GroupingQuery)
                                        : preVouchers.SelectVoucherDetailsGrouped(query.GroupingQuery));
        }

        /// <summary>
        ///     获取横轴标签
        /// </summary>
        /// <param name="level">分类汇总层次</param>
        /// <param name="cat">累计类别</param>
        /// <returns>标签</returns>
        private static object GetXValue(SubtotalLevel level, Balance cat)
        {
            switch (level)
            {
                case SubtotalLevel.Title:
                    return cat.Title.AsTitle();
                case SubtotalLevel.SubTitle:
                    return cat.SubTitle.AsSubTitle();
                case SubtotalLevel.Content:
                    return cat.Content ?? string.Empty;
                case SubtotalLevel.Remark:
                    return cat.Remark ?? string.Empty;
                case SubtotalLevel.Day:
                case SubtotalLevel.Week:
                case SubtotalLevel.Month:
                case SubtotalLevel.FinancialMonth:
                case SubtotalLevel.BillingMonth:
                case SubtotalLevel.Year:
                    return cat.Date;
                default:
                    throw new ArgumentException("分类汇总层次未知", nameof(cat));
            }
        }

        /// <summary>
        ///     获取系列名称
        /// </summary>
        /// <param name="level">分类汇总层次</param>
        /// <param name="cat">类别</param>
        /// <returns>系列名称</returns>
        private static string GetName(SubtotalLevel level, Balance cat)
        {
            switch (level)
            {
                case SubtotalLevel.Title:
                    return cat.Title.AsTitle() + TitleManager.GetTitleName(cat.Title);
                case SubtotalLevel.SubTitle:
                    return cat.SubTitle.AsSubTitle() + TitleManager.GetTitleName(cat.Title, cat.SubTitle);
                case SubtotalLevel.Content:
                    return cat.Content ?? string.Empty;
                case SubtotalLevel.Remark:
                    return cat.Remark ?? string.Empty;
                default:
                    return cat.Date.AsDate(level);
            }
        }

        /// <summary>
        ///     系列分裂
        /// </summary>
        /// <param name="lv">制图层次解析结果</param>
        /// <param name="path">路径</param>
        /// <param name="name">系列名称</param>
        /// <returns>新路径</returns>
        private static object Fork(ShellParser.ChartLevelContext lv, object path, string name)
        {
            if (lv.Ignore() != null)
                return path;

            if (lv.ChartArea() != null)
            {
                if (path == null)
                    return new ChartArea(name);

                var area = path as ChartArea;
                if (area == null)
                    throw new ArgumentException("制图层次与路径不符", nameof(path));

                return new ChartArea(area.Name.Merge(name));
            }

            if (lv.Series() != null)
            {
                if (path == null)
                    throw new ArgumentException("制图层次与路径不符", nameof(path));

                var area = path as ChartArea;
                if (area != null)
                    return new Series(name) { ChartArea = area.Name };

                var series = path as Series;
                if (series != null)
                    return new Series(series.Name.Merge(name)) { ChartArea = series.ChartArea };

                throw new ArgumentException("制图层次与路径不符", nameof(path));
            }
            throw new ArgumentException("制图层次未知", nameof(lv));
        }

        /// <summary>
        ///     汇总路径
        /// </summary>
        /// <param name="o">路径</param>
        /// <param name="data">路径集</param>
        /// <returns>是否并入路径集</returns>
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

        /// <summary>
        ///     汇总路径
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="lst">路径集</param>
        /// <param name="edge">是否末级汇总</param>
        /// <returns>路径</returns>
        private static object Reduce(object path, IEnumerable<object> lst, bool edge)
        {
            var lout = new List<Series>();
            if (path is Series)
            {
                foreach (var o in lst)
                    if (!Reduce(o, lout))
                        throw new ArgumentException("同一层级的路径之间不符", nameof(lst));
                return lout;
            }

            if (path != null &&
                !(path is ChartArea))
                throw new ArgumentException("路径与上一层级的路径不符", nameof(path));

            var aout = new List<ChartArea>();
            foreach (var o in lst)
            {
                if (Reduce(o, lout))
                    continue;

                var d = o as ChartData;
                if (d == null)
                    throw new ArgumentException("同一层级的路径之间不符", nameof(lst));

                aout.AddRange(d.ChartAreas);
                lout.AddRange(d.Series);
            }
            if (edge)
            {
                var area = path as ChartArea;
                if (area == null)
                    throw new ArgumentException("制图层次与路径不符", nameof(path));

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
