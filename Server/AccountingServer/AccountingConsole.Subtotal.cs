using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer
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
    }
}
