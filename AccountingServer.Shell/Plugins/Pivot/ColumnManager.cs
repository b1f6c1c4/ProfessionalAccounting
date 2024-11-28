/* Copyright (C) 2024 b1f6c1c4
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
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.Shell.Plugins.Pivot;

internal struct Property
{
    public string Path;
    public string Currency;
    public ISubtotalValue Sub;

    public override string ToString() => Path;
}

internal class Stringifier : ISubtotalVisitor<IEnumerable<Property>>
{
    private ISubtotal m_Par;
    private int m_Depth;
    private string m_Path;
    private string m_Currency;

    public Stringifier(ISubtotal par)
    {
        m_Par = par;
        m_Depth = 0;
        m_Path = "";
        m_Currency = null;
    }

    private IEnumerable<Property> NextA(string token, ISubtotalResult sub)
    {
        var prev = m_Path;
        m_Path = m_Path.Length == 0 ? token : $"{m_Path}:{token}";
        var res = VisitChildrenA(sub);
        m_Path = prev;
        return res;
    }

    private IEnumerable<Property> VisitChildrenA(ISubtotalResult sub)
    {
        if (sub.Items == null)
            yield break;

        IEnumerable<ISubtotalResult> items;
        if (m_Depth < m_Par.Levels.Count)
        {
            var comparer = CultureInfo.GetCultureInfo("zh-CN").CompareInfo
                .GetStringComparer(CompareOptions.StringSort);
            items = (m_Par.Levels[m_Depth] & SubtotalLevel.Subtotal) switch
                {
                    SubtotalLevel.VoucherRemark => sub.Items.Cast<ISubtotalVoucherRemark>()
                        .OrderBy(static s => s.VoucherRemark),
                    SubtotalLevel.TitleKind => sub.Items.Cast<ISubtotalTitleKind>().OrderBy(static s => s.Kind),
                    SubtotalLevel.Title => sub.Items.Cast<ISubtotalTitle>().OrderBy(static s => s.Title),
                    SubtotalLevel.SubTitle => sub.Items.Cast<ISubtotalSubTitle>().OrderBy(static s => s.SubTitle),
                    SubtotalLevel.Content => sub.Items.Cast<ISubtotalContent>()
                        .OrderBy(static s => s.Content, comparer),
                    SubtotalLevel.Remark => sub.Items.Cast<ISubtotalRemark>().OrderBy(static s => s.Remark, comparer),
                    SubtotalLevel.User => sub.Items.Cast<ISubtotalUser>()
                        .OrderBy(static s => s.User),
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

        m_Depth++;
        foreach (var item in items)
            foreach (var x in item.Accept(this))
                yield return x;

        m_Depth--;
    }

    IEnumerable<Property> ISubtotalVisitor<IEnumerable<Property>>.Visit(ISubtotalRoot sub)
        => NextA("", sub);

    IEnumerable<Property> ISubtotalVisitor<IEnumerable<Property>>.Visit(ISubtotalDate sub)
        => NextA(sub.Date.AsDate(sub.Level), sub);

    IEnumerable<Property> ISubtotalVisitor<IEnumerable<Property>>.Visit(ISubtotalVoucherRemark sub)
        => NextA(sub.VoucherRemark.Quotation('%'), sub);

    IEnumerable<Property> ISubtotalVisitor<IEnumerable<Property>>.Visit(ISubtotalTitleKind sub)
        => NextA($"{sub.Kind}", sub);

    IEnumerable<Property> ISubtotalVisitor<IEnumerable<Property>>.Visit(ISubtotalUser sub)
        => NextA(sub.User.AsUser(), sub);

    IEnumerable<Property> ISubtotalVisitor<IEnumerable<Property>>.Visit(ISubtotalCurrency sub)
    {
        var prev = m_Currency;
        m_Currency = sub.Currency;
        var res = NextA(sub.Currency.AsUser(), sub);
        m_Currency = prev;
        return res;
    }

    IEnumerable<Property> ISubtotalVisitor<IEnumerable<Property>>.Visit(ISubtotalTitle sub)
        => NextA(sub.Title.AsTitle(), sub);

    IEnumerable<Property> ISubtotalVisitor<IEnumerable<Property>>.Visit(ISubtotalSubTitle sub)
        => NextA(sub.SubTitle.AsSubTitle(), sub);

    IEnumerable<Property> ISubtotalVisitor<IEnumerable<Property>>.Visit(ISubtotalContent sub)
        => NextA(sub.Content.Quotation('\''), sub);

    IEnumerable<Property> ISubtotalVisitor<IEnumerable<Property>>.Visit(ISubtotalRemark sub)
        => NextA(sub.Remark.Quotation('"'), sub);

    IEnumerable<Property> ISubtotalVisitor<IEnumerable<Property>>.Visit(ISubtotalValue sub)
    {
        yield return new() { Path = m_Path, Currency = m_Currency, Sub = sub };
    }
}

internal class ColumnManager
{
    private List<Property> m_Columns;

    private List<List<double?>> m_Array;

    public ColumnManager(ISubtotalResult isr, ISubtotal col)
    {
        m_Columns = new();
        foreach (var p in isr.Accept(new Stringifier(col)))
            m_Columns.Add(p);

        m_Array = new();
    }

    public void Add(ISubtotalResult isr, ISubtotal row)
    {
        var lst = new List<double?>(Width);
        foreach (var p in isr.Accept(new Stringifier(row)))
        {
            var id = m_Columns.FindIndex(0, (q) => q.Path == p.Path);
            if (id == -1)
                throw new ApplicationException($"Unknown path of {p.Path}");

            lst[id] = p.Sub.Value;
        }
        m_Array.Add(lst);
    }

    public int Width => m_Columns.Count;
    public int Height => m_Array.Count;
    public IReadOnlyList<Property> Header => m_Columns;
    public IReadOnlyList<double?> Row(int i) => m_Array[i];
    public IEnumerable<double?> Col(int i)
    {
        foreach (var l in m_Array)
            yield return l[i];
    }
}
