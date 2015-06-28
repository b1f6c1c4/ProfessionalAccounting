using System;
using System.Text;
using AccountingServer.Entities;

namespace AccountingServer.Shell
{
    public partial class AccountingShell
    {
        /// <summary>
        ///     更新或添加记账凭证
        /// </summary>
        /// <param name="code">记账凭证的C#代码</param>
        /// <returns>新记账凭证的C#代码</returns>
        public string ExecuteVoucherUpsert(string code)
        {
            var voucher = CSharpHelper.ParseVoucher(code);

            if (!m_Accountant.Upsert(voucher))
                throw new ApplicationException("更新或添加失败");

            return CSharpHelper.PresentVoucher(voucher);
        }

        /// <summary>
        ///     删除记账凭证
        /// </summary>
        /// <param name="code">记账凭证的C#代码</param>
        /// <returns>是否成功</returns>
        public bool ExecuteVoucherRemoval(string code)
        {
            var voucher = CSharpHelper.ParseVoucher(code);

            if (voucher.ID == null)
                throw new ApplicationException("编号未知");

            return m_Accountant.DeleteVoucher(voucher.ID);
        }

        /// <summary>
        ///     执行记账凭证检索式并呈现记账凭证
        /// </summary>
        /// <param name="query">记账凭证检索式</param>
        /// <returns>记账凭证的C#表达式</returns>
        private IQueryResult PresentVoucherQuery(IQueryCompunded<IVoucherQueryAtom> query)
        {
            var sb = new StringBuilder();
            foreach (var voucher in m_Accountant.SelectVouchers(query))
                sb.Append(CSharpHelper.PresentVoucher(voucher));
            return new EditableText(sb.ToString());
        }
    }
}
