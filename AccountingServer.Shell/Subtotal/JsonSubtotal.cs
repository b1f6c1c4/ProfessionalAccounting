/* Copyright (C) 2020-2025 b1f6c1c4
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AccountingServer.Shell.Subtotal;

/// <summary>
///     分类汇总结果导出
/// </summary>
internal class JsonSubtotal : ISubtotalVisitor<JProperty>, ISubtotalStringify
{
    private int m_Depth;

    private ISubtotal m_Par;

    /// <inheritdoc />
    public IAsyncEnumerable<string> PresentSubtotal(ISubtotalResult raw, ISubtotal par, IEntitiesSerializer serializer)
    {
        m_Par = par;
        m_Depth = 0;
        var obj = raw?.Accept(this)?.Value as JObject;

        using var sw = new StringWriter(CultureInfo.InvariantCulture);
        sw.NewLine = "\n";
        var jw = new JsonTextWriter(sw);
        jw.Formatting = Formatting.Indented;
        obj?.WriteTo(jw);
        return AsyncEnumerable.Repeat(sw.ToString(), 1);
    }

    JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalRoot sub)
        => new("", VisitChildren(sub));

    JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalDate sub)
        => new(sub.Date.AsDate(sub.Level), VisitChildren(sub));

    JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalVoucherRemark sub)
        => new(sub.VoucherRemark, VisitChildren(sub));

    JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalVoucherType sub)
        => new($"{sub.Type}", VisitChildren(sub));

    JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalTitleKind sub)
        => new($"{sub.Kind}", VisitChildren(sub));

    JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalUser sub)
        => new(sub.User, VisitChildren(sub));

    JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalCurrency sub)
        => new(sub.Currency, VisitChildren(sub));

    JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalTitle sub)
        => new(sub.Title.AsTitle(), VisitChildren(sub));

    JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalSubTitle sub)
        => new(sub.SubTitle.AsSubTitle(), VisitChildren(sub));

    JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalContent sub)
        => new(sub.Content ?? "", VisitChildren(sub));

    JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalRemark sub)
        => new(sub.Remark ?? "", VisitChildren(sub));

    JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalValue sub)
        => new($"{sub.Value:R}", VisitChildren(sub));

    private JObject VisitChildren(ISubtotalResult sub)
    {
        var obj = new JObject(new JProperty("value", sub.Fund));
        if (sub.Items == null)
            return obj;

        var field = m_Depth < m_Par.Levels.Count
            ? (m_Par.Levels[m_Depth] & SubtotalLevel.Subtotal) switch
                {
                    SubtotalLevel.VoucherRemark => "vremark",
                    SubtotalLevel.Title => "title",
                    SubtotalLevel.SubTitle => "subtitle",
                    SubtotalLevel.Content => "content",
                    SubtotalLevel.Remark => "remark",
                    SubtotalLevel.User => "user",
                    SubtotalLevel.Currency => "currency",
                    SubtotalLevel.Day => "date",
                    SubtotalLevel.Week => "date",
                    SubtotalLevel.Month => "date",
                    SubtotalLevel.Quarter => "date",
                    SubtotalLevel.Year => "date",
                    _ => throw new ArgumentOutOfRangeException(),
                }
            : "aggr";

        m_Depth++;
        obj[field] = new JObject(sub.Items.Select(it => it.Accept(this)));
        m_Depth--;

        return obj;
    }
}
