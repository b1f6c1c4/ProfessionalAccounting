using System;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Carry;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     表达式解释器
    /// </summary>
    public class Facade : IShellComponent
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        /// <summary>
        ///     复合表达式解释器
        /// </summary>
        private readonly ShellComposer m_Composer;

        /// <summary>
        ///     表示器
        /// </summary>
        private readonly IEntitySerializer m_Serializer;

        public Facade()
        {
            m_Accountant = new Accountant();
            m_Serializer = new AlternativeSerializer(new AbbrSerializer(), new CSharpSerializer());
            m_Composer =
                new ShellComposer
                    {
                        new CheckShell(m_Accountant, m_Serializer),
                        new CarryShell(m_Accountant),
                        new CarryYearShell(m_Accountant),
                        new AssetShell(m_Accountant, m_Serializer),
                        new AmortizationShell(m_Accountant, m_Serializer),
                        new PluginShell(m_Accountant, m_Serializer),
                        new AccountingShell(m_Accountant, m_Serializer)
                    };

            ConnectServer(null);
        }

        /// <inheritdoc />
        public IQueryResult Execute(string expr)
        {
            switch (expr)
            {
                case "exit":
                    Environment.Exit(0);
                    break;
                case "T":
                    return ListTitles();
                case "?":
                    return ListHelp();
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (expr.StartsWith("con", StringComparison.Ordinal))
                return ConnectServer(expr.Substring(3).TrimStart());

            return m_Composer.Execute(expr);
        }

        /// <inheritdoc />
        public bool IsExecutable(string expr) { throw new InvalidOperationException(); }

        #region Miscellaneous

        /// <summary>
        ///     显示控制台帮助
        /// </summary>
        /// <returns>帮助内容</returns>
        private static IQueryResult ListHelp()
        {
            const string resName = "AccountingServer.Shell.Resources.Document.txt";
            using (var stream = typeof(AccountingShell).Assembly.GetManifestResourceStream(resName))
            {
                if (stream == null)
                    throw new MissingManifestResourceException();

                using (var reader = new StreamReader(stream))
                    return new UnEditableText(reader.ReadToEnd());
            }
        }

        /// <summary>
        ///     显示所有会计科目及其编号
        /// </summary>
        /// <returns>会计科目及其编号</returns>
        private static IQueryResult ListTitles()
        {
            var sb = new StringBuilder();
            foreach (var title in TitleManager.Titles)
            {
                sb.AppendLine($"{title.Id.AsTitle()}\t\t{title.Name}");
                foreach (var subTitle in title.SubTitles)
                    sb.AppendLine($"{title.Id.AsTitle()}{subTitle.Id.AsSubTitle()}\t\t{subTitle.Name}");
            }

            return new UnEditableText(sb.ToString());
        }

        /// <summary>
        ///     连接数据库服务器
        /// </summary>
        /// <param name="uri">地址</param>
        /// <returns>连接情况</returns>
        private IQueryResult ConnectServer(string uri)
        {
            if (!m_Accountant.Connected)
                m_Accountant.Connect(uri);
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
            var voucher = m_Serializer.ParseVoucher(code);
            // ReSharper disable once PossibleInvalidOperationException
            var unc = voucher.Details.SingleOrDefault(d => !d.Fund.HasValue);
            if (unc != null)
                // ReSharper disable once PossibleInvalidOperationException
                unc.Fund = -voucher.Details.Sum(d => d.Fund ?? 0D);

            if (!m_Accountant.Upsert(voucher))
                throw new ApplicationException("更新或添加失败");

            return m_Serializer.PresentVoucher(voucher);
        }

        /// <summary>
        ///     更新或添加资产
        /// </summary>
        /// <param name="code">资产的C#代码</param>
        /// <returns>新资产的C#代码</returns>
        public string ExecuteAssetUpsert(string code)
        {
            var asset = m_Serializer.ParseAsset(code);

            if (!m_Accountant.Upsert(asset))
                throw new ApplicationException("更新或添加失败");

            return m_Serializer.PresentAsset(asset);
        }

        /// <summary>
        ///     更新或添加摊销
        /// </summary>
        /// <param name="code">摊销的C#代码</param>
        /// <returns>新摊销的C#代码</returns>
        public string ExecuteAmortUpsert(string code)
        {
            var amort = m_Serializer.ParseAmort(code);

            if (!m_Accountant.Upsert(amort))
                throw new ApplicationException("更新或添加失败");

            return m_Serializer.PresentAmort(amort);
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
            var voucher = m_Serializer.ParseVoucher(code);

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
            var asset = m_Serializer.ParseAsset(code);

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
            var amort = m_Serializer.ParseAmort(code);

            if (!amort.ID.HasValue)
                throw new ApplicationException("编号未知");

            return m_Accountant.DeleteAmortization(amort.ID.Value);
        }

        #endregion

        /// <summary>
        ///     空记账凭证的表示
        /// </summary>
        public string EmptyVoucher => m_Serializer.PresentVoucher(null);
    }

    internal class AlternativeSerializer : IEntitySerializer
    {
        /// <summary>
        ///     主要表示器
        /// </summary>
        private readonly IEntitySerializer m_Primary;

        /// <summary>
        ///     次要表示器
        /// </summary>
        private readonly IEntitySerializer m_Secondary;

        public AlternativeSerializer(IEntitySerializer primary, IEntitySerializer secondary)
        {
            m_Primary = primary;
            m_Secondary = secondary;
        }

        private TOut Run<TOut>(Func<IEntitySerializer, TOut> func)
        {
            try
            {
                return func(m_Primary);
            }
            catch (NotImplementedException)
            {
                return func(m_Secondary);
            }
        }

        public string PresentVoucher(Voucher voucher) => Run(s => s.PresentVoucher(voucher));
        public Voucher ParseVoucher(string str) => Run(s => s.ParseVoucher(str));
        public string PresentAsset(Asset asset) => Run(s => s.PresentAsset(asset));
        public Asset ParseAsset(string str) => Run(s => s.ParseAsset(str));
        public string PresentAmort(Amortization amort) => Run(s => s.PresentAmort(amort));
        public Amortization ParseAmort(string str) => Run(s => s.ParseAmort(str));
    }
}
