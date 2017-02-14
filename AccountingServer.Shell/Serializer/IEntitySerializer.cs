using AccountingServer.Entities;

namespace AccountingServer.Shell.Serializer
{
    /// <summary>
    ///     表示器
    /// </summary>
    public interface IEntitySerializer
    {
        /// <summary>
        ///     将记账凭证表示
        /// </summary>
        /// <param name="voucher">记账凭证</param>
        /// <returns>表示</returns>
        string PresentVoucher(Voucher voucher);

        /// <summary>
        ///     从表示中取得记账凭证
        /// </summary>
        /// <param name="str">表示</param>
        /// <returns>记账凭证</returns>
        Voucher ParseVoucher(string str);

        /// <summary>
        ///     将细目用C#表示
        /// </summary>
        /// <param name="detail">细目</param>
        /// <returns>C#表达式</returns>
        string PresentVoucherDetail(VoucherDetail detail);

        /// <summary>
        ///     将资产表示
        /// </summary>
        /// <param name="asset">资产</param>
        /// <returns>表示</returns>
        string PresentAsset(Asset asset);

        /// <summary>
        ///     从表示中取得资产
        /// </summary>
        /// <param name="str">表示</param>
        /// <returns>资产</returns>
        Asset ParseAsset(string str);

        /// <summary>
        ///     将摊销表示
        /// </summary>
        /// <param name="amort">摊销</param>
        /// <returns>表示</returns>
        string PresentAmort(Amortization amort);

        /// <summary>
        ///     从表示中取得摊销
        /// </summary>
        /// <param name="str">表示</param>
        /// <returns>摊销</returns>
        Amortization ParseAmort(string str);
    }
}
