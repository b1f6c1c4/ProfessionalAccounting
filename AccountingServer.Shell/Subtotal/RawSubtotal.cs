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
using System.Linq;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;

namespace AccountingServer.Shell.Subtotal;

/// <summary>
///     记账凭证式报告结果处理器
/// </summary>
internal class RawSubtotal : StringSubtotalVisitor
{
    private readonly VoucherDetail m_Path = new();
    private readonly int m_Level;
    private readonly Voucher m_Template;
    public double Ratio { private get; init; } = 1.0;
    public string Inject { private get; init; } = null;

    public RawSubtotal(int level)
    {
        m_Level = level;
        m_Template = new();
    }

    private async IAsyncEnumerable<string> ShowSubtotal(ISubtotalResult sub)
    {
        DateTime? dt = null;
        if (Depth == m_Level)
        {
            dt = m_Template.Date;
            m_Template.Details = new();
        }

        if (sub.Items == null)
        {
            m_Path.Fund = sub.Fund * Ratio;
            if (m_Template.Details != null)
                m_Template.Details.Add(new(m_Path));
        }
        else
        {
            await foreach (var s in VisitChildren(sub))
                yield return s;
        }

        if (Depth == m_Level)
        {
            yield return Serializer.PresentVoucher(m_Template, Inject).Wrap();

            m_Template.Date = dt;
            m_Template.Details = null;
        }
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalRoot sub)
        => ShowSubtotal(sub);

    public override IAsyncEnumerable<string> Visit(ISubtotalDate sub)
    {
        if (DateHelper.CompareDate(m_Template.Date, sub.Date) < 0)
            m_Template.Date = sub.Date;
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalUser sub)
    {
        m_Path.User = sub.User;
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalCurrency sub)
    {
        m_Path.Currency = sub.Currency;
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalTitle sub)
    {
        m_Path.Title = sub.Title;
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalSubTitle sub)
    {
        m_Path.SubTitle = sub.SubTitle;
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalContent sub)
    {
        m_Path.Content = sub.Content;
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalRemark sub)
    {
        m_Path.Remark = sub.Remark;
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalValue sub)
        => ShowSubtotal(sub);
}
