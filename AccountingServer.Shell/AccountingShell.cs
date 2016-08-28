using System;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell.Parsing;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     表达式解释器
    /// </summary>
    public class AccountingShell
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        private readonly AmortizationShell m_AmortizationShell;
        private readonly AssetShell m_AssetShell;
        private readonly CarryShell m_CarryShell;
        private readonly CheckShell m_CheckShell;
        private readonly NamedQueryShell m_NamedQueryShell;
        private readonly ReportShell m_ReportShell;
        private readonly ChartShell m_ChartShell;

        public PluginShell PluginManager { get; set; }

        public AccountingShell(Accountant helper)
        {
            m_Accountant = helper;
            m_AmortizationShell = new AmortizationShell(helper);
            m_AssetShell = new AssetShell(helper);
            m_CarryShell = new CarryShell(helper);
            m_CheckShell = new CheckShell(helper);
            m_NamedQueryShell = new NamedQueryShell(helper);
            m_ReportShell = new ReportShell(helper);
            m_ChartShell = new ChartShell(helper);
        }

        /// <summary>
        ///     执行表达式
        /// </summary>
        /// <param name="s">表达式</param>
        /// <returns>执行结果</returns>
        public IQueryResult Execute(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                throw new ArgumentNullException(nameof(s));

            var res = ShellParser.From(s + Environment.NewLine).commandEOF();
            if (res.exception != null)
                throw new ArgumentException(res.exception.ToString(), nameof(s));
            var result = res.command();
            var text = result.GetText();
            var j = 0;
            for (var i = 0; i < text.Length; i++,j++)
            {
                if (text[i] == s[j])
                    continue;
                if (s[j] != ' ' ||
                    j == s.Length - 1)
                    throw new ArgumentException("语法错误", nameof(s));
                i--;
            }
            while (j != s.Length &&
                   s[j] == ' ')
                j++;
            if (j != s.Length)
                throw new ArgumentException("语法错误", nameof(s));


            if (result.autoCommand() != null)
                return PluginManager.ExecuteAuto(result.autoCommand());
            if (result.vouchers() != null)
                return PresentVoucherQuery(result.vouchers());
            if (result.groupedQuery() != null)
                return PresentSubtotal(result.groupedQuery());
            if (result.chart() != null)
                return m_ChartShell.ExecuteChartQuery(result.chart());
            if (result.report() != null)
                return m_ReportShell.ExecuteReportQuery(
                                                        result.report(),
                                                        result.report().Sheer != null,
                                                        result.report().Op.Text == "Rp");
            if (result.asset() != null)
                return m_AssetShell.ExecuteAsset(result.asset());
            if (result.amort() != null)
                return m_AmortizationShell.ExecuteAmort(result.amort());
            if (result.carry() != null)
                return m_CarryShell.ExecuteCarry(result.carry());
            if (result.otherCommand() != null)
            {
                if (result.otherCommand().Launch() != null)
                    return LaunchServer();
                if (result.otherCommand().Connect() != null)
                    return ConnectServer();
                if (result.otherCommand().Backup() != null)
                    return Backup();
                if (result.otherCommand().Titles() != null)
                    return ListTitles();
                if (result.otherCommand().Help() != null)
                {
                    var plgName = result.otherCommand().DollarQuotedString();
                    if (plgName != null)
                        return new UnEditableText(PluginManager.GetHelp(plgName.Dequotation()));
                    return ListHelp();
                }
                if (result.otherCommand().Check() != null)
                {
                    switch (result.otherCommand().Check().GetText())
                    {
                        case "chk1":
                            return m_CheckShell.BasicCheck();
                        case "chk2":
                            return m_CheckShell.AdvancedCheck();
                    }
                    throw new ArgumentException("表达式类型未知", nameof(s));
                }
                if (result.otherCommand().EditNamedQueries() != null)
                    return m_NamedQueryShell.ListNamedQueryTemplates();
                if (result.otherCommand().Exit() != null)
                    Environment.Exit(0);
                throw new ArgumentException("表达式类型未知", nameof(s));
            }
            throw new ArgumentException("表达式类型未知", nameof(s));
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

        /// <summary>
        ///     执行分类汇总检索式并呈现结果
        /// </summary>
        /// <param name="query">分类汇总检索式</param>
        /// <returns>执行结果</returns>
        private IQueryResult PresentSubtotal(IGroupedQuery query)
        {
            var result = m_Accountant.SelectVoucherDetailsGrouped(query);

            return new UnEditableText(SubtotalHelper.PresentSubtotal(result, query.Subtotal));
        }

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
            foreach (var title in TitleManager.GetTitles())
                sb.AppendLine($"{title.Item1.AsTitle()}{title.Item2.AsSubTitle()}\t\t{title.Item3}");
            return new UnEditableText(sb.ToString());
        }

        #region Miscellaneous

        /// <summary>
        ///     检查是否已连接；若未连接，尝试连接
        /// </summary>
        public IQueryResult AutoConnect()
        {
            if (!m_Accountant.Connected)
            {
                m_Accountant.Launch();
                m_Accountant.Connect();
            }
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

        /// <summary>
        ///     启动数据库服务器
        /// </summary>
        /// <returns>启动情况</returns>
        private IQueryResult LaunchServer()
        {
            m_Accountant.Launch();
            return new Succeed();
        }

        /// <summary>
        ///     备份数据库
        /// </summary>
        /// <returns>备份情况</returns>
        private IQueryResult Backup()
        {
            m_Accountant.Backup();
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

        /// <summary>
        ///     更新或添加命名查询模板
        /// </summary>
        /// <param name="code">命名查询模板表达式</param>
        /// <returns>命名查询模板表达式</returns>
        public string ExecuteNamedQueryTemplateUpsert(string code)
        {
            string name;
            var all = m_NamedQueryShell.ParseNamedQueryTemplate(code, out name);

            if (!m_Accountant.Upsert(name, all))
                throw new ApplicationException("更新或添加失败");

            return $"@new NamedQueryTemplate {{{all}}}@";
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

        /// <summary>
        ///     删除命名查询模板
        /// </summary>
        /// <param name="code">命名查询模板表达式</param>
        /// <returns>是否成功</returns>
        public bool ExecuteNamedQueryTemplateRemoval(string code)
        {
            string name;
            m_NamedQueryShell.ParseNamedQueryTemplate(code, out name);

            return m_Accountant.DeleteNamedQueryTemplate(name);
        }

        #endregion
    }
}
