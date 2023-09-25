/* Copyright (C) 2020-2023 b1f6c1c4
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
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;

namespace AccountingServer.Shell.Subtotal;

/// <summary>
///     分类汇总结果处理器
/// </summary>
internal abstract class StringSubtotalVisitor
    : IClientDependable, ISubtotalVisitor<IAsyncEnumerable<string>>, ISubtotalStringify
{
    protected string Cu;
    protected int Depth;

    protected GatheringType Ga;

    private ISubtotal m_Par;
    protected IEntitiesSerializer Serializer;

    public Client Client { private get; set; }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> PresentSubtotal(ISubtotalResult raw, ISubtotal par,
        IEntitiesSerializer serializer)
    {
        m_Par = par;
        Ga = par.GatherType;
        Cu = par.EquivalentCurrency;
        Serializer = serializer;
        Depth = 0;
        if (raw != null)
            await foreach (var s in raw.Accept(this))
                yield return s;
    }

    public abstract IAsyncEnumerable<string> Visit(ISubtotalRoot sub);
    public abstract IAsyncEnumerable<string> Visit(ISubtotalDate sub);
    public abstract IAsyncEnumerable<string> Visit(ISubtotalUser sub);
    public abstract IAsyncEnumerable<string> Visit(ISubtotalCurrency sub);
    public abstract IAsyncEnumerable<string> Visit(ISubtotalTitle sub);
    public abstract IAsyncEnumerable<string> Visit(ISubtotalSubTitle sub);
    public abstract IAsyncEnumerable<string> Visit(ISubtotalContent sub);
    public abstract IAsyncEnumerable<string> Visit(ISubtotalRemark sub);
    public abstract IAsyncEnumerable<string> Visit(ISubtotalValue sub);

    protected async IAsyncEnumerable<string> VisitChildren(ISubtotalResult sub)
    {
        if (sub.Items == null)
            yield break;

        IEnumerable<ISubtotalResult> items;
        if (Depth < m_Par.Levels.Count)
        {
            var comparer = CultureInfo.GetCultureInfo("zh-CN").CompareInfo
                .GetStringComparer(CompareOptions.StringSort);
            items = (m_Par.Levels[Depth] & SubtotalLevel.Subtotal) switch
                {
                    SubtotalLevel.Title => sub.Items.Cast<ISubtotalTitle>().OrderBy(static s => s.Title),
                    SubtotalLevel.SubTitle => sub.Items.Cast<ISubtotalSubTitle>().OrderBy(static s => s.SubTitle),
                    SubtotalLevel.Content => sub.Items.Cast<ISubtotalContent>()
                        .OrderBy(static s => s.Content, comparer),
                    SubtotalLevel.Remark => sub.Items.Cast<ISubtotalRemark>().OrderBy(static s => s.Remark, comparer),
                    SubtotalLevel.User => sub.Items.Cast<ISubtotalUser>()
                        .OrderBy(s => s.User == Client.User ? null : s.User),
                    SubtotalLevel.Currency => sub.Items.Cast<ISubtotalCurrency>()
                        .OrderBy(static s => s.Currency == BaseCurrency.Now ? null : s.Currency),
                    SubtotalLevel.Day => sub.Items.Cast<ISubtotalDate>().OrderBy(static s => s.Date),
                    SubtotalLevel.Week => sub.Items.Cast<ISubtotalDate>().OrderBy(static s => s.Date),
                    SubtotalLevel.Month => sub.Items.Cast<ISubtotalDate>().OrderBy(static s => s.Date),
                    SubtotalLevel.Quarter => sub.Items.Cast<ISubtotalDate>().OrderBy(static s => s.Date),
                    SubtotalLevel.Year => sub.Items.Cast<ISubtotalDate>().OrderBy(static s => s.Date),
                    SubtotalLevel.Value => sub.Items.Cast<ISubtotalValue>().OrderBy(static s => s.Value),
                    _ => throw new ArgumentOutOfRangeException(),
                };
        }
        else
            items = sub.Items;

        Depth++;
        foreach (var item in items)
        await foreach (var s in item.Accept(this))
            yield return s;

        Depth--;
    }
}
