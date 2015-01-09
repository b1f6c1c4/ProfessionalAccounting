using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    internal partial class AccountingConsole
    {
        /// <summary>
        ///     显示二层分类汇总的结果
        /// </summary>
        /// <param name="t">按一级科目的汇总</param>
        /// <param name="tsc">按内容的汇总</param>
        /// <returns></returns>
        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        private static string PresentSubtotal(List<Balance> t, List<Balance> tsc)
        {
            var sb = new StringBuilder();
            foreach (var balanceT in t)
            {
                var copiedT = balanceT;
                sb.AppendFormat(
                                "{0}-{1}:{2}",
                                copiedT.Title.AsTitle(),
                                TitleManager.GetTitleName(copiedT.Title).CPadRight(28),
                                copiedT.Fund.AsCurrency().CPadLeft(15));
                sb.AppendLine();
                foreach (var balanceC in tsc.Where(cx => cx.Title == copiedT.Title))
                {
                    var copiedC = balanceC;
                    sb.AppendFormat(
                                    "        {0}:{1}   ({2:00.0%})",
                                    copiedC.Content.CPadRight(25),
                                    copiedC.Fund.AsCurrency().CPadLeft(15),
                                    copiedC.Fund / copiedT.Fund);
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        /// <summary>
        ///     显示三层分类汇总的结果
        /// </summary>
        /// <param name="t">按一级科目的汇总</param>
        /// <param name="ts">按二级科目的汇总</param>
        /// <param name="tsc">按内容的汇总</param>
        /// <returns></returns>
        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        private static string PresentSubtotal(List<Balance> t, List<Balance> ts, List<Balance> tsc)
        {
            var sb = new StringBuilder();
            foreach (var balanceT in t)
            {
                var copiedT = balanceT;
                sb.AppendFormat(
                                "{0}-{1}:{2}",
                                copiedT.Title.AsTitle(),
                                TitleManager.GetTitleName(copiedT.Title).CPadRight(28),
                                copiedT.Fund.AsCurrency().CPadLeft(15));
                sb.AppendLine();
                foreach (var balanceS in ts.Where(sx => sx.Title == copiedT.Title))
                {
                    var copiedS = balanceS;
                    sb.AppendFormat(
                                    "  {0}-{1}:{2}   ({3:00.0%})",
                                    copiedS.SubTitle.HasValue ? copiedS.SubTitle.AsSubTitle() : "  ",
                                    TitleManager.GetTitleName(copiedS).CPadRight(28),
                                    copiedS.Fund.AsCurrency().CPadLeft(15),
                                    copiedS.Fund / copiedT.Fund);
                    sb.AppendLine();
                    foreach (var balanceC in tsc.Where(
                                                       cx => cx.Title == copiedS.Title
                                                             && cx.SubTitle == copiedS.SubTitle))
                    {
                        var copiedC = balanceC;
                        sb.AppendFormat(
                                        "        {0}:{1}   ({2:00.0%}, {3:00.0%})",
                                        copiedC.Content.CPadRight(25),
                                        copiedC.Fund.AsCurrency().CPadLeft(15),
                                        copiedC.Fund / copiedS.Fund,
                                        copiedC.Fund / copiedT.Fund);
                        sb.AppendLine();
                    }
                }
            }
            return sb.ToString();
        }


        /// <summary>
        ///     检索记账凭证并按一级科目、内容三层分类汇总，生成报表
        /// </summary>
        /// <param name="sx">检索表达式</param>
        /// <param name="withZero">是否包含汇总为零的部分</param>
        /// <returns>报表</returns>
        private string SubtotalWith2Levels(string sx, bool withZero)
        {
            var res = ExecuteDetailQuery(sx);
            if (res == null)
                throw new InvalidOperationException("检索表达式无效");

            var tscX = res.GroupBy(
                                   d =>
                                   new Balance { Title = d.Title, Content = d.Content },
                                   (b, bs) =>
                                   new Balance
                                       {
                                           Title = b.Title,
                                           Content = b.Content,
                                           Fund = bs.Sum(d => d.Fund.Value)
                                       },
                                   new BalanceEqualityComparer());
            var tsc = !withZero
                          ? tscX.Where(d => Math.Abs(d.Fund) > Accountant.Tolerance).ToList()
                          : tscX.ToList();
            tsc.Sort(new BalanceComparer());
            var tX = tsc.GroupBy(
                                 d => d.Title,
                                 (b, bs) =>
                                 new Balance
                                     {
                                         Title = b,
                                         Fund = bs.Sum(d => d.Fund)
                                     },
                                 EqualityComparer<int?>.Default);
            var t = !withZero
                        ? tX.Where(d => Math.Abs(d.Fund) > Accountant.Tolerance).ToList()
                        : tX.ToList();

            return PresentSubtotal(t, tsc);
        }

        /// <summary>
        ///     检索记账凭证并按一级科目、二级科目、内容三层分类汇总，生成报表
        /// </summary>
        /// <param name="sx">检索表达式</param>
        /// <param name="withZero">是否包含汇总为零的部分</param>
        /// <returns>报表</returns>
        private string SubtotalWith3Levels(string sx, bool withZero)
        {
            var res = ExecuteDetailQuery(sx);
            if (res == null)
                throw new InvalidOperationException("检索表达式无效");

            var tscX = res.GroupBy(
                                   d =>
                                   new Balance { Title = d.Title, SubTitle = d.SubTitle, Content = d.Content },
                                   (b, bs) =>
                                   new Balance
                                       {
                                           Title = b.Title,
                                           SubTitle = b.SubTitle,
                                           Content = b.Content,
                                           Fund = bs.Sum(d => d.Fund.Value)
                                       },
                                   new BalanceEqualityComparer());
            var tsc = !withZero
                          ? tscX.Where(d => Math.Abs(d.Fund) > Accountant.Tolerance).ToList()
                          : tscX.ToList();
            tsc.Sort(new BalanceComparer());
            var tsX = tsc.GroupBy(
                                  d =>
                                  new Balance { Title = d.Title, SubTitle = d.SubTitle },
                                  (b, bs) =>
                                  new Balance
                                      {
                                          Title = b.Title,
                                          SubTitle = b.SubTitle,
                                          Fund = bs.Sum(d => d.Fund)
                                      },
                                  new BalanceEqualityComparer());
            var ts = !withZero
                         ? tsX.Where(d => Math.Abs(d.Fund) > Accountant.Tolerance).ToList()
                         : tsX.ToList();
            var tX = ts.GroupBy(
                                d => d.Title,
                                (b, bs) =>
                                new Balance
                                    {
                                        Title = b,
                                        Fund = bs.Sum(d => d.Fund)
                                    },
                                EqualityComparer<int?>.Default);
            var t = !withZero
                        ? tX.Where(d => Math.Abs(d.Fund) > Accountant.Tolerance).ToList()
                        : tX.ToList();

            return PresentSubtotal(t, ts, tsc);
        }

        /// <summary>
        ///     检索记账凭证并按日期、一级科目、二级科目、内容四层分类汇总，生成报表
        /// </summary>
        /// <param name="sx">检索表达式</param>
        /// <param name="withZero">是否包含汇总为零的部分</param>
        /// <param name="reversed">是否将日期放在最外侧</param>
        /// <param name="aggr"></param>
        /// <returns>报表</returns>
        private string DailySubtotalWith4Levels(string sx, bool withZero, bool reversed, bool aggr)
        {
            string dateQ;
            var detail = ParseQuery(sx, out dateQ);

            var rng = ParseDateQuery(dateQ);

            AutoConnect();

            if (detail.Content == null)
            {
                if (detail.Title != null)
                    return reversed
                               ? SubtotalWith4Levels1X00Reversed(detail, rng, withZero)
                               : SubtotalWith4Levels1X00(detail, rng, withZero, aggr);

                throw new NotImplementedException();
            }
            if (detail.Title != null)
                return SubtotalWith4Levels1X10(withZero, detail, rng, aggr);

            throw new NotImplementedException();
        }

        /// <summary>
        ///     检索记账凭证并按日期、一级科目、内容三层分类汇总，生成报表
        /// </summary>
        /// <param name="sx">检索表达式</param>
        /// <param name="withZero">是否包含汇总为零的部分</param>
        /// <param name="reversed">是否将日期放在最外侧</param>
        /// <param name="aggr"></param>
        /// <returns>报表</returns>
        private string DailySubtotalWith3Levels(string sx, bool withZero, bool reversed, bool aggr)
        {
            string dateQ;
            var detail = ParseQuery(sx, out dateQ);

            var rng = ParseDateQuery(dateQ);

            AutoConnect();

            if (detail.Content == null)
            {
                if (detail.Title != null)
                    return reversed
                               ? SubtotalWith4Levels1X00Reversed(detail, rng, withZero)
                               : SubtotalWith4Levels1X00(detail, rng, withZero, aggr);

                throw new NotImplementedException();
            }
            if (detail.Title != null)
                return SubtotalWith4Levels1X10(withZero, detail, rng, aggr);

            throw new NotImplementedException();
        }

        /// <summary>
        ///     根据相关信息检索记账凭证并按内容、日期分类汇总，生成报表
        /// </summary>
        private string SubtotalWith4Levels1X00(VoucherDetail filter, DateFilter rng, bool withZero, bool aggr)
        {
            var vouchers = m_Accountant.FilteredSelect(filter, rng);

            var result = vouchers.SelectMany(
                                             v =>
                                             v.Details.Where(d => d.IsMatch(filter))
                                              .Select(d => new Tuple<DateTime?, VoucherDetail>(v.Date, d)))
                                 .GroupBy(
                                          t => t.Item2.Content,
                                          (c, ts) =>
                                          {
                                              var tmp = ts.GroupBy(
                                                                   t => t.Item1,
                                                                   (d, tss) =>
                                                                   new Balance
                                                                       {
                                                                           Date = d,
                                                                           //Content = c,
                                                                           Fund = tss.Sum(t => t.Item2.Fund.Value)
                                                                       }).ToList();
                                              tmp.Sort(new BalanceComparer());
                                              return new Tuple<string, IEnumerable<Balance>>(
                                                  c,
                                                  aggr
                                                      ? Accountant.ProcessAccumulatedBalance(tmp, rng)
                                                      : Accountant.ProcessDailyBalance(tmp, rng));
                                          });

            var sb = new StringBuilder();
            var flag = false;
            foreach (var balances in result)
            {
                sb.AppendFormat("    {0}:", balances.Item1);
                sb.AppendLine();

                foreach (var balance in balances.Item2)
                {
                    if (withZero)
                        if (Math.Abs(balance.Fund) < Accountant.Tolerance)
                        {
                            flag = true;
                            sb.Append("~");
                            continue;
                        }
                    if (flag)
                    {
                        sb.AppendLine();
                        flag = false;
                    }
                    sb.AppendFormat("{0:yyyyMMdd} {1}", balance.Date, balance.Fund.AsCurrency().CPadLeft(12));
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        /// <summary>
        ///     根据相关信息检索记账凭证并按日期、内容分类汇总，生成报表
        /// </summary>
        private string SubtotalWith4Levels1X00Reversed(VoucherDetail filter, DateFilter rng, bool withZero)
        {
            var vouchers = m_Accountant.FilteredSelect(filter, rng);

            var result = vouchers.GroupBy(
                                          v => v.Date,
                                          (dt, vs) => new Tuple<DateTime?, IEnumerable<Balance>>(
                                                          dt,
                                                          vs.SelectMany(v => v.Details)
                                                            .Where(d => d.IsMatch(filter))
                                                            .GroupBy(
                                                                     d => new Balance { Content = d.Content },
                                                                     (b, ds) =>
                                                                     new Balance
                                                                         {
                                                                             Date = dt,
                                                                             Title = filter.Title,
                                                                             SubTitle = filter.SubTitle,
                                                                             Content = b.Content,
                                                                             Fund = ds.Sum(d => d.Fund.Value)
                                                                         }))).ToList();

            result.Sort((t1, t2) => DateHelper.CompareDate(t1.Item1, t2.Item1));

            var cuml = Accountant.ProcessDailyBalance(result, rng);

            var sb = new StringBuilder();
            foreach (var balances in cuml)
            {
                sb.AppendFormat("{0:yyyyMMdd}:", balances.First().Date);
                sb.AppendLine();

                foreach (var balance in balances)
                {
                    if (withZero)
                        if (Math.Abs(balance.Fund) < Accountant.Tolerance)
                            continue;
                    sb.AppendFormat(
                                    "    {0}:{1}",
                                    balance.Content.CPadRight(25),
                                    balance.Fund.AsCurrency().CPadLeft(12));
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        /// <summary>
        ///     根据相关信息检索记账凭证并按日期分类汇总，生成报表
        /// </summary>
        private string SubtotalWith4Levels1X10(bool withZero, VoucherDetail filter, DateFilter rng, bool aggr)
        {
            var result =
                m_Accountant.GetDailyBalance(
                                             new Balance
                                                 {
                                                     Title = filter.Title,
                                                     SubTitle = filter.SubTitle,
                                                     Content = filter.Content
                                                 },
                                             rng,
                                             aggr: aggr);
            var sb = new StringBuilder();
            var flag = false;
            foreach (var balance in result)
            {
                if (withZero)
                    if (Math.Abs(balance.Fund) < Accountant.Tolerance)
                    {
                        flag = true;
                        sb.Append("~");
                        continue;
                    }
                if (flag)
                {
                    sb.AppendLine();
                    flag = false;
                }
                sb.AppendFormat("{0:yyyyMMdd} {1}", balance.Date, balance.Fund.AsCurrency().CPadLeft(12));
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
