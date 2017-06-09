using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;

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
            switch (expr)
            {
                case "1":
                    return BasicCheck();
                case "2":
                    return AdvancedCheck();
                case "3":
                    if (m_Accountant.RunVoucherQuery("A").Any(voucher => !m_Accountant.Upsert(voucher)))
                        throw new ApplicationException("未知错误");

                    return new Succeed();
                default:
                    throw new InvalidOperationException("表达式无效");
            }
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
            foreach (var voucher in m_Accountant.SelectVouchers(VoucherQueryUnconstrained.Instance))
            {
                var flag = false;
                // ReSharper disable once PossibleInvalidOperationException
                var grps = voucher.Details.GroupBy(d => d.Currency, d => d.Fund.Value);
                foreach (var grp in grps)
                {
                    var val = grp.Sum();
                    if (val.IsZero())
                        continue;

                    flag = true;
                    sb.AppendLine(
                        val > 0
                            ? $"/* @{grp.Key}: Debit - Credit = {val:R} */"
                            : $"/* @{grp.Key}: Credit - Debit = {-val:R} */");
                }

                if (flag)
                    sb.Append(m_Serializer.PresentVoucher(voucher).Wrap());
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
                                        .Select(d => (Voucher: v, Detail: d))),
                            $"T{title.Id.AsTitle()}00",
                            sb);
                    else if (Math.Abs(title.Direction) == 2)
                        DoCheck(
                            title.Direction,
                            m_Accountant.RunGroupedQuery($"T{title.Id.AsTitle()}00 G`CcD"),
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
                                            .Select(d => (Voucher: v, Detail: d))),
                            $"T{title.Id.AsTitle()}{subTitle.Id.AsSubTitle()}",
                            sb);
                    else if (Math.Abs(subTitle.Direction) == 2)
                        DoCheck(
                            subTitle.Direction,
                            m_Accountant.RunGroupedQuery($"T{title.Id.AsTitle()}{subTitle.Id.AsSubTitle()} G`CcD"),
                            $"T{title.Id.AsTitle()}{subTitle.Id.AsSubTitle()}",
                            sb);
            }

            if (sb.Length > 0)
                return new EditableText(sb.ToString());

            return new Succeed();
        }

        private static void DoCheck(int dir, ISubtotalResult res, string info, StringBuilder sb)
        {
            foreach (var grpC in res.Items.Cast<ISubtotalCurrency>())
            foreach (var grpc in grpC.Items.Cast<ISubtotalContent>())
            foreach (var grpd in grpc.Items.Cast<ISubtotalDate>())
            {
                if (dir > 0 &&
                    grpd.Fund.IsNonNegative())
                    continue;
                if (dir < 0 &&
                    grpd.Fund.IsNonPositive())
                    continue;

                sb.AppendLine($"{grpd.Date:yyyyMMdd} {info} {grpc.Content}:@{grpC.Currency} {grpd.Fund:R}");
            }
        }

        private static void DoCheck(IEnumerable<(Voucher Voucher, VoucherDetail Detail)> res, string info,
            StringBuilder sb)
        {
            foreach (var d in res)
            {
                // ReSharper disable PossibleInvalidOperationException
                sb.AppendLine(
                    $"{d.Voucher.ID} {d.Voucher.Date:yyyyMMdd} {info} {d.Detail.Content}:{d.Detail.Fund.Value:R}");
                sb.AppendLine();
                // ReSharper restore PossibleInvalidOperationException
            }
        }
    }
}
