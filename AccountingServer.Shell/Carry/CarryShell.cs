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
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Carry;

/// <summary>
///     结转/所有者权益币种转换表达式解释器
/// </summary>
internal partial class CarryShell : IShellComponent
{
    /// <inheritdoc />
    public async IAsyncEnumerable<string> Execute(string expr, Context ctx, string term)
    {
        expr = expr.Rest();
        DateFilter rng;
        IAsyncEnumerable<string> iae;
        switch (expr.Initial())
        {
            case "lst":
                expr = expr.Rest();
                rng = Parsing.Range(ref expr, ctx.Client) ?? DateFilter.Unconstrained;
                Parsing.Eof(expr);
                iae = ListHistory(rng).ToAsyncEnumerable();
                break;
            case "rst":
                expr = expr.Rest();
                rng = Parsing.Range(ref expr, ctx.Client) ?? DateFilter.Unconstrained;
                Parsing.Eof(expr);
                iae = PerformAction(ctx, rng, true);
                break;
            default:
                rng = Parsing.Range(ref expr, ctx.Client) ?? DateFilter.Unconstrained;
                Parsing.Eof(expr);
                iae = PerformAction(ctx, await AutomaticRange(ctx, rng), false);
                break;
        }

        await foreach (var e in iae)
            yield return e;
    }

    /// <inheritdoc />
    public bool IsExecutable(string expr) => expr.Initial() == "ca";

    private async ValueTask<DateFilter> AutomaticRange(Context ctx, DateFilter rng)
    {
        rng ??= DateFilter.Unconstrained;
        if (rng.NullOnly)
            return rng;

        var today = ctx.Client.Today;
        rng.EndDate = rng.EndDate.HasValue
            ? new(rng.EndDate!.Value.Year, rng.EndDate!.Value.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            : new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMonths(-1);
        rng.EndDate = rng.EndDate!.Value.AddMonths(1).AddDays(-1);
        if (!rng.StartDate.HasValue)
        {
            var st = (await ctx.Accountant.RunVoucherGroupedQueryAsync("[~null] !!y")).Items.Cast<ISubtotalDate>()
                .OrderBy(static grpd => grpd.Date).FirstOrDefault()?.Date;
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

    private async IAsyncEnumerable<string> PerformAction(Context ctx, DateFilter rng, bool isRst)
    {
        yield return $"=== rm -rf Carry {rng.AsDateRange()} ===> {await ResetCarry(ctx, rng)} removed\n";
        yield return $"=== rm -rf CarryYear {rng.AsDateRange()} ===> {await ResetCarryYear(ctx, rng)} removed\n";
        yield return $"=== rm -rf Conversion {rng.AsDateRange()} ===> {await ResetConversion(ctx, rng)} removed\n";
        if (isRst)
            yield break;

        await using var vir = ctx.Accountant.Virtualize();

        if (rng.NullOnly || rng.Nullable)
        {
            await foreach (var s in Carry(ctx, null))
                yield return s;

            await foreach (var s in CarryYear(ctx, null))
                yield return s;
        }

        if (!rng.NullOnly)
            for (var dt = rng.StartDate!.Value; dt <= rng.EndDate!.Value; dt = dt.AddMonths(1))
            {
                foreach (var info in BaseCurrency.History.Where(info => info.Date >= dt && info.Date < dt.AddMonths(1)))
                await foreach (var s in ConvertEquity(ctx, info.Date!.Value, info.Currency))
                    yield return s;

                await foreach (var s in Carry(ctx, dt))
                    yield return s;

                if (dt.Month == 12)
                    await foreach (var s in CarryYear(ctx, dt))
                        yield return s;
            }

        yield return $"=== Total vouchers: {vir}\n";
    }
}
