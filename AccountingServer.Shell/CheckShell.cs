/* Copyright (C) 2020-2025 b1f6c1c4
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
    public IAsyncEnumerable<string> Execute(string expr, Context ctx, string term)
        => expr.Rest() switch
            {
                "1" => BasicCheck(ctx),
                "2" => AdvancedCheck(ctx),
                "3" => UpsertCheck(ctx),
                var x when x.StartsWith("4", StringComparison.Ordinal) => DuplicationCheck(ctx, x.Rest()),
                _ => throw new InvalidOperationException("表达式无效"),
            };

    /// <inheritdoc />
    public bool IsExecutable(string expr) => expr.Initial() == "chk";

    /// <summary>
    ///     检查每张会计记账凭证借贷方是否相等
    /// </summary>
    /// <param name="ctx">客户端上下文</param>
    /// <returns>有误的会计记账凭证表达式</returns>
    private async IAsyncEnumerable<string> BasicCheck(Context ctx)
    {
        ctx.Identity.WillInvoke("chk-1");
        Voucher old = null;
        await foreach (var (voucher, user, curr, v) in
                       ctx.Accountant.SelectUnbalancedVouchersAsync(VoucherQueryUnconstrained.Instance))
        {
            if (old != null && voucher.ID != old.ID)
                yield return ctx.Serializer.PresentVoucher(old).Wrap();
            old = voucher;

            yield return $"/* {user.AsUser()} {curr.AsCurrency()}: Debit - Credit = {v:R} */\n";
        }

        if (old != null)
            yield return ctx.Serializer.PresentVoucher(old).Wrap();
    }

    /// <summary>
    ///     检查每科目每内容借贷方向
    /// </summary>
    /// <param name="ctx">客户端上下文</param>
    /// <returns>发生错误的信息</returns>
    private async IAsyncEnumerable<string> AdvancedCheck(Context ctx)
    {
        ctx.Identity.WillInvoke("chk-2");
        foreach (var title in TitleManager.Titles)
        {
            if (!title.IsVirtual)
                if (Math.Abs(title.Direction) == 1)
                    await foreach (var s in DoCheck(
                                       ctx.Accountant
                                           .RunVoucherQueryAsync(
                                               $"{title.Id.AsTitle()}00 {(title.Direction < 0 ? ">" : "<")} G")
                                           .SelectMany(v
                                               => v.Details.Where(d => d.Title == title.Id)
                                                   .Select(d => (Voucher: v, Detail: d)).ToAsyncEnumerable()),
                                       $"{title.Id.AsTitle()}00"))
                        yield return s;
                else if (Math.Abs(title.Direction) == 2)
                    foreach (var s in DoCheck(
                                 title.Direction,
                                 await ctx.Accountant.RunGroupedQueryAsync($"{title.Id.AsTitle()}00 G`CcD"),
                                 $"{title.Id.AsTitle()}00"))
                        yield return s;

            foreach (var subTitle in title.SubTitles)
                if (Math.Abs(subTitle.Direction) == 1)
                    await foreach (var s in DoCheck(
                                       ctx.Accountant.RunVoucherQueryAsync(
                                               $"{title.Id.AsTitle()}{subTitle.Id.AsSubTitle()} {(subTitle.Direction < 0 ? ">" : "<")} G")
                                           .SelectMany(v
                                               => v.Details.Where(d => d.Title == title.Id && d.SubTitle == subTitle.Id)
                                                   .Select(d => (Voucher: v, Detail: d)).ToAsyncEnumerable()),
                                       $"{title.Id.AsTitle()}{subTitle.Id.AsSubTitle()}"))
                        yield return s;
                else if (Math.Abs(subTitle.Direction) == 2)
                    foreach (var s in DoCheck(
                                 subTitle.Direction,
                                 await ctx.Accountant.RunGroupedQueryAsync(
                                     $"{title.Id.AsTitle()}{subTitle.Id.AsSubTitle()} G`CcD"),
                                 $"{title.Id.AsTitle()}{subTitle.Id.AsSubTitle()}"))
                        yield return s;
        }
    }

    private static IEnumerable<string> DoCheck(int dir, ISubtotalResult res, string info)
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
                    yield return $"{grpd.Date:yyyyMMdd} {info} {grpc.Content}:{grpC.Currency.AsCurrency()} {grpd.Fund:R}\n";
                    break;
            }
    }

    private static async IAsyncEnumerable<string> DoCheck(IAsyncEnumerable<(Voucher Voucher, VoucherDetail Detail)> res,
        string info)
    {
        await foreach (var (v, d) in res)
        {
            if (d.Remark == "reconciliation")
                continue;

            yield return $"{v.ID} {v.Date:yyyyMMdd} {info} {d.Content}:{d.Fund!.Value:R}\n";
        }
    }

    private async IAsyncEnumerable<string> UpsertCheck(Context ctx)
    {
        ctx.Identity.WillInvoke("chk-3");
        {
            yield return "Reading vouchers...\n";
            var lst = await ctx.Accountant.SelectVouchersAsync(VoucherQueryUnconstrained.Instance).ToListAsync();
            yield return $"Read {lst.Count} vouchers, writing...\n";
            await ctx.Accountant.UpsertAsync(lst);
        }
        {
            yield return "Reading assets...\n";
            var lst = await ctx.Accountant.SelectAssetsAsync(DistributedQueryUnconstrained.Instance).ToListAsync();
            yield return $"Read {lst.Count} assets, writing...\n";
            await ctx.Accountant.UpsertAsync(lst);
        }
        {
            yield return "Reading amorts...\n";
            var lst = await ctx.Accountant.SelectAmortizationsAsync(DistributedQueryUnconstrained.Instance).ToListAsync();
            yield return $"Read {lst.Count} amorts, writing...\n";
            await ctx.Accountant.UpsertAsync(lst);
        }
        yield return "Written\n";
    }

    private async IAsyncEnumerable<string> DuplicationCheck(Context ctx, string expr)
    {
        ctx.Identity.WillInvoke("chk-4");
        var query = Parsing.VoucherQuery(ref expr, ctx.Client);
        Parsing.Eof(expr);
        await foreach (var (v, ids) in ctx.Accountant.SelectDuplicatedVouchersAsync(query))
        {
            yield return $"// Date = {v.Date.AsDate()} Duplication = {ids.Count}\n";
            foreach (var id in ids)
                yield return $"//   ^{id}^\n";
            yield return ctx.Serializer.PresentVoucher(v).Wrap();
        }
    }
}
