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
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Carry;

/// <summary>
///     结转表达式解释器
/// </summary>
internal partial class CarryShell : IShellComponent
{
    /// <summary>
    ///     基本会计业务处理类
    /// </summary>
    private readonly Accountant m_Accountant;

    public CarryShell(Accountant helper) => m_Accountant = helper;

    /// <inheritdoc />
    public IQueryResult Execute(string expr, IEntitiesSerializer serializer)
    {
        expr = expr.Rest();
        var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
        Parsing.Eof(expr);

        if (!rng.NullOnly)
        {
            rng.EndDate = rng.EndDate.HasValue
                ? new DateTime(rng.EndDate!.Value.Year, rng.EndDate!.Value.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                : new DateTime(ClientDateTime.Today.Year, ClientDateTime.Today.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddMonths(-1);
            if (!rng.StartDate.HasValue)
            {
                var st = m_Accountant.RunVoucherGroupedQuery("[~null] !!y").Items.Cast<ISubtotalDate>()
                    .OrderBy(grpd => grpd.Date).FirstOrDefault()?.Date;
                if (st.HasValue)
                    rng.StartDate = st;
                else
                    rng = DateFilter.TheNullOnly;
            }
            else
                rng.StartDate
                    = new DateTime(rng.StartDate!.Value.Year, rng.StartDate!.Value.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        var sb = new StringBuilder();

        if (rng.NullOnly || rng.Nullable)
        {
            sb.AppendLine($"=== rm -rf Carry [null] ===> {ResetCarry(DateFilter.TheNullOnly)} removed");
            sb.AppendLine($"=== rm -rf AnnualCarry [null] ===> {ResetCarryYear(DateFilter.TheNullOnly)} removed");
            Carry(sb, null);
            CarryYear(sb, null);
        }

        if (!rng.NullOnly)
            for (var dt = rng.StartDate!.Value; dt <= rng.EndDate!.Value; dt = dt.AddMonths(1))
            {
                sb.AppendLine(
                    $"=== rm -rf Carry {dt.AsDate(SubtotalLevel.Month)} ===> {ResetCarry(new(dt, dt.AddMonths(1).AddDays(-1)))} removed");
                Carry(sb, dt);
                if (dt.Month != 12)
                    continue;
                sb.AppendLine(
                    $"=== rm -rf AnnualCarry {dt.AsDate(SubtotalLevel.Year)} ===> {ResetCarryYear(new(dt, dt.AddYears(1).AddDays(-1)))} removed");
                CarryYear(sb, dt);
            }

        return new DirtyText(sb.ToString());
    }

    /// <inheritdoc />
    public bool IsExecutable(string expr) => expr.Initial() == "ca";
}
