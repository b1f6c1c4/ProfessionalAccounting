/* Copyright (C) 2020-2024 b1f6c1c4
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
    private ValueTask<long> ResetCarryYear(Session session, DateFilter rng)
        => session.Accountant.DeleteVouchersAsync($"{rng.AsDateRange()} AnnualCarry");

    /// <summary>
    ///     年末结转
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="dt">年，若为<c>null</c>则表示对无日期进行结转</param>
    private async IAsyncEnumerable<string> CarryYear(Session session, DateTime? dt)
    {
        DateTime? ed;
        DateFilter rng;
        if (dt.HasValue)
        {
            var sd = new DateTime(dt.Value.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            ed = sd.AddYears(1).AddDays(-1);
            rng = new(sd, ed);
        }
        else
        {
            ed = null;
            rng = DateFilter.TheNullOnly;
        }

        foreach (var grpC in (await session.Accountant.RunGroupedQueryAsync(
                     $"T4103 {rng.AsDateRange()}`Cs")).Items.Cast<ISubtotalCurrency>())
        {
            yield return
                $"{dt.AsDate(SubtotalLevel.Month)} CarryYear => {grpC.Currency.AsCurrency()} {grpC.Fund.AsFund(grpC.Currency)}\n";
            foreach (var grps in grpC.Items.Cast<ISubtotalSubTitle>())
                await session.Accountant.UpsertAsync(new Voucher
                    {
                        Date = ed,
                        Type = VoucherType.AnnualCarry,
                        Details =
                            new()
                                {
                                    new()
                                        {
                                            Currency = grpC.Currency,
                                            Title = 4101,
                                            SubTitle = grps.SubTitle,
                                            Fund = +grps.Fund,
                                        },
                                    new()
                                        {
                                            Currency = grpC.Currency,
                                            Title = 4103,
                                            SubTitle = grps.SubTitle,
                                            Fund = -grps.Fund,
                                        },
                                },
                    });
        }
    }
}
