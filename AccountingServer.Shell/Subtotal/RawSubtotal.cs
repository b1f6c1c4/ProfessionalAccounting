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
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;

namespace AccountingServer.Shell.Subtotal;

/// <summary>
///     记账凭证式报告结果处理器
/// </summary>
internal class RawSubtotal : StringSubtotalVisitor
{
    private VoucherDetail m_Path;
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
            m_Path = new() { User = Client.User, Currency = BaseCurrency.Now };
            dt = m_Template.Date;
            m_Template.Details = new();
        }

        if (sub.Items == null)
        {
            if (m_Path != null)
                m_Path.Fund = sub.Fund * Ratio;
            m_Template.Details?.Add(new(m_Path));
        }
        else
        {
            await foreach (var s in VisitChildren(sub))
                yield return s;
        }

        if (Depth == m_Level)
        {
            yield return Serializer.PresentVoucher(m_Template, Inject).Wrap();

            m_Path = null;
            m_Template.Date = dt;
            m_Template.Details = null;
        }
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalRoot sub)
        => ShowSubtotal(sub);

    public override IAsyncEnumerable<string> Visit(ISubtotalDate sub)
    {
        if (m_Path == null)
            m_Template.Date = (sub.Level & SubtotalLevel.Subtotal) switch
                {
                    SubtotalLevel.Day => sub.Date,
                    SubtotalLevel.Week => sub.Date?.AddDays(6),
                    SubtotalLevel.Month => sub.Date?.AddMonths(1).AddDays(-1),
                    SubtotalLevel.Quarter => sub.Date?.AddMonths(3).AddDays(-1),
                    SubtotalLevel.Year => sub.Date?.AddYears(1).AddDays(-1),
                };
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalUser sub)
    {
        if (m_Path != null)
            m_Path.User = sub.User;
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalCurrency sub)
    {
        if (m_Path != null)
            m_Path.Currency = sub.Currency;
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalTitle sub)
    {
        if (m_Path != null)
            m_Path.Title = sub.Title;
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalSubTitle sub)
    {
        if (m_Path != null)
            m_Path.SubTitle = sub.SubTitle;
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalContent sub)
    {
        if (m_Path != null)
            m_Path.Content = sub.Content;
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalRemark sub)
    {
        if (m_Path != null)
            m_Path.Remark = sub.Remark;
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalValue sub)
        => ShowSubtotal(sub);
}
