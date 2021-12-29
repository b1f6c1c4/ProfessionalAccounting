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
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Carry;

/// <summary>
///     结转/所有者权益币种转换表达式解释器
/// </summary>
internal partial class CarryShell : IShellComponent
{
    /// <inheritdoc />
    public IAsyncEnumerable<string> Execute(string expr, Session session)
    {
        expr = expr.Rest();
        DateFilter rng;
        switch (expr.Initial())
        {
            case "lst":
                expr = expr.Rest();
                rng = Parsing.Range(ref expr, session.Client) ?? DateFilter.Unconstrained;
                Parsing.Eof(expr);
                return ListHistory(rng);
            case "rst":
                expr = expr.Rest();
                rng = Parsing.Range(ref expr, session.Client) ?? DateFilter.Unconstrained;
                Parsing.Eof(expr);
                return PerformAction(session, rng, true);
            default:
                rng = Parsing.Range(ref expr, session.Client) ?? DateFilter.Unconstrained;
                Parsing.Eof(expr);
                return PerformAction(session, AutomaticRange(session, rng), false);
        }
    }

    private DateFilter AutomaticRange(Session session, DateFilter rng)
    {
        rng ??= DateFilter.Unconstrained;
        if (rng.NullOnly)
            return rng;

        var today = session.Client.Today;
        rng.EndDate = rng.EndDate.HasValue
            ? new DateTime(rng.EndDate!.Value.Year, rng.EndDate!.Value.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            : new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMonths(-1);
        rng.EndDate = rng.EndDate!.Value.AddMonths(1).AddDays(-1);
        if (!rng.StartDate.HasValue)
        {
            var st = session.Accountant.RunVoucherGroupedQuery("[~null] !!y").Items.Cast<ISubtotalDate>()
                .OrderBy(grpd => grpd.Date).FirstOrDefault()?.Date;
            if (st.HasValue)
                rng.StartDate = st;
            else
                rng = DateFilter.TheNullOnly;
        }
        else
            rng.StartDate
                = new DateTime(rng.StartDate!.Value.Year, rng.StartDate!.Value.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return rng;
    }

    private async IAsyncEnumerable<string> PerformAction(Session session, DateFilter rng, bool isRst)
    {
        yield return $"=== rm -rf Carry {rng.AsDateRange()} ===> {await ResetCarry(session, rng)} removed";
        yield return $"=== rm -rf CarryYear {rng.AsDateRange()} ===> {await ResetCarryYear(session, rng)} removed";
        yield return $"=== rm -rf Conversion {rng.AsDateRange()} ===> {await ResetConversion(session, rng)} removed";
        if (isRst)
            yield break;

        using var vir = session.Accountant.Virtualize();

        if (rng.NullOnly || rng.Nullable)
        {
            await foreach (var s in Carry(session, null))
                yield return s;

            await foreach (var s in CarryYear(session, null))
                yield return s;
        }

        if (!rng.NullOnly)
            for (var dt = rng.StartDate!.Value; dt <= rng.EndDate!.Value; dt = dt.AddMonths(1))
            {
                foreach (var info in BaseCurrency.History)
                    if (info.Date >= dt && info.Date < dt.AddMonths(1))
                        await foreach (var s in ConvertEquity(session, info.Date!.Value, info.Currency))
                            yield return s;

                await foreach (var s in Carry(session, dt))
                    yield return s;

                if (dt.Month == 12)
                    await foreach (var s in CarryYear(session, dt))
                        yield return s;
            }

        yield return $"=== Total vouchers: {vir.CachedVouchers}";
    }

    /// <inheritdoc />
    public bool IsExecutable(string expr) => expr.Initial() == "ca";
}
