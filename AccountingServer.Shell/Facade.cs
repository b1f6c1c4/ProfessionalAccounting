using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Carry;
using AccountingServer.Shell.Serializer;

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
        private readonly IEntitiesSerializer m_Serializer;

        public Facade()
        {
            m_Accountant = new Accountant();
            m_Serializer = new TrivialEntitiesSerializer(
                new AlternativeSerializer(
                    new DiscountSerializer(),
                    new AlternativeSerializer(new AbbrSerializer(), new CSharpSerializer())));
            m_Composer =
                new ShellComposer
                    {
                        new CheckShell(m_Accountant, m_Serializer),
                        new CarryShell(m_Accountant),
                        new CarryYearShell(m_Accountant),
                        new ExchangeShell(),
                        new AssetShell(m_Accountant, m_Serializer),
                        new AmortizationShell(m_Accountant, m_Serializer),
                        new PluginShell(m_Accountant, m_Serializer),
                        new AccountingShell(m_Accountant, m_Serializer)
                    };
        }

        /// <summary>
        ///     空记账凭证的表示
        /// </summary>
        public string EmptyVoucher => m_Serializer.PresentVoucher(null).Wrap();

        /// <inheritdoc />
        public IQueryResult Execute(string expr)
        {
            switch (expr)
            {
                case "T":
                    return ListTitles();
                case "?":
                    return ListHelp();
            }

            return m_Composer.Execute(expr);
        }

        /// <inheritdoc />
        public bool IsExecutable(string expr) => throw new InvalidOperationException();

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
                sb.AppendLine($"{title.Id.AsTitle(),-8}{title.Name}");
                foreach (var subTitle in title.SubTitles)
                    sb.AppendLine($"{title.Id.AsTitle()}{subTitle.Id.AsSubTitle(),-4}{subTitle.Name}");
            }

            return new UnEditableText(sb.ToString());
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
            var lst = new List<VoucherDetail>();

            var voucher = m_Serializer.ParseVoucher(code);
            foreach (var grp in voucher.Details.GroupBy(d => d.Currency ?? BaseCurrency.Now))
            {
                var unc = grp.SingleOrDefault(d => !d.Fund.HasValue);
                var sum = grp.Sum(d => d.Fund ?? 0D);
                if (unc != null)
                {
                    lst = null;
                    unc.Fund = -sum;
                    continue;
                }

                // ReSharper disable once PossibleInvalidOperationException
                if (!sum.IsZero())
                    lst?.Add(
                        new VoucherDetail
                            {
                                Currency = grp.Key,
                                Title = 3999,
                                Fund = -sum
                            });
            }

            if (lst != null &&
                lst.Count >= 2)
                voucher.Details.AddRange(lst);

            if (!m_Accountant.Upsert(voucher))
                throw new ApplicationException("更新或添加失败");

            return m_Serializer.PresentVoucher(voucher).Wrap();
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
    }
}
