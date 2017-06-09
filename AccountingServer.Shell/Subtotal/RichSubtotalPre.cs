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

        private string Ts(double f) => Ga == GatheringType.Count
            ? f.ToString("N0")
            : f.AsCurrency();

        private int? m_Title;
        private int m_Depth;
        private string Idents => new string(' ', (m_Depth > 0 ? m_Depth - 1 : 0) * Ident);

        private void ShowSubtotal(ISubtotalResult sub, string str)
        {
            Sb.AppendLine($"{Idents}{str.CPadRight(38)}{Ts(sub.Fund).CPadLeft(12 + 2 * m_Depth)}");
            VisitChildren(sub);
        }

        private void VisitChildren(ISubtotalResult sub)
        {
            if (sub.Items == null)
                return;

            m_Depth++;
            foreach (var item in sub.Items)
                item.Accept(this);

            m_Depth--;
        }

        public override void Visit(ISubtotalRoot sub)
        {
            Sb.AppendLine($"{Idents}{Ts(sub.Fund)}");
            VisitChildren(sub);
        }

        public override void Visit(ISubtotalDate sub) => ShowSubtotal(sub, sub.Date.AsDate(sub.Level));

        public override void Visit(ISubtotalCurrency sub) => ShowSubtotal(sub, $"@{sub.Currency}");

        public override void Visit(ISubtotalTitle sub)
        {
            m_Title = sub.Title;
            ShowSubtotal(sub, $"{sub.Title.AsTitle()} {TitleManager.GetTitleName(sub.Title)}");
        }

        public override void Visit(ISubtotalSubTitle sub) => ShowSubtotal(
            sub,
            $"{sub.SubTitle.AsSubTitle()} {TitleManager.GetTitleName(m_Title, sub.SubTitle)}");

        public override void Visit(ISubtotalContent sub) => ShowSubtotal(sub, sub.Content.Quotation('\''));

        public override void Visit(ISubtotalRemark sub) => ShowSubtotal(sub, sub.Remark.Quotation('"'));
    }
}
