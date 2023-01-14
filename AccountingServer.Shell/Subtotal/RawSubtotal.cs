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

using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Subtotal;

/// <summary>
///     原始报告结果处理器
/// </summary>
internal class RawSubtotal : StringSubtotalVisitor
{
    private readonly VoucherDetailR m_Path = new(new(), new());
    private readonly bool m_Separate;
    private List<VoucherDetailR> m_History;

    public RawSubtotal(bool separate = false) => m_Separate = separate;

    private async IAsyncEnumerable<string> ShowSubtotal(ISubtotalResult sub)
    {
        if (sub.Items == null)
        {
            m_Path.Fund = sub.Fund;
            if (m_Separate)
                yield return Serializer.PresentVoucherDetail(m_Path);
            else
                m_History.Add(new(m_Path));
        }

        await foreach (var s in VisitChildren(sub))
            yield return s;
    }

    protected override IAsyncEnumerable<string> Pre()
    {
        m_History = new();
        return base.Pre();
    }

    protected override IAsyncEnumerable<string> Post()
        => Serializer.PresentVoucherDetails(m_History.ToAsyncEnumerable());

    public override IAsyncEnumerable<string> Visit(ISubtotalRoot sub)
        => ShowSubtotal(sub);

    public override IAsyncEnumerable<string> Visit(ISubtotalDate sub)
    {
        m_Path.Voucher.Date = sub.Date;
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
