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
using System.Text;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Plugins.BankBalance;

/// <summary>
///     计算日均余额
/// </summary>
internal class AverageDailyBalance : PluginBase
{
    /// <inheritdoc />
    public override async IAsyncEnumerable<string> Execute(string expr, Context ctx)
    {
        var content = Parsing.Token(ref expr);
        var avg = Parsing.DoubleF(ref expr);
        Parsing.Eof(expr);

        var tdy = ctx.Client.Today;
        var ldom = DateHelper.LastDayOfMonth(tdy.Year, tdy.Month);
        var srng = new DateFilter(new(tdy.Year, tdy.Month, 1, 0, 0, 0, DateTimeKind.Utc), tdy);
        var balance = await ctx.Accountant.RunGroupedQueryAsync(
            $"T1002 {content.Quotation('\'')} [~{tdy.AsDate()}]`vD{srng.AsDateRange()}");

        var bal = 0D;
        var btd = 0D;
        foreach (var b in balance.Items.Cast<ISubtotalDate>())
            if (b.Date == tdy)
                btd += b.Fund;
            else
                bal += b.Fund;

        var targ = ldom.Day * avg;

        var sb = new StringBuilder();
        sb.Append($"Target: {targ.AsFund()}\n");
        sb.Append($"Balance until yesterday: {bal.AsFund()}\n");
        if ((bal - targ).IsNonNegative())
        {
            sb.Append("Achieved.\n");
            sb.Append("\n");

            sb.Append(
                (btd - avg).IsNonNegative()
                    ? $"Plan A: Credit {(btd - avg).AsFund()}, Balance {avg.AsFund()}\n"
                    : $"Plan A: Debit {(avg - btd).AsFund()}, Balance {avg.AsFund()}\n");
            sb.Append("Plan B: No Action\n");
        }
        else
        {
            var res = targ - bal;
            var rsd = ldom.Day - tdy.Day + 1;
            sb.Append($"Deficiency: {res.AsFund()}\n");
            var avx = res / rsd;
            if ((rsd * avg - res).IsNonNegative())
            {
                sb.Append($"Average deficiency: {avx.AsFund()} <= {avg.AsFund()}\n");
                sb.Append("\n");

                sb.Append(
                    (btd - avx).IsNonNegative()
                        ? $"Plan A: Credit {(btd - avx).AsFund()}, Balance {avx.AsFund()}\n"
                        : $"Plan A: Debit {(avx - btd).AsFund()}, Balance {avx.AsFund()}\n");
                sb.Append(
                    (btd - avg).IsNonNegative()
                        ? $"Plan B: Credit {(btd - avg).AsFund()}, Balance {avg.AsFund()}\n"
                        : $"Plan B: Debit {(avg - btd).AsFund()}, Balance {avg.AsFund()}\n");
            }
            else
            {
                sb.Append($"Average deficiency: {avx.AsFund()} > {avg.AsFund()}\n");
                sb.Append("\n");

                sb.Append(
                    (btd - avx).IsNonNegative()
                        ? $"Plan: Credit {(btd - avx).AsFund()}, Balance {avx.AsFund()}\n"
                        : $"Plan: Debit {(avx - btd).AsFund()}, Balance {avx.AsFund()}\n");
            }
        }

        yield return sb.ToString();
    }
}
