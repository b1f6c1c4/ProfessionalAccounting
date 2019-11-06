using System.Collections.Generic;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Subtotal
{
    /// <summary>
    ///     原始报告结果处理器
    /// </summary>
    internal class RawSubtotal : StringSubtotalVisitor
    {
        private readonly bool m_Separate;
        private List<VoucherDetailR> m_History;
        private readonly VoucherDetailR m_Path = new VoucherDetailR(new Voucher(), new VoucherDetail());

        public RawSubtotal(bool separate = false) => m_Separate = separate;

        private void ShowSubtotal(ISubtotalResult sub)
        {
            if (sub.Items == null)
            {
                m_Path.Fund = sub.Fund;
                if (m_Separate)
                    Sb.Append(Serializer.PresentVoucherDetail(m_Path));
                else
                    m_History.Add(new VoucherDetailR(m_Path));
            }

            VisitChildren(sub);
        }

        protected override void Pre() => m_History = new List<VoucherDetailR>();

        protected override void Post() => Sb.Append(Serializer.PresentVoucherDetails(m_History));

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
}
