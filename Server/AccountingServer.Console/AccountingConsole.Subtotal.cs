using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class AccountingConsole
    {
        /// <summary>
        ///     执行分类汇总检索式并呈现结果
        /// </summary>
        /// <param name="query">分类汇总检索式</param>
        /// <returns>执行结果</returns>
        private IQueryResult PresentSubtotal(IGroupedQuery query)
        {
            AutoConnect();

            var result = m_Accountant.SelectVoucherDetailsGrouped(query);

            var sb = new StringBuilder();
            PresentSubtotal(result, 0, query.Subtotal, sb);

            return new UnEditableText(sb.ToString());
        }

        /// <summary>
        ///     呈现分类汇总
        /// </summary>
        /// <param name="res">分类汇总结果</param>
        /// <param name="depth">深度</param>
        /// <param name="args">分类汇总参数</param>
        /// <param name="sb">输出</param>
        private static void PresentSubtotal(IEnumerable<Balance> res, int depth, ISubtotal args, StringBuilder sb)
        {
            const int ident = 4;

            if (depth >= args.Levels.Count)
            {
                if (args.AggrType == AggregationType.None)
                {
                    var val = res.Sum(b => b.Fund);
                    sb.AppendLine(val.AsCurrency().CPadLeft(ident * 4));
                    return;
                }
                if (args.AggrType == AggregationType.ChangedDay)
                {
                    sb.AppendLine();
                    foreach (var b in Accountant.AggregateChangedDay(res))
                    {
                        if (args.NonZero &&
                            Math.Abs(b.Fund) < Accountant.Tolerance)
                            continue;

                        sb.Append(' ', depth * ident);
                        sb.AppendFormat("{0}:{1}", b.Date.AsDate(), b.Fund.AsCurrency().CPadLeft(ident * 4));
                        sb.AppendLine();
                    }
                    return;
                }
                if (args.AggrType == AggregationType.EveryDay)
                {
                    sb.AppendLine();
                    foreach (var b in Accountant.AggregateEveryDay(res, args.EveryDayRange.Range))
                    {
                        if (args.NonZero &&
                            Math.Abs(b.Fund) < Accountant.Tolerance)
                            continue;

                        sb.Append(' ', depth * ident);
                        sb.AppendFormat("{0}:{1}", b.Date.AsDate(), b.Fund.AsCurrency().CPadLeft(ident * 4));
                        sb.AppendLine();
                    }
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
                        sb.Append(' ', depth * ident);
                        sb.AppendFormat("{0}:", grp.Key.AsTitle());
                        PresentSubtotal(grp, depth + 1, args, sb);
                    }
                    break;
                case SubtotalLevel.SubTitle:
                    foreach (var grp in Accountant.GroupBySubTitle(res))
                    {
                        sb.Append(' ', depth * ident);
                        sb.AppendFormat("{0}:", grp.Key.AsSubTitle());
                        PresentSubtotal(grp, depth + 1, args, sb);
                    }
                    break;
                case SubtotalLevel.Content:
                    foreach (var grp in Accountant.GroupByContent(res))
                    {
                        sb.Append(' ', depth * ident);
                        sb.AppendFormat("{0}:", grp.Key.CPadRight(25));
                        PresentSubtotal(grp, depth + 1, args, sb);
                    }
                    break;
                case SubtotalLevel.Remark:
                    foreach (var grp in Accountant.GroupByRemark(res))
                    {
                        sb.Append(' ', depth * ident);
                        sb.AppendFormat("{0}:", grp.Key);
                        PresentSubtotal(grp, depth + 1, args, sb);
                    }
                    break;
                case SubtotalLevel.Day:
                case SubtotalLevel.Week:
                    foreach (var grp in Accountant.GroupByDate(res))
                    {
                        sb.Append(' ', depth * ident);
                        sb.AppendFormat("{0}:", grp.Key.AsDate());
                        PresentSubtotal(grp, depth + 1, args, sb);
                    }
                    break;
                case SubtotalLevel.FinancialMonth:
                    foreach (var grp in Accountant.GroupByDate(res))
                    {
                        sb.Append(' ', depth * ident);
                        if (grp.Key.HasValue)
                            sb.AppendFormat("{0:D4}{1:D2}:", grp.Key.Value.Year, grp.Key.Value.Month);
                        else
                            sb.Append("[null]:");
                        PresentSubtotal(grp, depth + 1, args, sb);
                    }
                    break;
                case SubtotalLevel.Month:
                    foreach (var grp in Accountant.GroupByDate(res))
                    {
                        sb.Append(' ', depth * ident);
                        if (grp.Key.HasValue)
                            sb.AppendFormat("@{0:D4}{1:D2}:", grp.Key.Value.Year, grp.Key.Value.Month);
                        else
                            sb.Append("[null]:");
                        PresentSubtotal(grp, depth + 1, args, sb);
                    }
                    break;
                case SubtotalLevel.BillingMonth:
                    foreach (var grp in Accountant.GroupByDate(res))
                    {
                        sb.Append(' ', depth * ident);
                        if (grp.Key.HasValue)
                            sb.AppendFormat("#{0:D4}{1:D2}:", grp.Key.Value.Year, grp.Key.Value.Month);
                        else
                            sb.Append("[null]:");
                        PresentSubtotal(grp, depth + 1, args, sb);
                    }
                    break;
                case SubtotalLevel.Year:
                    foreach (var grp in Accountant.GroupByDate(res))
                    {
                        sb.Append(' ', depth * ident);
                        if (grp.Key.HasValue)
                            sb.AppendFormat("{0:D4}:", grp.Key.Value.Year);
                        else
                            sb.Append("[null]:");
                        PresentSubtotal(grp, depth + 1, args, sb);
                    }
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
