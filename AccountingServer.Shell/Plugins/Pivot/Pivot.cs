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
using System.Threading.Tasks;
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
        List<IGroupedQuery> dqueries = new();
        List<IVoucherGroupedQuery> vqueries = new();
        ISubtotal col = null;
        try
        {
            while (true)
            {
                var dquery = ParsingF.GroupedQuery(ref expr, session.Client);
                dqueries.Add(dquery);
                try
                {
                    col = ParsingF.Subtotal(ref expr, session.Client);
                    ParsingF.Eof(expr);
                    break;
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            if (col.GatherType == GatheringType.VoucherCount)
                throw new FormatException();
        }
        catch (Exception)
        {
            dqueries = null;
        }
        if (dqueries != null)
            return PostProcess(CombinedSubtotal.Query(dqueries, col, session), col, session);

        try
        {
            while (true)
            {
                var vquery = ParsingF.VoucherGroupedQuery(ref expr, session.Client);
                vqueries.Add(vquery);
                try
                {
                    col = ParsingF.Subtotal(ref expr, session.Client);
                    ParsingF.Eof(expr);
                    break;
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            if (col.GatherType != GatheringType.VoucherCount)
                throw new FormatException();
        }
        catch (Exception)
        {
            vqueries = null;
        }
        if (vqueries != null)
            return PostProcess(CombinedSubtotal.Query(vqueries, col, session), col, session);

next2:
        throw new FormatException();
    }

    private async IAsyncEnumerable<string> PostProcess(
            ValueTask<(List<List<Balance>>, List<CombinedSubtotal>, bool)> task, ISubtotal col, Session session)
    {
        var (data, subs, flipped) = await task;
        var cconv = new SubtotalBuilder(col, session.Accountant);
        var curr = (double? v, string c) => {
            if (col.EquivalentCurrency != null)
                return v.AsFund(col.EquivalentCurrency);
            if (col.GatherType != GatheringType.Sum)
                return v?.ToString("N0");
            return v.AsFund(c);
        };
        var mgr = new ColumnManager(await cconv.Build(data.SelectMany(static d => d).ToAsyncEnumerable()), col);
        yield return $"\t{string.Join("\t", mgr.Header)}\n";
        if (!flipped)
        {
            foreach (var (dat, sub) in data.Zip(subs))
            {
                var rconv = new SubtotalBuilder(sub.LocalRow, session.Accountant);
                var head = new List<Property>();
                mgr.Clear();
                foreach (var p in (await rconv.Build(dat.ToAsyncEnumerable())).Accept(new Stringifier(sub.LocalRow)))
                {
                    head.Add(p);
                    mgr.Add(await cconv.Build(p.Sub.Balances));
                }
                for (var i = 0; i < mgr.Height; i++)
                {
                    var txt = mgr.Header.Zip(mgr.Row(i), (p, v) => curr(v, p.Currency ?? head[i].Currency));
                    yield return $"{head[i]}\t{string.Join("\t", txt)}\n";
                }
            }
        }
        else
        {
            foreach (var (dat, sub) in data.Zip(subs))
            {
                var lcconv = new SubtotalBuilder(sub.LocalCol, session.Accountant);
                var lmgr = new ColumnManager(await lcconv.Build(dat.ToAsyncEnumerable()), sub.LocalCol);
                var shuffler = new int[mgr.Width];
                var id = 1;
                foreach (var p in (await cconv.Build(dat.ToAsyncEnumerable())).Accept(new Stringifier(col)))
                {
                    shuffler[mgr.IndexOf(p)] = id++;
                    lmgr.Add(await lcconv.Build(p.Sub.Balances));
                }
                for (var i = 0; i < lmgr.Width; i++)
                {
                    var values = shuffler.Select(id => id > 0 ? lmgr.Row(id - 1)[i] : null);
                    var txt = mgr.Header.Zip(values, (p, v) => curr(v, p.Currency ?? lmgr.Header[i].Currency));
                    yield return $"{lmgr.Header[i]}\t{string.Join("\t", txt)}\n";
                }
            }
        }
    }
}
