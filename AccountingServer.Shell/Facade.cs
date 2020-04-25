using System;
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
    public class Facade
    {
        /// <summary>
        ///     默认表示器
        /// </summary>
        private static readonly IEntitiesSerializer DefaultSerializer
            = new TrivialEntitiesSerializer(
                AlternativeSerializer.Compose(
                    new DiscountSerializer(),
                    new AbbrSerializer(),
                    new CSharpSerializer()));

        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        /// <summary>
        ///     复合表达式解释器
        /// </summary>
        private readonly ShellComposer m_Composer;

        public Facade()
        {
            m_Accountant = new Accountant();
            m_Composer =
                new ShellComposer
                    {
                        new CheckShell(m_Accountant),
                        new CarryShell(m_Accountant),
                        new CarryYearShell(m_Accountant),
                        new ExchangeShell(m_Accountant),
                        new BaseCurrencyShell(m_Accountant),
                        new AssetShell(m_Accountant),
                        new AmortizationShell(m_Accountant),
                        new PluginShell(m_Accountant),
                        new AccountingShell(m_Accountant),
                    };
        }

        /// <summary>
        ///     空记账凭证的表示
        /// </summary>
        // ReSharper disable once MemberCanBeMadeStatic.Global
        public string EmptyVoucher(string spec) => GetSerializer(spec).PresentVoucher(null).Wrap();

        /// <summary>
        ///     从表示器代号寻找表示器
        /// </summary>
        /// <param name="spec">表示器代号</param>
        /// <returns>表示器</returns>
        private static IEntitiesSerializer GetSerializer(string spec)
        {
            if (string.IsNullOrWhiteSpace(spec))
                return DefaultSerializer;

            switch (spec.Initital())
            {
                case "abbr":
                    return new TrivialEntitiesSerializer(new AbbrSerializer());
                case "csharp":
                    return new TrivialEntitiesSerializer(new CSharpSerializer());
                case "discount":
                    return new TrivialEntitiesSerializer(new DiscountSerializer());
                case "expr":
                    return new TrivialEntitiesSerializer(new ExprSerializer());
                case "json":
                    return new JsonSerializer();
                case "csv":
                    return new CsvSerializer(spec.Rest());
                default:
                    throw new ArgumentException("表示器未知", nameof(spec));
            }
        }

        /// <summary>
        ///     执行表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <param name="spec">表示器代号</param>
        /// <returns>执行结果</returns>
        public IQueryResult Execute(string expr, string spec)
        {
            switch (expr)
            {
                case "T":
                    return ListTitles();
                case "?":
                    return ListHelp();
                case "reload":
                    return new NumberAffected(MetaConfigManager.ReloadAll());
                case "die":
                    Environment.Exit(0);
                    break;
            }

            return m_Composer.Execute(expr, GetSerializer(spec));
        }

        #region Miscellaneous

        /// <summary>
        ///     显示控制台帮助
        /// </summary>
        /// <returns>帮助内容</returns>
        private static IQueryResult ListHelp()
        {
            const string resName = "AccountingServer.Shell.Resources.Document.txt";
            using (var stream = typeof(Facade).Assembly.GetManifestResourceStream(resName))
            {
                if (stream == null)
                    throw new MissingManifestResourceException();

                using (var reader = new StreamReader(stream))
                    return new PlainText(reader.ReadToEnd());
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

            return new PlainText(sb.ToString());
        }

        #endregion

        #region Upsert

        /// <summary>
        ///     更新或添加记账凭证
        /// </summary>
        /// <param name="str">记账凭证的表达式</param>
        /// <param name="spec">表示器代号</param>
        /// <returns>新记账凭证的表达式</returns>
        public string ExecuteVoucherUpsert(string str, string spec)
        {
            var serializer = GetSerializer(spec);

            var voucher = serializer.ParseVoucher(str);
            var grpCs = voucher.Details.GroupBy(d => d.Currency ?? BaseCurrency.Now);
            var grpUs = voucher.Details.GroupBy(d => d.User ?? ClientUser.Name);
            foreach (var grpC in grpCs)
            {
                var unc = grpC.SingleOrDefault(d => !d.Fund.HasValue);
                if (unc != null)
                    unc.Fund = -grpC.Sum(d => d.Fund ?? 0D);
            }

            if (grpCs.Count() == 1 && grpUs.Count() > 1)
                foreach (var grpU in grpUs)
                {
                    var sum = grpU.Sum(d => d.Fund.Value);
                    if (sum.IsZero())
                        continue;

                    voucher.Details.Add(
                        new VoucherDetail { User = grpU.Key, Currency = grpCs.First().Key, Title = 3998, Fund = -sum });
                }
            else if (grpCs.Count() > 1 && grpUs.Count() == 1)
                foreach (var grpC in grpCs)
                {
                    var sum = grpC.Sum(d => d.Fund.Value);
                    if (sum.IsZero())
                        continue;

                    voucher.Details.Add(
                        new VoucherDetail { User = grpUs.First().Key, Currency = grpC.Key, Title = 3999, Fund = -sum });
                }

            if (!m_Accountant.Upsert(voucher))
                throw new ApplicationException("更新或添加失败");

            return serializer.PresentVoucher(voucher).Wrap();
        }

        /// <summary>
        ///     更新或添加资产
        /// </summary>
        /// <param name="str">资产表达式</param>
        /// <param name="spec">表示器代号</param>
        /// <returns>新资产表达式</returns>
        public string ExecuteAssetUpsert(string str, string spec)
        {
            var serializer = GetSerializer(spec);
            var asset = serializer.ParseAsset(str);

            if (!m_Accountant.Upsert(asset))
                throw new ApplicationException("更新或添加失败");

            return serializer.PresentAsset(asset).Wrap();
        }

        /// <summary>
        ///     更新或添加摊销
        /// </summary>
        /// <param name="str">摊销表达式</param>
        /// <param name="spec">表示器代号</param>
        /// <returns>新摊销表达式</returns>
        public string ExecuteAmortUpsert(string str, string spec)
        {
            var serializer = GetSerializer(spec);
            var amort = serializer.ParseAmort(str);

            if (!m_Accountant.Upsert(amort))
                throw new ApplicationException("更新或添加失败");

            return serializer.PresentAmort(amort).Wrap();
        }

        #endregion

        #region Removal

        /// <summary>
        ///     删除记账凭证
        /// </summary>
        /// <param name="str">记账凭证表达式</param>
        /// <param name="spec">表示器代号</param>
        /// <returns>是否成功</returns>
        public bool ExecuteVoucherRemoval(string str, string spec)
        {
            var serializer = GetSerializer(spec);
            var voucher = serializer.ParseVoucher(str);

            if (voucher.ID == null)
                throw new ApplicationException("编号未知");

            return m_Accountant.DeleteVoucher(voucher.ID);
        }

        /// <summary>
        ///     删除资产
        /// </summary>
        /// <param name="str">资产表达式</param>
        /// <param name="spec">表示器代号</param>
        /// <returns>是否成功</returns>
        public bool ExecuteAssetRemoval(string str, string spec)
        {
            var serializer = GetSerializer(spec);
            var asset = serializer.ParseAsset(str);

            if (!asset.ID.HasValue)
                throw new ApplicationException("编号未知");

            return m_Accountant.DeleteAsset(asset.ID.Value);
        }

        /// <summary>
        ///     删除摊销
        /// </summary>
        /// <param name="str">摊销表达式</param>
        /// <param name="spec">表示器代号</param>
        /// <returns>是否成功</returns>
        public bool ExecuteAmortRemoval(string str, string spec)
        {
            var serializer = GetSerializer(spec);
            var amort = serializer.ParseAmort(str);

            if (!amort.ID.HasValue)
                throw new ApplicationException("编号未知");

            return m_Accountant.DeleteAmortization(amort.ID.Value);
        }

        #endregion
    }
}
