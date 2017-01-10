using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.Entities;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     检验表达式解释器
    /// </summary>
    internal class CheckShell : IShellComponent
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public CheckShell(Accountant helper) { m_Accountant = helper; }

        /// <inheritdoc />
        public IQueryResult Execute(string expr)
        {
            expr = expr.Rest();
            if (expr == "1")
                return BasicCheck();
            if (expr == "2")
                return AdvancedCheck();

            throw new InvalidOperationException("表达式无效");
        }

        /// <inheritdoc />
        public bool IsExecutable(string expr) => expr.Initital() == "chk";

        private static readonly ConfigManager<CheckedTitles> CheckedTitles =
            new ConfigManager<CheckedTitles>("Chk.xml");

        /// <summary>
        ///     检查每张会计记账凭证借贷方是否相等
        /// </summary>
        /// <returns>有误的会计记账凭证表达式</returns>
        private IQueryResult BasicCheck()
        {
            var sb = new StringBuilder();
            foreach (var voucher in m_Accountant.SelectVouchers(null))
            {
                // ReSharper disable once PossibleInvalidOperationException
                var val = voucher.Details.Sum(d => d.Fund.Value);
                if (val.IsZero())
                    continue;

                sb.AppendLine(val > 0 ? $"/* Debit - Credit = {val:R} */" : $"/* Credit - Debit = {-val:R} */");
                sb.Append(CSharpHelper.PresentVoucher(voucher));
            }
            if (sb.Length > 0)
                return new EditableText(sb.ToString());
            return new Succeed();
        }

        /// <summary>
        ///     检查每科目每内容每日资产无贷方余额，负债无借方余额
        /// </summary>
        /// <returns>发生错误的第一日及其信息</returns>
        private IQueryResult AdvancedCheck()
        {
            var sb = new StringBuilder();
            foreach (var title in CheckedTitles.Config.Titles)
            {
                var res = m_Accountant.RunGroupedQuery($"T{title.Title.AsTitle()}{title.SubTitle.AsSubTitle()}G`cD");

                foreach (var grpContent in res.GroupByContent())
                    foreach (var balance in grpContent.AggregateChangedDay())
                    {
                        if (title.Direction &&
                            balance.Fund.IsNonNegative())
                            continue;
                        if (!title.Direction &&
                            balance.Fund.IsNonPositive())
                            continue;

                        sb.AppendLine(
                                      $"{balance.Date:yyyyMMdd} " +
                                      $"{title.Title.AsTitle()}{title.SubTitle.AsSubTitle()} " +
                                      $"{grpContent.Key}:{balance.Fund:R}");
                        sb.AppendLine();
                        break;
                    }
            }

            if (sb.Length > 0)
                return new EditableText(sb.ToString());
            return new Succeed();
        }
    }

    [Serializable]
    [XmlRoot("CheckedTitles")]
    public class CheckedTitles
    {
        [XmlElement("Title")] public List<CheckedTitle> Titles;
    }

    [Serializable]
    public class CheckedTitle
    {
        /// <summary>
        ///     方向，<c>True</c>表示非负
        /// </summary>
        [XmlAttribute("dir")]
        public bool Direction { get; set; }

        public int Title { get; set; }

        public int? SubTitle { get; set; }
    }
}
