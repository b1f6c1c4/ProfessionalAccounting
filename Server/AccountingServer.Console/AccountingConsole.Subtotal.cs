﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class AccountingConsole
    {
        private IQueryResult PresentSubtotal(IGroupedQuery query)
        {
            AutoConnect();
            var result = m_Accountant.FilteredSelect(query);
            var sb = new StringBuilder();
            PresentSubtotal(result, 0, query.Subtotal, sb);
            return new UnEditableText(sb.ToString());
        }

        private void PresentSubtotal(IEnumerable<Balance> res, int depth, ISubtotal args, StringBuilder sb)
        {
            const int ident = 4;

            if (depth >= args.Levels.Count)
            {
                if (!args.AggrEnabled)
                {
                    sb.AppendLine(res.Sum(b => b.Fund).AsCurrency());
                    return;
                }
                if (args.AggrRage == null)
                {
                    sb.AppendLine();
                    foreach (var b in Accountant.GroupByDateAggr(res))
                    {
                        sb.Append(depth * ident);
                        sb.AppendFormat("{0}:{1}", b.Date.AsDate(), b.Fund.AsCurrency().PadLeft(ident * 4));
                        sb.AppendLine();
                    }
                    return;
                }
                //else
                {
                    sb.AppendLine();
                    foreach (var b in Accountant.GroupByDateBal(res, args.AggrRage.Range))
                    {
                        sb.Append(depth * ident);
                        sb.AppendFormat("{0}:{1}", b.Date.AsDate(), b.Fund.AsCurrency().PadLeft(ident * 4));
                        sb.AppendLine();
                    }
                    return;
                }
            }

            sb.AppendLine();
            switch (args.Levels[depth])
            {
                case SubtotalLevel.Title:
                    foreach (var grp in Accountant.GroupByTitle(res))
                    {
                        sb.Append(depth * ident);
                        sb.AppendFormat("{0}:", grp.Key.AsTitle());
                        PresentSubtotal(grp, depth + 1, args, sb);
                    }
                    break;
                case SubtotalLevel.SubTitle:
                    foreach (var grp in Accountant.GroupBySubTitle(res))
                    {
                        sb.Append(depth * ident);
                        sb.AppendFormat("{0}:", grp.Key.AsSubTitle());
                        PresentSubtotal(grp, depth + 1, args, sb);
                    }
                    break;
                case SubtotalLevel.Content:
                    foreach (var grp in Accountant.GroupByContent(res))
                    {
                        sb.Append(depth * ident);
                        sb.AppendFormat("{0}:", grp.Key);
                        PresentSubtotal(grp, depth + 1, args, sb);
                    }
                    break;
                case SubtotalLevel.Remark:
                    foreach (var grp in Accountant.GroupByRemark(res))
                    {
                        sb.Append(depth * ident);
                        sb.AppendFormat("{0}:", grp.Key);
                        PresentSubtotal(grp, depth + 1, args, sb);
                    }
                    break;
                case SubtotalLevel.Day:
                case SubtotalLevel.Week:
                    foreach (var grp in Accountant.GroupByDate(res))
                    {
                        sb.Append(depth * ident);
                        sb.AppendFormat("{0}:", grp.Key);
                        PresentSubtotal(grp, depth + 1, args, sb);
                    }
                    break;
                case SubtotalLevel.FinancialMonth:
                    foreach (var grp in Accountant.GroupByDate(res))
                    {
                        sb.Append(depth * ident);
                        sb.AppendFormat("{0}:", grp.Key);
                        PresentSubtotal(grp, depth + 1, args, sb);
                    }
                    break;
            }
        }
    }
}
