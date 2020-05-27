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
        private string Idents => new string(' ', (Depth > 0 ? Depth - 1 : 0) * Ident);

        private string Ts(double f) => Ga == GatheringType.Count || Ga == GatheringType.VoucherCount
            ? f.ToString("N0")
            : f.AsCurrency(Cu ?? m_Currency);

        private void ShowSubtotal<TC>(ISubtotalResult<TC> sub, string str) where TC : ISubtotalResult
        {
            Sb.AppendLine($"{Idents}{str.CPadRight(38)}{Ts(sub.Fund).CPadLeft(12 + 2 * Depth)}");
            VisitChildren(sub);
        }

        public override Nothing Visit<TC>(ISubtotalRoot<TC> sub)
        {
            Sb.AppendLine($"{Idents}{Ts(sub.Fund)}");
            VisitChildren(sub);
            return Nothing.AtAll;
        }

        public override Nothing Visit<TC>(ISubtotalDate<TC> sub)
        {
            ShowSubtotal(sub, sub.Date.AsDate(sub.Level));
            return Nothing.AtAll;
        }

        public override Nothing Visit<TC>(ISubtotalUser<TC> sub)
        {
            ShowSubtotal(sub, $"U{sub.User.AsUser()}");
            return Nothing.AtAll;
        }

        public override Nothing Visit<TC>(ISubtotalCurrency<TC> sub)
        {
            m_Currency = sub.Currency;
            ShowSubtotal(sub, $"@{sub.Currency}");
            m_Currency = null;
            return Nothing.AtAll;
        }

        public override Nothing Visit<TC>(ISubtotalTitle<TC> sub)
        {
            m_Title = sub.Title;
            ShowSubtotal(sub, $"{sub.Title.AsTitle()} {TitleManager.GetTitleName(sub.Title)}");
            return Nothing.AtAll;
        }

        public override Nothing Visit<TC>(ISubtotalSubTitle<TC> sub)
        {
            ShowSubtotal(
                sub,
                $"{sub.SubTitle.AsSubTitle()} {TitleManager.GetTitleName(m_Title, sub.SubTitle)}");
            return Nothing.AtAll;
        }

        public override Nothing Visit<TC>(ISubtotalContent<TC> sub)
        {
            ShowSubtotal(sub, sub.Content.Quotation('\''));
            return Nothing.AtAll;
        }

        public override Nothing Visit<TC>(ISubtotalRemark<TC> sub)
        {
            ShowSubtotal(sub, sub.Remark.Quotation('"'));
            return Nothing.AtAll;
        }
    }
}
