using System.Collections.Generic;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Subtotal
{
    /// <summary>
    ///     原始报告结果处理器
    /// </summary>
    internal class RawSubtotal : StringSubtotalVisitor
    {
        private readonly VoucherDetailR m_Path = new VoucherDetailR(new Voucher(), new VoucherDetail());
        private readonly bool m_Separate;
        private List<VoucherDetailR> m_History;

        public RawSubtotal(bool separate = false) => m_Separate = separate;

        private void ShowSubtotal<TC>(ISubtotalResult<TC> sub) where TC : ISubtotalResult
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

        public override Nothing Visit<TC>(ISubtotalRoot<TC> sub)
        {
            ShowSubtotal(sub);
            return Nothing.AtAll;
        }

        public override Nothing Visit<TC>(ISubtotalDate<TC> sub)
        {
            m_Path.Voucher.Date = sub.Date;
            ShowSubtotal(sub);
            return Nothing.AtAll;
        }

        public override Nothing Visit<TC>(ISubtotalUser<TC> sub)
        {
            m_Path.User = sub.User;
            ShowSubtotal(sub);
            return Nothing.AtAll;
        }

        public override Nothing Visit<TC>(ISubtotalCurrency<TC> sub)
        {
            m_Path.Currency = sub.Currency;
            ShowSubtotal(sub);
            return Nothing.AtAll;
        }

        public override Nothing Visit<TC>(ISubtotalTitle<TC> sub)
        {
            m_Path.Title = sub.Title;
            ShowSubtotal(sub);
            return Nothing.AtAll;
        }

        public override Nothing Visit<TC>(ISubtotalSubTitle<TC> sub)
        {
            m_Path.SubTitle = sub.SubTitle;
            ShowSubtotal(sub);
            return Nothing.AtAll;
        }

        public override Nothing Visit<TC>(ISubtotalContent<TC> sub)
        {
            m_Path.Content = sub.Content;
            ShowSubtotal(sub);
            return Nothing.AtAll;
        }

        public override Nothing Visit<TC>(ISubtotalRemark<TC> sub)
        {
            m_Path.Remark = sub.Remark;
            ShowSubtotal(sub);
            return Nothing.AtAll;
        }
    }
}
