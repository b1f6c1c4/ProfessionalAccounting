/* Copyright (C) 2024-2025 b1f6c1c4
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
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell;

internal class CombinedSubtotal : ISubtotal
{
    public static async ValueTask<(List<List<Balance>>, List<CombinedSubtotal>, bool)> Query(
            IReadOnlyList<IGroupedQuery> dqueries,
            ISubtotal col, Session session)
    {
        var ress = new List<List<Balance>>();
        var subs = new List<CombinedSubtotal>();
        var flipped = dqueries.Any(static (gq) => gq.Subtotal.AggrType != AggregationType.None);
        foreach (var gq in dqueries)
        {
            var sub = flipped
                ? new CombinedSubtotal(col, gq.Subtotal)
                : new CombinedSubtotal(gq.Subtotal, col);
            subs.Add(sub);
            ress.Add(await session.Accountant.SelectVoucherDetailsGroupedDirectAsync(
                    new GroupedQuery(gq.VoucherEmitQuery, sub)).ToListAsync());
        }

        return (ress, subs, flipped);
    }

    public static async ValueTask<(List<List<Balance>>, List<CombinedSubtotal>, bool)> Query(
            IReadOnlyList<IVoucherGroupedQuery> vqueries,
            ISubtotal col, Session session)
    {
        var ress = new List<List<Balance>>();
        var subs = new List<CombinedSubtotal>();
        var flipped = vqueries.Any(static (gq) => gq.Subtotal.AggrType != AggregationType.None);
        foreach (var vgq in vqueries)
        {
            var sub = flipped
                ? new CombinedSubtotal(col, vgq.Subtotal)
                : new CombinedSubtotal(vgq.Subtotal, col);
            subs.Add(sub);
            ress.Add(await session.Accountant.SelectVouchersGroupedDirectAsync(
                    new VoucherGroupedQuery(vgq.VoucherQuery, sub)).ToListAsync());
        }

        return (ress, subs, flipped);
    }

    public ISubtotal LocalRow { get; }
    public ISubtotal LocalCol { get; }

    public CombinedSubtotal(ISubtotal r, ISubtotal c)
    {
        LocalRow = r;
        LocalCol = c;

        if (r == null)
            throw new ApplicationException("r is null");
        if (c == null)
            throw new ApplicationException("c is null");

        if (r.GatherType != c.GatherType)
            throw new ApplicationException($"Mismatching GatherType: {r.GatherType} vs {c.GatherType}");

        GatherType = r.GatherType;

        if (r.EquivalentCurrency != c.EquivalentCurrency)
            throw new ApplicationException($"Mismatching equi: {r.EquivalentCurrency} vs {c.EquivalentCurrency}");
        if (r.EquivalentDate != c.EquivalentDate)
            throw new ApplicationException($"Mismatching equi: {r.EquivalentDate} vs {c.EquivalentDate}");

        EquivalentCurrency = r.EquivalentCurrency;
        EquivalentDate = r.EquivalentDate;

        if (r.AggrType != AggregationType.None
            && c.AggrType != AggregationType.None)
            throw new ApplicationException($"Mismatching AggrType: {r.AggrType} vs {c.AggrType}");

        AggrInterval = c.AggrInterval;
        if (c.AggrType == AggregationType.EveryDay)
            EveryDayRange = c.EveryDayRange;

        var lvl = AggrInterval;
        foreach (var l in r.Levels)
            lvl |= l;
        lvl &= ~SubtotalLevel.NonZero;
        foreach (var l in c.Levels)
            if ((lvl & l) != SubtotalLevel.None)
                throw new ApplicationException($"Duplicating Levels: 0b{(int)lvl:b} vs 0b{(int)l:b}");
        foreach (var l in c.Levels)
            lvl |= l;
        lvl &= ~SubtotalLevel.NonZero;
        Levels = new List<SubtotalLevel>() { lvl };
    }

    /// <inheritdoc />
    public GatheringType GatherType { get; }

    /// <inheritdoc />
    public IReadOnlyList<SubtotalLevel> Levels { get; }

    /// <inheritdoc />
    public AggregationType AggrType { get; }

    /// <inheritdoc />
    public SubtotalLevel AggrInterval { get; }

    /// <inheritdoc />
    public DateFilter EveryDayRange { get; }

    /// <inheritdoc />
    public string EquivalentCurrency { get; }

    /// <inheritdoc />
    public DateTime? EquivalentDate { get; }
}
