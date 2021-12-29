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
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Carry;

/// <summary>
///     结转/所有者权益币种转换表达式解释器
/// </summary>
internal partial class CarryShell : IShellComponent
{
    /// <inheritdoc />
    public async ValueTask<IQueryResult> Execute(string expr, Session session)
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
                return await PerformAction(session, rng, true);
            default:
                rng = Parsing.Range(ref expr, session.Client) ?? DateFilter.Unconstrained;
                Parsing.Eof(expr);
                return await PerformAction(session, AutomaticRange(session, rng), false);
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

    private async ValueTask<IQueryResult> PerformAction(Session session, DateFilter rng, bool isRst)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"=== rm -rf Carry {rng.AsDateRange()} ===> {ResetCarry(session, rng)} removed");
        sb.AppendLine($"=== rm -rf CarryYear {rng.AsDateRange()} ===> {ResetCarryYear(session, rng)} removed");
        sb.AppendLine($"=== rm -rf Conversion {rng.AsDateRange()} ===> {ResetConversion(session, rng)} removed");
        if (isRst)
            return new DirtyText(sb.ToString());

        using var vir = session.Accountant.Virtualize();

        if (rng.NullOnly || rng.Nullable)
        {
            await Carry(session, sb, null);
            CarryYear(session, sb, null);
        }

        if (!rng.NullOnly)
            for (var dt = rng.StartDate!.Value; dt <= rng.EndDate!.Value; dt = dt.AddMonths(1))
            {
                foreach (var info in BaseCurrency.History)
                    if (info.Date >= dt && info.Date < dt.AddMonths(1))
                        await ConvertEquity(session, sb, info.Date!.Value, info.Currency);

                await Carry(session, sb, dt);
                if (dt.Month == 12)
                    CarryYear(session, sb, dt);
            }

        sb.AppendLine($"=== Total vouchers: {vir.CachedVouchers}");

        return new DirtyText(sb.ToString());
    }

    /// <inheritdoc />
    public bool IsExecutable(string expr) => expr.Initial() == "ca";
}
