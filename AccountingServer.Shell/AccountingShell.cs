using System;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     基本表达式解释器
    /// </summary>
    internal class AccountingShell : IShellComponent
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public AccountingShell(Accountant helper) { m_Accountant = helper; }

        /// <inheritdoc />
        public IQueryResult Execute(string s)
        {
            if (s == "?")
                return ListHelp();
            if (s == "T")
                return ListTitles();

            {
                var res = Parsing.GroupedQuery(s);
                if (res != null)
                    return PresentSubtotal(res);
            }

            {
                var l = s.Length;
                var res = Parsing.VoucherQuery(ref s);
                if (l != s.Length)
                    if (res != null)
                        return PresentVoucherQuery(res);
            }

            throw new InvalidOperationException("表达式无效");
        }

        /// <inheritdoc />
        public bool IsExecutable(string expr) => true;

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
            foreach (var title in TitleManager.Titles)
            {
                sb.AppendLine($"{title.Id.AsTitle()}\t\t{title.Name}");
                foreach (var subTitle in title.SubTitles)
                    sb.AppendLine($"{title.Id.AsTitle()}{subTitle.Id.AsSubTitle()}\t\t{subTitle.Name}");
            }
            return new UnEditableText(sb.ToString());
        }
    }
}
