using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;

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

        /// <summary>
        ///     表示器
        /// </summary>
        private readonly IEntitySerializer m_Serializer;

        public CheckShell(Accountant helper, IEntitySerializer serializer)
        {
            m_Accountant = helper;
            m_Serializer = serializer;
        }

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
                sb.Append(m_Serializer.PresentVoucher(voucher));
            }

            if (sb.Length > 0)
                return new EditableText(sb.ToString());

            return new Succeed();
        }

        /// <summary>
        ///     检查每科目每内容借贷方向
        /// </summary>
        /// <returns>发生错误的信息</returns>
        private IQueryResult AdvancedCheck()
        {
            var sb = new StringBuilder();
            foreach (var title in TitleManager.Titles)
            {
                if (!title.IsVirtual)
                    if (Math.Abs(title.Direction) == 1)
                        DoCheck(
                            m_Accountant.RunVoucherQuery(
                                    $"T{title.Id.AsTitle()}00 {(title.Direction < 0 ? ">" : "<")} G")
                                .SelectMany(
                                    v => v.Details.Where(d => d.Title == title.Id)
                                        .Select(d => new Tuple<Voucher, VoucherDetail>(v, d))),
                            $"T{title.Id.AsTitle()}00",
                            sb);
                    else if (Math.Abs(title.Direction) == 2)
                        DoCheck(
                            title.Direction,
                            m_Accountant.RunGroupedQuery($"T{title.Id.AsTitle()}00 G`cD"),
                            $"T{title.Id.AsTitle()}00",
                            sb);

                foreach (var subTitle in title.SubTitles)
                    if (Math.Abs(subTitle.Direction) == 1)
                        DoCheck(
                            m_Accountant.RunVoucherQuery(
                                    $"T{title.Id.AsTitle()}{subTitle.Id.AsSubTitle()} {(subTitle.Direction < 0 ? ">" : "<")} G")
                                .SelectMany(
                                    v =>
                                        v.Details.Where(
                                                d =>
                                                    d.Title == title.Id && d.SubTitle == subTitle.Id)
                                            .Select(d => new Tuple<Voucher, VoucherDetail>(v, d))),
                            $"T{title.Id.AsTitle()}{subTitle.Id.AsSubTitle()}",
                            sb);
                    else if (Math.Abs(subTitle.Direction) == 2)
                        DoCheck(
                            subTitle.Direction,
                            m_Accountant.RunGroupedQuery($"T{title.Id.AsTitle()}{subTitle.Id.AsSubTitle()} G`cD"),
                            $"T{title.Id.AsTitle()}{subTitle.Id.AsSubTitle()}",
                            sb);
            }

            if (sb.Length > 0)
                return new EditableText(sb.ToString());

            return new Succeed();
        }

        private static void DoCheck(int dir, IEnumerable<Balance> res, string info, StringBuilder sb)
        {
            foreach (var grpContent in res.GroupByContent())
            foreach (var balance in grpContent.AggregateChangedDay())
            {
                if (dir > 0 &&
                    balance.Fund.IsNonNegative())
                    continue;
                if (dir < 0 &&
                    balance.Fund.IsNonPositive())
                    continue;

                sb.AppendLine($"{balance.Date:yyyyMMdd} {info} {grpContent.Key}:{balance.Fund:R}");
            }
        }

        private static void DoCheck(IEnumerable<Tuple<Voucher, VoucherDetail>> res, string info,
            StringBuilder sb)
        {
            foreach (var d in res)
            {
                // ReSharper disable PossibleInvalidOperationException
                sb.AppendLine($"{d.Item1.ID} {d.Item1.Date:yyyyMMdd} {info} {d.Item2.Content}:{d.Item2.Fund.Value:R}");
                sb.AppendLine();
                // ReSharper restore PossibleInvalidOperationException
            }
        }
    }
}
