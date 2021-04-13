/* Copyright (C) 2020-2021 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
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

        public CheckShell(Accountant helper) => m_Accountant = helper;

        /// <inheritdoc />
        public IQueryResult Execute(string expr, IEntitiesSerializer serializer)
            => expr.Rest() switch
                {
                    "1" => BasicCheck(serializer),
                    "2" => AdvancedCheck(),
                    "3" => m_Accountant.RunVoucherQuery("A").Any(voucher => !m_Accountant.Upsert(voucher))
                        ? throw new ApplicationException("未知错误")
                        : new DirtySucceed(),
                    _ => throw new InvalidOperationException("表达式无效"),
                };

        /// <inheritdoc />
        public bool IsExecutable(string expr) => expr.Initial() == "chk";

        /// <summary>
        ///     检查每张会计记账凭证借贷方是否相等
        /// </summary>
        /// <param name="serializer">表示器</param>
        /// <returns>有误的会计记账凭证表达式</returns>
        private IQueryResult BasicCheck(IEntitySerializer serializer)
        {
            var sb = new StringBuilder();
            foreach (var voucher in m_Accountant.SelectVouchers(VoucherQueryUnconstrained.Instance))
            {
                var flag = false;
                var grps = voucher.Details
                    // ReSharper disable once PossibleInvalidOperationException
                    .GroupBy(d => new { d.User, d.Currency }, d => d.Fund.Value);
                foreach (var grp in grps)
                {
                    var val = grp.Sum();
                    if (val.IsZero())
                        continue;

                    flag = true;
                    sb.AppendLine(
                        val > 0
                            ? $"/* U{grp.Key.User.AsUser()} @{grp.Key.Currency}: Debit - Credit = {val:R} */"
                            : $"/* U{grp.Key.User.AsUser()} @{grp.Key.Currency}: Credit - Debit = {-val:R} */");
                }

                if (flag)
                    sb.Append(serializer.PresentVoucher(voucher).Wrap());
            }

            if (sb.Length > 0)
                return new PlainText(sb.ToString());

            return new PlainSucceed();
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
                return new PlainText(sb.ToString());

            return new PlainSucceed();
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
            foreach (var (v, d) in res)
            {
                if (d.Remark == "reconciliation")
                    continue;

                // ReSharper disable PossibleInvalidOperationException
                sb.AppendLine(
                    $"{v.ID} {v.Date:yyyyMMdd} {info} {d.Content}:{d.Fund.Value:R}");
                sb.AppendLine();
                // ReSharper restore PossibleInvalidOperationException
            }
        }
    }
}
