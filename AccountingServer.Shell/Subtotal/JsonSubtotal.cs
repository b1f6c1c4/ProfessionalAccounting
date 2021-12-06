/* Copyright (C) 2020 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using Newtonsoft.Json.Linq;

namespace AccountingServer.Shell.Subtotal
{
    /// <summary>
    ///     分类汇总结果导出
    /// </summary>
    internal class JsonSubtotal : ISubtotalVisitor<JProperty>, ISubtotalStringify
    {
        private int m_Depth;

        private ISubtotal m_Par;

        /// <inheritdoc />
        public string PresentSubtotal(ISubtotalResult raw, ISubtotal par, IEntitiesSerializer serializer)
        {
            m_Par = par;
            m_Depth = 0;
            return (raw?.Accept(this)?.Value as JObject)?.ToString();
        }

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalRoot sub)
            => new JProperty("", VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalDate sub)
            => new JProperty(sub.Date.AsDate(sub.Level), VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalUser sub)
            => new JProperty(sub.User, VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalCurrency sub)
            => new JProperty(sub.Currency, VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalTitle sub)
            => new JProperty(sub.Title.AsTitle(), VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalSubTitle sub)
            => new JProperty(sub.SubTitle.AsSubTitle(), VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalContent sub)
            => new JProperty(sub.Content ?? "", VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalRemark sub)
            => new JProperty(sub.Remark ?? "", VisitChildren(sub));

        private JObject VisitChildren(ISubtotalResult sub)
        {
            var obj = new JObject(new JProperty("value", sub.Fund));
            if (sub.Items == null)
                return obj;

            string field;
            if (m_Depth < m_Par.Levels.Count)
                switch (m_Par.Levels[m_Depth] & SubtotalLevel.Subtotal)
                {
                    case SubtotalLevel.Title:
                        field = "title";
                        break;
                    case SubtotalLevel.SubTitle:
                        field = "subtitle";
                        break;
                    case SubtotalLevel.Content:
                        field = "content";
                        break;
                    case SubtotalLevel.Remark:
                        field = "remark";
                        break;
                    case SubtotalLevel.User:
                        field = "user";
                        break;
                    case SubtotalLevel.Currency:
                        field = "currency";
                        break;
                    case SubtotalLevel.Day:
                    case SubtotalLevel.Week:
                    case SubtotalLevel.Month:
                    case SubtotalLevel.Year:
                        field = "date";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            else
                field = "aggr";

            m_Depth++;
            obj[field] = new JObject(sub.Items.Select(it => it.Accept(this)));
            m_Depth--;

            return obj;
        }
    }
}
