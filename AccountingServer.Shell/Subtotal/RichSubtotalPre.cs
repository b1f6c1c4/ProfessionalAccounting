/* Copyright (C) 2020-2024 b1f6c1c4
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

using System.Collections.Generic;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Subtotal;

/// <summary>
///     分类汇总结果处理器
/// </summary>
internal class RichSubtotalPre : StringSubtotalVisitor
{
    private const int Ident = 4;

    private string m_Currency;

    private int? m_Title;
    private string Idents => new(' ', (Depth > 0 ? Depth - 1 : 0) * Ident);

    private static string F(double? value, string curr)
    {
        if (curr == null)
            return $"{value.AsFund()} (!)";
        if (curr.EndsWith('#'))
            return $"{value.AsFund()} (#)";
        return value.AsFund(curr);
    }

    private string Ts(double f) => Ga is GatheringType.Count or GatheringType.VoucherCount
        ? f.ToString("N0")
        : F(f, Cu ?? m_Currency);

    private async IAsyncEnumerable<string> ShowSubtotal(ISubtotalResult sub, string str)
    {
        yield return $"{Idents}{str.CPadRight(38)}{Ts(sub.Fund).CPadLeft(12 + 2 * Depth)}\n";
        await foreach (var s in VisitChildren(sub))
            yield return s;
    }

    public override async IAsyncEnumerable<string> Visit(ISubtotalRoot sub)
    {
        yield return $"{Idents}{Ts(sub.Fund)}\n";
        await foreach (var s in VisitChildren(sub))
            yield return s;
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalDate sub)
        => ShowSubtotal(sub, sub.Date.AsDate(sub.Level));

    public override IAsyncEnumerable<string> Visit(ISubtotalVoucherRemark sub)
        => ShowSubtotal(sub, sub.VoucherRemark.Quotation('%'));

    public override IAsyncEnumerable<string> Visit(ISubtotalTitleKind sub)
        => ShowSubtotal(sub, $"{sub.Kind}");

    public override IAsyncEnumerable<string> Visit(ISubtotalUser sub)
        => ShowSubtotal(sub, sub.User.AsUser());

    public override async IAsyncEnumerable<string> Visit(ISubtotalCurrency sub)
    {
        m_Currency = sub.Currency;
        await foreach (var s in ShowSubtotal(sub, sub.Currency.AsCurrency()))
            yield return s;
        m_Currency = null;
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalTitle sub)
    {
        m_Title = sub.Title;
        return ShowSubtotal(sub, $"{sub.Title.AsTitle()} {TitleManager.GetTitleName(sub.Title)}");
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalSubTitle sub)
        => ShowSubtotal(sub, $"{sub.SubTitle.AsSubTitle()} {TitleManager.GetTitleName(m_Title, sub.SubTitle)}");

    public override IAsyncEnumerable<string> Visit(ISubtotalContent sub)
        => ShowSubtotal(sub, sub.Content.Quotation('\''));

    public override IAsyncEnumerable<string> Visit(ISubtotalRemark sub)
        => ShowSubtotal(sub, sub.Remark.Quotation('"'));

    public override IAsyncEnumerable<string> Visit(ISubtotalValue sub)
        => ShowSubtotal(sub, (m_Currency == null ? "(!)" : "")
                + F(sub.Value, m_Currency.EndsWith('#') ? null : m_Currency));
}
