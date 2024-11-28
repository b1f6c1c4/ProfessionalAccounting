/* Copyright (C) 2024 b1f6c1c4
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
using AccountingServer.Entities;
using AccountingServer.BLL.Util;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell.Plugins.Pivot;

internal class Pivot : PluginBase
{
    /// <inheritdoc />
    public override IAsyncEnumerable<string> Execute(string expr, Session session)
    {
        IVoucherDetailQuery dquery = null;
        IQueryCompounded<IVoucherQueryAtom> vquery = null;
        ISubtotal row = null, col = null;
        try
        {
            dquery = ParsingF.DetailQuery(ref expr, session.Client);
            row = ParsingF.Subtotal(ref expr, session.Client);
            col = ParsingF.Subtotal(ref expr, session.Client);
            ParsingF.Eof(expr);
            if (row.GatherType == GatheringType.VoucherCount
                    || col.GatherType == GatheringType.VoucherCount)
                dquery = null;
        }
        catch (Exception)
        {
            dquery = null;
        }
        if (dquery != null)
        {
            var sub = new CombinedSubtotal(row, col);
            return PostProcess(session.Accountant.SelectVoucherDetailsGroupedDirectAsync(
                        new GroupedQueryStub{ VoucherEmitQuery = dquery, Subtotal = sub }), sub, session);
        }

        try
        {
            vquery = ParsingF.VoucherQuery(ref expr, session.Client);
            row = ParsingF.Subtotal(ref expr, session.Client);
            col = ParsingF.Subtotal(ref expr, session.Client);
            ParsingF.Eof(expr);
            if (row.GatherType != GatheringType.VoucherCount
                || col.GatherType != GatheringType.VoucherCount)
                vquery = null;
        }
        catch (Exception)
        {
            vquery = null;
        }
        if (vquery != null)
        {
            var sub = new CombinedSubtotal(row, col);
            return PostProcess(session.Accountant.SelectVouchersGroupedDirectAsync(
                        new VoucherGroupedQueryStub{ VoucherQuery = vquery, Subtotal = sub }), sub, session);
        }

        throw new FormatException();
    }

    public class CombinedSubtotal : ISubtotal
    {
        public CombinedSubtotal(ISubtotal r, ISubtotal c)
        {
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

            if (r.AggrType != AggregationType.None)
            {
                Row = c;
                Col = r;
                AggrInterval = r.AggrInterval;
                if (r.AggrType == AggregationType.EveryDay)
                    EveryDayRange = r.EveryDayRange;
                Flipped = true;
            }
            else if (c.AggrType != AggregationType.None)
            {
                Row = r;
                Col = c;
                AggrInterval = c.AggrInterval;
                if (c.AggrType == AggregationType.EveryDay)
                    EveryDayRange = c.EveryDayRange;
                Flipped = false;
            }
            else
            {
                Row = r;
                Col = c;
                Flipped = false;
            }

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

        public ISubtotal Row { get; }
        public ISubtotal Col { get; }
        public bool Flipped { get; }

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

    private sealed class GroupedQueryStub : IGroupedQuery
    {
        public IVoucherDetailQuery VoucherEmitQuery { get; init; }

        public ISubtotal Subtotal { get; init; }
    }

    private sealed class VoucherGroupedQueryStub : IVoucherGroupedQuery
    {
        public IQueryCompounded<IVoucherQueryAtom> VoucherQuery { get; init; }

        public ISubtotal Subtotal { get; init; }
    }

    private async IAsyncEnumerable<string> PostProcess(IAsyncEnumerable<Balance> raw, CombinedSubtotal sub, Session session)
    {
        var data = (await raw.ToListAsync()).ToAsyncEnumerable();
        var cconv = new SubtotalBuilder(sub.Col, session.Accountant);
        var rconv = new SubtotalBuilder(sub.Row, session.Accountant);
        var mgr = new ColumnManager(await cconv.Build(data), sub.Col);
        var head = new List<Property>();
        var curr = (double? v, string c) => {
            if (sub.EquivalentCurrency != null)
                return v.AsFund(sub.EquivalentCurrency);
            if (sub.GatherType != GatheringType.Sum)
                return v?.ToString("N0");
            return v.AsFund(c);
        };
        foreach (var p in (await rconv.Build(data)).Accept(new Stringifier(sub.Row)))
        {
            head.Add(p);
            mgr.Add(await cconv.Build(p.Sub.Balances));
        }
        if (!sub.Flipped)
        {
            yield return $"\t{string.Join("\t", mgr.Header)}\n";
            for (var i = 0; i < mgr.Height; i++)
            {
                var txt = mgr.Header.Zip(mgr.Row(i), (p, v) => curr(v, p.Currency ?? head[i].Currency));
                yield return $"{head[i]}\t{string.Join("\t", txt)}\n";
            }
        }
        else
        {
            yield return $"\t{string.Join("\t", head)}\n";
            for (var i = 0; i < mgr.Width; i++)
            {
                var txt = head.Zip(mgr.Col(i), (p, v) => curr(v, p.Currency ?? mgr.Header[i].Currency));
                yield return $"{mgr.Header[i]}\t{string.Join("\t", txt)}\n";
            }
        }
    }
}
