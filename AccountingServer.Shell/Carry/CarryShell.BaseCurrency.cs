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
using System.Threading.Tasks;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Util;

namespace AccountingServer.Shell.Carry;

internal partial class CarryShell
{
    /// <summary>
    ///     列出记账本位币变更历史
    /// </summary>
    /// <param name="rng">过滤器</param>
    /// <returns>执行结果</returns>
    private static IEnumerable<string> ListHistory(DateFilter rng)
    {
        foreach (var info in BaseCurrency.History)
            if (info.Date.Within(rng))
                yield return $"{info.Date.AsDate(),8} {info.Currency.AsCurrency()}\n";
    }

    /// <summary>
    ///     取消摊销
    /// </summary>
    /// <param name="ctx">客户端上下文</param>
    /// <param name="rng">过滤器</param>
    /// <returns>执行结果</returns>
    private ValueTask<long> ResetConversion(Context ctx, DateFilter rng)
        => ctx.Accountant.DeleteVouchersAsync($"{rng.AsDateRange()} %equity conversion% AnnualCarry");

    /// <summary>
    ///     所有者权益币种转换
    /// </summary>
    /// <param name="ctx">客户端上下文</param>
    /// <param name="dt">日期</param>
    /// <param name="to">目标币种</param>
    private async IAsyncEnumerable<string> ConvertEquity(Context ctx, DateTime dt, string to)
    {
        var rst = await ctx.Accountant.RunGroupedQueryAsync($"T4101+T4103-{to.AsCurrency()} [~{dt.AsDate()}]`Cts");

        foreach (var grpC in rst.Items.Cast<ISubtotalCurrency>())
        {
            var rate = await ctx.Accountant.Query(dt, grpC.Currency, to);
            var dst = rate * grpC.Fund;
            yield return
                $"=== {dt.AsDate()} {grpC.Currency} {grpC.Fund.AsFund(grpC.Currency)} => {to.AsCurrency()} {dst.AsFund(to)}\n";

            foreach (var grpt in grpC.Items.Cast<ISubtotalTitle>())
            foreach (var grps in grpt.Items.Cast<ISubtotalSubTitle>())
                await ctx.Accountant.UpsertAsync(new Voucher
                    {
                        Date = dt,
                        Type = VoucherType.AnnualCarry,
                        Remark = "equity conversion",
                        Details =
                            new()
                                {
                                    new()
                                        {
                                            Currency = grpC.Currency,
                                            Title = grpt.Title,
                                            SubTitle = grps.SubTitle,
                                            Fund = -grps.Fund,
                                        },
                                    new()
                                        {
                                            Currency = to,
                                            Title = grpt.Title,
                                            SubTitle = grps.SubTitle,
                                            Fund = grps.Fund * rate,
                                        },
                                    new() { Title = 3999, Currency = grpC.Currency, Fund = grps.Fund },
                                    new() { Title = 3999, Currency = to, Fund = -grps.Fund * rate },
                                },
                    });
        }
    }
}
