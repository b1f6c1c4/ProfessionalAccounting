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
    private long ResetCarryYear(DateFilter rng)
        => m_Accountant.DeleteVouchers($"{rng.AsDateRange()} AnnualCarry");

    /// <summary>
    ///     年末结转
    /// </summary>
    /// <param name="sb">日志记录</param>
    /// <param name="dt">年，若为<c>null</c>则表示对无日期进行结转</param>
    private void CarryYear(StringBuilder sb, DateTime? dt)
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

        foreach (var grpC in m_Accountant.RunGroupedQuery(
                     $"T4103 {rng.AsDateRange()}`Cs").Items.Cast<ISubtotalCurrency>())
        {
            sb.AppendLine(
                $"{dt.AsDate(SubtotalLevel.Month)} CarryYear => @{grpC.Currency} {grpC.Fund.AsCurrency(grpC.Currency)}");
            foreach (var grps in grpC.Items.Cast<ISubtotalSubTitle>())
                m_Accountant.Upsert(new Voucher
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
