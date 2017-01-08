using System;
using System.Linq;
using AccountingServer.BLL;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     表达式解释器
    /// </summary>
    public class Facade : IShellComponent
    {
        private readonly Accountant m_Accountant;

        private readonly ShellComposer m_Composer;

        public Facade(Accountant helper)
        {
            m_Accountant = helper;

            m_Composer =
                new ShellComposer
                    {
                        new CheckShell(helper),
                        new CarryShell(helper),
                        new AssetShell(helper),
                        new AmortizationShell(helper),
                        new PluginShell(helper),
                        new AccountingShell(helper)
                    };
        }

        /// <inheritdoc />
        public IQueryResult Execute(string expr) => m_Composer.Execute(expr);

        /// <inheritdoc />
        public bool IsExecutable(string expr) => m_Composer.IsExecutable(expr);

        #region Miscellaneous

        /// <summary>
        ///     检查是否已连接；若未连接，尝试连接
        /// </summary>
        public IQueryResult AutoConnect()
        {
            if (!m_Accountant.Connected)
                m_Accountant.Connect();
            return new Succeed();
        }

        /// <summary>
        ///     连接数据库服务器
        /// </summary>
        /// <returns>连接情况</returns>
        private IQueryResult ConnectServer()
        {
            m_Accountant.Connect();
            return new Succeed();
        }

        #endregion

        #region Upsert

        /// <summary>
        ///     更新或添加记账凭证
        /// </summary>
        /// <param name="code">记账凭证的C#代码</param>
        /// <returns>新记账凭证的C#代码</returns>
        public string ExecuteVoucherUpsert(string code)
        {
            var voucher = CSharpHelper.ParseVoucher(code);
            // ReSharper disable once PossibleInvalidOperationException
            var unc = voucher.Details.SingleOrDefault(d => !d.Fund.HasValue);
            if (unc != null)
                // ReSharper disable once PossibleInvalidOperationException
                unc.Fund = -voucher.Details.Sum(d => d.Fund ?? 0D);

            if (!m_Accountant.Upsert(voucher))
                throw new ApplicationException("更新或添加失败");

            return CSharpHelper.PresentVoucher(voucher);
        }

        /// <summary>
        ///     更新或添加资产
        /// </summary>
        /// <param name="code">资产的C#代码</param>
        /// <returns>新资产的C#代码</returns>
        public string ExecuteAssetUpsert(string code)
        {
            var asset = CSharpHelper.ParseAsset(code);

            if (!m_Accountant.Upsert(asset))
                throw new ApplicationException("更新或添加失败");

            return CSharpHelper.PresentAsset(asset);
        }

        /// <summary>
        ///     更新或添加摊销
        /// </summary>
        /// <param name="code">摊销的C#代码</param>
        /// <returns>新摊销的C#代码</returns>
        public string ExecuteAmortUpsert(string code)
        {
            var amort = CSharpHelper.ParseAmort(code);

            if (!m_Accountant.Upsert(amort))
                throw new ApplicationException("更新或添加失败");

            return CSharpHelper.PresentAmort(amort);
        }

        #endregion

        #region Removal

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
        ///     删除资产
        /// </summary>
        /// <param name="code">资产的C#代码</param>
        /// <returns>是否成功</returns>
        public bool ExecuteAssetRemoval(string code)
        {
            var asset = CSharpHelper.ParseAsset(code);

            if (!asset.ID.HasValue)
                throw new ApplicationException("编号未知");

            return m_Accountant.DeleteAsset(asset.ID.Value);
        }

        /// <summary>
        ///     删除摊销
        /// </summary>
        /// <param name="code">摊销的C#代码</param>
        /// <returns>是否成功</returns>
        public bool ExecuteAmortRemoval(string code)
        {
            var amort = CSharpHelper.ParseAmort(code);

            if (!amort.ID.HasValue)
                throw new ApplicationException("编号未知");

            return m_Accountant.DeleteAmortization(amort.ID.Value);
        }

        #endregion
    }
}
