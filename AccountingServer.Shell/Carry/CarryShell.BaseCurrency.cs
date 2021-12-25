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
    private static IQueryResult ListHistory(DateFilter rng)
    {
        var sb = new StringBuilder();

        foreach (var info in BaseCurrency.History)
            if (info.Date.Within(rng))
                sb.AppendLine($"{info.Date.AsDate().PadLeft(8)} @{info.Currency}");

        return new PlainText(sb.ToString());
    }

    /// <summary>
    ///     取消摊销
    /// </summary>
    /// <param name="rng">过滤器</param>
    /// <returns>执行结果</returns>
    private long ResetConversion(DateFilter rng)
        => m_Accountant.DeleteVouchers($"{rng.AsDateRange()} %equity conversion%");

    /// <summary>
    ///     所有者权益币种转换
    /// </summary>
    /// <param name="sb">日志记录</param>
    /// <param name="dt">日期</param>
    /// <param name="to">目标币种</param>
    /// <returns>记账凭证</returns>
    private IEnumerable<Voucher> ConvertEquity(StringBuilder sb, DateTime dt, string to)
    {
        var rst = m_Accountant.RunGroupedQuery($"T4101+T4103-@{to} [~{dt.AsDate()}]`Cts");

        foreach (var grpC in rst.Items.Cast<ISubtotalCurrency>())
        {
            var rate = m_Accountant.Query(dt, grpC.Currency, to);
            var dst = rate * grpC.Fund;
            sb.AppendLine(
                $"=== {dt.AsDate()} @{grpC.Currency} {grpC.Fund.AsCurrency(grpC.Currency)} => @{to} {dst.AsCurrency(to)}");

            foreach (var grpt in grpC.Items.Cast<ISubtotalTitle>())
            foreach (var grps in grpt.Items.Cast<ISubtotalSubTitle>())
                yield return new Voucher
                    {
                        Date = dt,
                        Type = VoucherType.Ordinary,
                        Remark = "equity conversion",
                        Details =
                            new()
                                {
                                    new()
                                        {
                                            Title = grpt.Title,
                                            SubTitle = grps.SubTitle,
                                            Currency = grpC.Currency,
                                            Fund = -grps.Fund,
                                        },
                                    new()
                                        {
                                            Title = grpt.Title,
                                            SubTitle = grps.SubTitle,
                                            Currency = to,
                                            Fund = grps.Fund * rate,
                                        },
                                    new() { Title = 3999, Currency = grpC.Currency, Fund = grps.Fund },
                                    new() { Title = 3999, Currency = to, Fund = -grps.Fund * rate },
                                },
                    };
        }
    }
}
