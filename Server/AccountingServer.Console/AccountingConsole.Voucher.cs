using System;

namespace AccountingServer.Console
{
    public partial class AccountingConsole
    {
        /// <summary>
        ///     更新或添加记账凭证
        /// </summary>
        /// <param name="code">记账凭证的C#代码</param>
        /// <returns>新记账凭证的C#代码</returns>
        public string ExecuteVoucherUpsert(string code)
        {
            var voucher = CSharpHelper.ParseVoucher(code);

            if (voucher.ID == null)
            {
                if (!m_Accountant.Upsert(voucher))
                    throw new Exception();
            }
            else if (!m_Accountant.Update(voucher))
                throw new Exception();

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
                throw new Exception();

            return m_Accountant.DeleteVoucher(voucher.ID);
        }
    }
}
