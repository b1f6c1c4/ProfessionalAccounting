using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.BLL.Util;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using CredentialManagement;

namespace AccountingServer.Shell.Plugins.THUInfo
{
    /// <summary>
    ///     从info.tsinghua.edu.cn更新账户
    /// </summary>
    internal partial class THUInfo : PluginBase
    {
        /// <summary>
        ///     对账忽略标志
        /// </summary>
        private const string IgnoranceMark = "reconciliation";

        public static IConfigManager<EndPointTemplates> EndPointTemplates { private get; set; } =
            new ConfigManager<EndPointTemplates>("EndPoint.xml");

        private static IReadOnlyList<EndPointTemplate> Templates => EndPointTemplates.Config.Templates.AsReadOnly();

        private readonly Crawler m_Crawler;

        private readonly object m_Lock = new object();


        public THUInfo(Accountant accountant, IEntitySerializer serializer) : base(accountant, serializer)
        {
            m_Crawler = new Crawler();
            Task.Run(() => FetchData());
        }

        /// <summary>
        ///     读取凭证并获取数据
        /// </summary>
        private void FetchData()
        {
            var cred = CredentialTemplate();
            if (cred.Exists())
                cred.Load();
            else
            {
                cred = PromptForCredential();
                if (cred == null)
                    return;
            }

            lock (m_Lock)
                m_Crawler.FetchData(cred.Username, cred.Password);
        }

        private static Credential CredentialTemplate() =>
            new Credential
                {
                    Target = "THUInfo",
                    PersistanceType = PersistanceType.Enterprise,
                    Type = CredentialType.DomainVisiblePassword
                };

        /// <summary>
        ///     删除保存的凭证
        /// </summary>
        private static void DropCredential()
        {
            var cred = CredentialTemplate();
            cred.Delete();
        }

        /// <summary>
        ///     提示输入凭证
        /// </summary>
        /// <returns>凭证</returns>
        private static Credential PromptForCredential()
        {
            var prompt = new XPPrompt
                {
                    Target = "THUInfo",
                    Persist = true
                };
            if (prompt.ShowDialog() != DialogResult.OK)
                return null;

            var cred = CredentialTemplate();
            cred.Username = prompt.Username.Split(new[] { '\\' }, 2)[1];
            cred.Password = prompt.Password;

            cred.Save();
            return cred;
        }

        private IQueryResult DoCompare(bool force, out Problems problems)
        {
            lock (m_Lock)
                problems = Compare();

            if (problems.Any || force)
                return ShowComparison(problems);

            return null;
        }

        /// <inheritdoc />
        public override IQueryResult Execute(string expr)
        {
            Problems problems;

            expr = expr.TrimStart();
            switch (expr)
            {
                case "ep":
                    return ShowEndPoints();
                case "raw":
                    return ShowRawRecords();
                case "cred":
                    DropCredential();
                    FetchData();
                    return new Succeed();
                case "whatif":
                    return DoCompare(true, out problems);
                case "":
                    break;
                default:
                    throw new InvalidOperationException("表达式无效");
            }

            var t = DoCompare(false, out problems);
            if (t != null)
                return t;

            var sb = new StringBuilder();

            var vouchers = AutoGenerate(problems.Records, out List<TransactionRecord> fail);
            foreach (var voucher in vouchers)
            {
                Accountant.Upsert(voucher);
                sb.AppendLine(Serializer.PresentVoucher(voucher).Wrap());
            }

            if (fail.Any())
            {
                sb.AppendLine("---Can not generate");
                foreach (var r in fail)
                    sb.AppendLine(r.ToString());
            }

            if (DoCompare(true, out var _) is EditableText cmp)
                sb.AppendLine(cmp.ToString());

            if (sb.Length <= 0)
                return new Succeed();

            sb.Insert(0, "---Generated" + Environment.NewLine);
            return new EditableText(sb.ToString());
        }

        /// <summary>
        ///     列出终端列表
        /// </summary>
        /// <returns>执行结果</returns>
        private IQueryResult ShowEndPoints()
        {
            var sb = new StringBuilder();
            var bin = new HashSet<int>();
            foreach (var d in GetVDetails())
            {
                var id = Convert.ToInt32(d.Detail.Remark);
                if (!bin.Add(id))
                    continue;

                var record = m_Crawler.Result.SingleOrDefault(r => r.Index == id);
                if (record == null)
                    continue;
                // ReSharper disable once PossibleInvalidOperationException
                if (!(Math.Abs(d.Detail.Fund.Value) - record.Fund).IsZero())
                    continue;

                var tmp = d.Voucher.Details.Where(dd => dd.Title == 6602).ToArray();
                var content = tmp.Length == 1 ? tmp.First().Content : string.Empty;
                sb.AppendLine(
                    $"{d.Voucher.ID,28}{d.Detail.Remark.CPadRight(20)}{content.CPadRight(20)}{record.Endpoint}");
            }

            return new UnEditableText(sb.ToString());
        }

        private IEnumerable<VDetail> GetVDetails() =>
            Accountant.RunVoucherQuery("T101205")
                .SelectMany(
                    v =>
                        v.Details.Where(
                                d =>
                                    d.Title == 1012 && d.SubTitle == 05
                                    && d.Remark != null && d.Remark != IgnoranceMark)
                            .Select(d => new VDetail { Detail = d, Voucher = v }));

        /// <summary>
        ///     列出原始记录
        /// </summary>
        /// <returns>执行结果</returns>
        private IQueryResult ShowRawRecords()
        {
            var sb = new StringBuilder();

            lock (m_Lock)
                foreach (var record in m_Crawler.Result)
                    sb.AppendLine(record.ToString());

            return new UnEditableText(sb.ToString());
        }
    }
}
