/* Copyright (C) 2020-2021 b1f6c1c4
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
using AccountingServer.Entities.Util;

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

    private void ShowSubtotal(ISubtotalResult sub)
    {
        if (sub.Items == null)
        {
            m_Path.Fund = sub.Fund;
            if (m_Separate)
                Sb.Append(Serializer.PresentVoucherDetail(m_Path));
            else
                m_History.Add(new(m_Path));
        }

        VisitChildren(sub);
    }

    protected override void Pre() => m_History = new();

    protected override void Post() => Sb.Append(Serializer.PresentVoucherDetails(m_History.ToAsyncEnumerable()));

    public override Nothing Visit(ISubtotalRoot sub)
    {
        ShowSubtotal(sub);
        return Nothing.AtAll;
    }

    public override Nothing Visit(ISubtotalDate sub)
    {
        m_Path.Voucher.Date = sub.Date;
        ShowSubtotal(sub);
        return Nothing.AtAll;
    }

    public override Nothing Visit(ISubtotalUser sub)
    {
        m_Path.User = sub.User;
        ShowSubtotal(sub);
        return Nothing.AtAll;
    }

    public override Nothing Visit(ISubtotalCurrency sub)
    {
        m_Path.Currency = sub.Currency;
        ShowSubtotal(sub);
        return Nothing.AtAll;
    }

    public override Nothing Visit(ISubtotalTitle sub)
    {
        m_Path.Title = sub.Title;
        ShowSubtotal(sub);
        return Nothing.AtAll;
    }

    public override Nothing Visit(ISubtotalSubTitle sub)
    {
        m_Path.SubTitle = sub.SubTitle;
        ShowSubtotal(sub);
        return Nothing.AtAll;
    }

    public override Nothing Visit(ISubtotalContent sub)
    {
        m_Path.Content = sub.Content;
        ShowSubtotal(sub);
        return Nothing.AtAll;
    }

    public override Nothing Visit(ISubtotalRemark sub)
    {
        m_Path.Remark = sub.Remark;
        ShowSubtotal(sub);
        return Nothing.AtAll;
    }
}
