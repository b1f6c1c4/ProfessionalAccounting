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
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell;

/// <summary>
///     检验表达式解释器
/// </summary>
internal class CheckShell : IShellComponent
{
    /// <inheritdoc />
    public IQueryResult Execute(string expr, Session session)
        => expr.Rest() switch
            {
                "1" => BasicCheck(session),
                "2" => AdvancedCheck(session),
                "3" => new NumberAffected(session.Accountant.Upsert(session.Accountant.RunVoucherQuery("U A").ToList())),
                var x when x.StartsWith("4") => DuplicationCheck(session, x.Rest()),
                _ => throw new InvalidOperationException("表达式无效"),
            };

    /// <inheritdoc />
    public bool IsExecutable(string expr) => expr.Initial() == "chk";

    /// <summary>
    ///     检查每张会计记账凭证借贷方是否相等
    /// </summary>
    /// <param name="session"></param>
    /// <returns>有误的会计记账凭证表达式</returns>
    private IQueryResult BasicCheck(Session session)
    {
        var sb = new StringBuilder();
        Voucher old = null;
        foreach (var (voucher, user, curr, v) in
                 session.Accountant.SelectUnbalancedVouchers(VoucherQueryUnconstrained.Instance))
        {
            if (old == null)
                old = voucher;
            else if (voucher.ID != old.ID)
            {
                sb.Append(session.Serializer.PresentVoucher(old).Wrap());
                sb.AppendLine();
            }

            sb.AppendLine($"/* U{user.AsUser()} @{curr}: Debit - Credit = {v:R} */");
        }

        if (old != null)
            sb.Append(session.Serializer.PresentVoucher(old).Wrap());

        if (sb.Length > 0)
            return new PlainText(sb.ToString());

        return new PlainSucceed();
    }

    /// <summary>
    ///     检查每科目每内容借贷方向
    /// </summary>
    /// <param name="session"></param>
    /// <returns>发生错误的信息</returns>
    private IQueryResult AdvancedCheck(Session session)
    {
        var sb = new StringBuilder();
        foreach (var title in TitleManager.Titles)
        {
            if (!title.IsVirtual)
                if (Math.Abs(title.Direction) == 1)
                    DoCheck(
                        session.Accountant.RunVoucherQuery(
                                $"T{title.Id.AsTitle()}00 {(title.Direction < 0 ? ">" : "<")} G")
                            .SelectMany(
                                v => v.Details.Where(d => d.Title == title.Id)
                                    .Select(d => (Voucher: v, Detail: d))),
                        $"T{title.Id.AsTitle()}00",
                        sb);
                else if (Math.Abs(title.Direction) == 2)
                    DoCheck(
                        title.Direction,
                        session.Accountant.RunGroupedQuery($"T{title.Id.AsTitle()}00 G`CcD"),
                        $"T{title.Id.AsTitle()}00",
                        sb);

            foreach (var subTitle in title.SubTitles)
                if (Math.Abs(subTitle.Direction) == 1)
                    DoCheck(
                        session.Accountant.RunVoucherQuery(
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
                        session.Accountant.RunGroupedQuery($"T{title.Id.AsTitle()}{subTitle.Id.AsSubTitle()} G`CcD"),
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
            switch (dir)
            {
                case > 0 when grpd.Fund.IsNonNegative():
                case < 0 when grpd.Fund.IsNonPositive():
                    continue;
                default:
                    sb.AppendLine($"{grpd.Date:yyyyMMdd} {info} {grpc.Content}:@{grpC.Currency} {grpd.Fund:R}");
                    break;
            }
    }

    private static void DoCheck(IEnumerable<(Voucher Voucher, VoucherDetail Detail)> res, string info,
        StringBuilder sb)
    {
        foreach (var (v, d) in res)
        {
            if (d.Remark == "reconciliation")
                continue;

            sb.AppendLine(
                $"{v.ID} {v.Date:yyyyMMdd} {info} {d.Content}:{d.Fund!.Value:R}");
            sb.AppendLine();
        }
    }

    private IQueryResult DuplicationCheck(Session session, string expr)
    {
        var sb = new StringBuilder();
        var query = Parsing.VoucherQuery(ref expr, session.Accountant.Client);
        Parsing.Eof(expr);
        foreach (var (v, ids) in session.Accountant.SelectDuplicatedVouchers(query))
        {
            sb.AppendLine($"// Date = {v.Date.AsDate()} Duplication = {ids.Count}");
            foreach (var id in ids)
                sb.AppendLine($"//   ^{id}^");
            sb.AppendLine(session.Serializer.PresentVoucher(v).Wrap());
        }

        if (sb.Length > 0)
            return new PlainText(sb.ToString());

        return new PlainSucceed();
    }
}
