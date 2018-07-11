using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;

namespace AccountingServer.Shell.Subtotal
{
    /// <summary>
    ///     原始报告结果处理器
    /// </summary>
    internal class RawSubtotal : StringSubtotalVisitor
    {
        private static readonly IEntitySerializer Serializer = new ExprSerializer();
        private readonly VoucherDetail m_Path = new VoucherDetail();

        private void ShowSubtotal(ISubtotalResult sub)
        {
            if (sub.Items == null)
            {
                m_Path.Fund = sub.Fund;
                Sb.Append(Serializer.PresentVoucherDetail(m_Path));
            }

            VisitChildren(sub);
        }

        public override void Visit(ISubtotalRoot sub) => ShowSubtotal(sub);

        public override void Visit(ISubtotalDate sub)
        {
            Sb.AppendLine(sub.Date.AsDate());
            ShowSubtotal(sub);
        }

        public override void Visit(ISubtotalCurrency sub)
        {
            m_Path.Currency = sub.Currency;
            ShowSubtotal(sub);
        }

        public override void Visit(ISubtotalTitle sub)
        {
            m_Path.Title = sub.Title;
            ShowSubtotal(sub);
        }

        public override void Visit(ISubtotalSubTitle sub)
        {
            m_Path.SubTitle = sub.SubTitle;
            ShowSubtotal(sub);
        }

        public override void Visit(ISubtotalContent sub)
        {
            m_Path.Content = sub.Content;
            ShowSubtotal(sub);
        }

        public override void Visit(ISubtotalRemark sub)
        {
            m_Path.Remark = sub.Remark;
            ShowSubtotal(sub);
        }
    }
}
