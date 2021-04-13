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

using AccountingServer.BLL.Util;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Subtotal
{
    /// <summary>
    ///     分类汇总结果处理器
    /// </summary>
    internal class RichSubtotalPre : StringSubtotalVisitor
    {
        private const int Ident = 4;

        private string m_Currency;

        private int? m_Title;
        private string Idents => new(' ', (Depth > 0 ? Depth - 1 : 0) * Ident);

        private string Ts(double f) => Ga is GatheringType.Count or GatheringType.VoucherCount
            ? f.ToString("N0")
            : f.AsCurrency(Cu ?? m_Currency);

        private void ShowSubtotal(ISubtotalResult sub, string str)
        {
            Sb.AppendLine($"{Idents}{str.CPadRight(38)}{Ts(sub.Fund).CPadLeft(12 + 2 * Depth)}");
            VisitChildren(sub);
        }

        public override Nothing Visit(ISubtotalRoot sub)
        {
            Sb.AppendLine($"{Idents}{Ts(sub.Fund)}");
            VisitChildren(sub);
            return Nothing.AtAll;
        }

        public override Nothing Visit(ISubtotalDate sub)
        {
            ShowSubtotal(sub, sub.Date.AsDate(sub.Level));
            return Nothing.AtAll;
        }

        public override Nothing Visit(ISubtotalUser sub)
        {
            ShowSubtotal(sub, $"U{sub.User.AsUser()}");
            return Nothing.AtAll;
        }

        public override Nothing Visit(ISubtotalCurrency sub)
        {
            m_Currency = sub.Currency;
            ShowSubtotal(sub, $"@{sub.Currency}");
            m_Currency = null;
            return Nothing.AtAll;
        }

        public override Nothing Visit(ISubtotalTitle sub)
        {
            m_Title = sub.Title;
            ShowSubtotal(sub, $"{sub.Title.AsTitle()} {TitleManager.GetTitleName(sub.Title)}");
            return Nothing.AtAll;
        }

        public override Nothing Visit(ISubtotalSubTitle sub)
        {
            ShowSubtotal(
                sub,
                $"{sub.SubTitle.AsSubTitle()} {TitleManager.GetTitleName(m_Title, sub.SubTitle)}");
            return Nothing.AtAll;
        }

        public override Nothing Visit(ISubtotalContent sub)
        {
            ShowSubtotal(sub, sub.Content.Quotation('\''));
            return Nothing.AtAll;
        }

        public override Nothing Visit(ISubtotalRemark sub)
        {
            ShowSubtotal(sub, sub.Remark.Quotation('"'));
            return Nothing.AtAll;
        }
    }
}
