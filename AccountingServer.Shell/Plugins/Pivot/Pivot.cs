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
using static AccountingServer.BLL.Parsing.Facade;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell.Plugins.Pivot;

internal class Pivot : PluginBase
{
    /// <inheritdoc />
    public override IAsyncEnumerable<string> Execute(string expr, Session session)
    {
        List<string> prefixes = null;
        List<IGroupedQuery> dqueries;
        List<IVoucherGroupedQuery> vqueries;
        ISubtotal col = null;
        var transpose = false;
        var fitting = false;
        try
        {
            prefixes = new();
            dqueries = new();
            while (true)
            {
                prefixes.Add(Parsing.Regex(ref expr));
                dqueries.Add(ParsingF.GroupedQuery(ref expr, session.Client));
                try
                {
                    col = ParsingF.Subtotal(ref expr, session.Client);
                    transpose = ParsingF.Optional(ref expr, "t");
                    fitting = ParsingF.Optional(ref expr, "f");
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
            return PostProcess(CombinedSubtotal.Query(dqueries, col, session),
                    prefixes, col, transpose, fitting, session);

        try
        {
            prefixes = new();
            vqueries = new();
            while (true)
            {
                prefixes.Add(Parsing.Regex(ref expr));
                vqueries.Add(ParsingF.VoucherGroupedQuery(ref expr, session.Client));
                try
                {
                    col = ParsingF.Subtotal(ref expr, session.Client);
                    transpose = ParsingF.Optional(ref expr, "t");
                    fitting = ParsingF.Optional(ref expr, "f");
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
            return PostProcess(CombinedSubtotal.Query(vqueries, col, session),
                    prefixes, col, transpose, fitting, session);

        throw new FormatException();
    }

    private async IAsyncEnumerable<string> PostProcess(
            ValueTask<(List<List<Balance>>, List<CombinedSubtotal>, bool)> task,
            List<string> prefixes, ISubtotal col, bool transpose, bool fitting, Session session)
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
        List<string> headers = null;
        List<List<string>> texts = null;
        if (!transpose && !fitting)
            yield return $"\t{string.Join("\t", mgr.Header)}\n";
        else
        {
            headers = new();
            texts = new();
        }
        if (!flipped)
        {
            foreach (var ((dat, sub), prefix) in data.Zip(subs).Zip(prefixes))
            {
                var rconv = new SubtotalBuilder(sub.LocalRow, session.Accountant);
                var head = new List<Property>();
                mgr.Clear();
                var sgf = new Stringifier(sub.LocalRow, prefix);
                foreach (var p in (await rconv.Build(dat.ToAsyncEnumerable())).Accept(sgf))
                {
                    head.Add(p);
                    mgr.Add(await cconv.Build(p.Sub.Balances));
                }
                for (var i = 0; i < mgr.Height; i++)
                {
                    var txt = mgr.Header.Zip(mgr.Row(i), (p, v) => curr(v, p.Currency ?? head[i].Currency));
                    if (!transpose && !fitting)
                        yield return $"{head[i]}\t{string.Join("\t", txt)}\n";
                    else
                    {
                        headers.Add(head[i].ToString());
                        texts.Add(txt.ToList());
                    }
                }
            }
        }
        else
        {
            foreach (var ((dat, sub), prefix) in data.Zip(subs).Zip(prefixes))
            {
                var lcconv = new SubtotalBuilder(sub.LocalCol, session.Accountant);
                var lmgr = new ColumnManager(await lcconv.Build(dat.ToAsyncEnumerable()), sub.LocalCol);
                var shuffler = new int[mgr.Width];
                var id = 1;
                var sgf = new Stringifier(col, prefix);
                foreach (var p in (await cconv.Build(dat.ToAsyncEnumerable())).Accept(sgf))
                {
                    shuffler[mgr.IndexOf(p)] = id++;
                    lmgr.Add(await lcconv.Build(p.Sub.Balances));
                }
                for (var i = 0; i < lmgr.Width; i++)
                {
                    var values = shuffler.Select(id => id > 0 ? lmgr.Row(id - 1)[i] : null);
                    var txt = mgr.Header.Zip(values, (p, v) => curr(v, p.Currency ?? lmgr.Header[i].Currency));
                    if (!transpose && !fitting)
                        yield return $"{lmgr.Header[i]}\t{string.Join("\t", txt)}\n";
                    else
                    {
                        headers.Add(lmgr.Header[i].ToString());
                        texts.Add(txt.ToList());
                    }
                }
            }
        }
        Func<string, int, string> fit = static (s, w) => s.CPadLeft(w);
        Func<string, int, string> fitL = static (s, w) => s.CPadRight(w);
        if (transpose && !fitting)
        {
            yield return $"\t{string.Join("\t", headers)}\n";
            for (var i = 0; i < mgr.Width; i++)
                yield return $"{mgr.Header[i]}\t{string.Join("\t", texts.Select(txt => txt[i]))}\n";
        }
        else if (transpose && fitting)
        {
            var wh = mgr.Header.Max(static s => s.ToString().CLength());
            var wt = headers.Zip(texts, static (h, txt)
                    => Math.Max(h.CLength(), txt.Max(StringFormatter.CLength)));
            yield return $"{fit("", wh)} {string.Join(" ", headers.Zip(wt, fitL))}\n";
            for (var i = 0; i < mgr.Width; i++)
                yield return $"{fitL(mgr.Header[i].ToString(), wh)} {string.Join("\t",
                      texts.Zip(wt, (txt, w) => fit(txt[i], w)))}\n";
        }
        else // if (!transpose && fitting)
        {
            var wh = headers.Max(StringFormatter.CLength);
            var wt = mgr.Header.Select((p, i) =>
                    Math.Max(p.ToString().CLength(), texts.Max(txt => txt[i].CLength())));
            yield return $"{fit("", wh)} {string.Join(" ", mgr.Header.Zip(wt, (h, w) => fitL(h.ToString(), w)))}\n";
            foreach (var (h, txt) in headers.Zip(texts))
                yield return $"{fitL(h, wh)} {string.Join(" ", txt.Zip(wt, fit))}\n";
        }
    }
}
