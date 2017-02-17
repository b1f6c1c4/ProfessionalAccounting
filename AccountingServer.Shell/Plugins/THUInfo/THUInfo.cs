using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using CredentialManagement;
using static AccountingServer.BLL.Parsing.FacadeF;

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

        private static readonly IQueryCompunded<IDetailQueryAtom> DetailQuery =
            new DetailQueryAryBase(
                OperatorType.Substract,
                new IQueryCompunded<IDetailQueryAtom>[]
                    {
                        new DetailQueryAtomBase(new VoucherDetail { Title = 1012, SubTitle = 05 }),
                        new DetailQueryAryBase(
                            OperatorType.Union,
                            new IQueryCompunded<IDetailQueryAtom>[]
                                {
                                    new DetailQueryAtomBase(new VoucherDetail { Remark = "" }),
                                    new DetailQueryAtomBase(new VoucherDetail { Remark = IgnoranceMark })
                                }
                        )
                    });

        private static readonly ConfigManager<EndPointTemplates> EndPointTemplates;

        private static IReadOnlyList<EndPointTemplate> Templates => EndPointTemplates.Config.Templates.AsReadOnly();

        private readonly Crawler m_Crawler;

        private readonly object m_Lock = new object();

        static THUInfo() { EndPointTemplates = new ConfigManager<EndPointTemplates>("EndPoint.xml"); }

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
            sb.AppendLine("---Generated");

            List<TransactionRecord> fail;
            foreach (var voucher in AutoGenerate(problems.Records, out fail))
            {
                Accountant.Upsert(voucher);
                sb.AppendLine(Serializer.PresentVoucher(voucher));
            }

            if (fail.Any())
            {
                sb.AppendLine("---Can not generate");
                foreach (var r in fail)
                    sb.AppendLine(r.ToString());
            }

            return new EditableText(sb.ToString());
        }

        /// <summary>
        ///     列出终端列表
        /// </summary>
        /// <returns>执行结果</returns>
        private IQueryResult ShowEndPoints()
        {
            var sb = new StringBuilder();
            var voucherQuery = new VoucherQueryAtomBase { DetailFilter = DetailQuery };
            var bin = new HashSet<int>();
            foreach (var d in Accountant.SelectVouchers(voucherQuery)
                .SelectMany(
                    v => v.Details.Where(d => d.IsMatch(DetailQuery))
                        .Select(d => new VDetail { Detail = d, Voucher = v })))
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
                sb.AppendLine($"{d.Voucher.ID,28}{d.Detail.Remark.CPadRight(20)}{content.CPadRight(20)}{record.Endpoint}");
            }

            return new UnEditableText(sb.ToString());
        }

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
