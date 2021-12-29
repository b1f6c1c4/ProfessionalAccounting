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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public override ValueTask<IQueryResult> Execute(string expr, Session session)
    {
        var content = Parsing.Token(ref expr);
        var avg = Parsing.DoubleF(ref expr);
        Parsing.Eof(expr);

        var tdy = session.Client.Today;
        var ldom = DateHelper.LastDayOfMonth(tdy.Year, tdy.Month);
        var srng = new DateFilter(new(tdy.Year, tdy.Month, 1, 0, 0, 0, DateTimeKind.Utc), tdy);
        var balance = session.Accountant.RunGroupedQuery(
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
        sb.AppendLine($"Target: {targ.AsCurrency()}");
        sb.AppendLine($"Balance until yesterday: {bal.AsCurrency()}");
        if ((bal - targ).IsNonNegative())
        {
            sb.AppendLine("Achieved.");
            sb.AppendLine();

            sb.AppendLine(
                (btd - avg).IsNonNegative()
                    ? $"Plan A: Credit {(btd - avg).AsCurrency()}, Balance {avg.AsCurrency()}"
                    : $"Plan A: Debit {(avg - btd).AsCurrency()}, Balance {avg.AsCurrency()}");
            sb.AppendLine("Plan B: No Action");
        }
        else
        {
            var res = targ - bal;
            var rsd = ldom.Day - tdy.Day + 1;
            sb.AppendLine($"Deficiency: {res.AsCurrency()}");
            var avx = res / rsd;
            if ((rsd * avg - res).IsNonNegative())
            {
                sb.AppendLine($"Average deficiency: {avx.AsCurrency()} <= {avg.AsCurrency()}");
                sb.AppendLine();

                sb.AppendLine(
                    (btd - avx).IsNonNegative()
                        ? $"Plan A: Credit {(btd - avx).AsCurrency()}, Balance {avx.AsCurrency()}"
                        : $"Plan A: Debit {(avx - btd).AsCurrency()}, Balance {avx.AsCurrency()}");
                sb.AppendLine(
                    (btd - avg).IsNonNegative()
                        ? $"Plan B: Credit {(btd - avg).AsCurrency()}, Balance {avg.AsCurrency()}"
                        : $"Plan B: Debit {(avg - btd).AsCurrency()}, Balance {avg.AsCurrency()}");
            }
            else
            {
                sb.AppendLine($"Average deficiency: {avx.AsCurrency()} > {avg.AsCurrency()}");
                sb.AppendLine();

                sb.AppendLine(
                    (btd - avx).IsNonNegative()
                        ? $"Plan: Credit {(btd - avx).AsCurrency()}, Balance {avx.AsCurrency()}"
                        : $"Plan: Debit {(avx - btd).AsCurrency()}, Balance {avx.AsCurrency()}");
            }
        }

        return ValueTask.FromResult<IQueryResult>(new PlainText(sb.ToString()));
    }
}
