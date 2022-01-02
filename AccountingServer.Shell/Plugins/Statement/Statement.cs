/* Copyright (C) 2020-2022 b1f6c1c4
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
using System.Security;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell.Plugins.Statement;

/// <summary>
///     自动对账
/// </summary>
internal class Statement : PluginBase
{
    /// <inheritdoc />
    public override async IAsyncEnumerable<string> Execute(string expr, Session session)
    {
        var csv = expr;
        expr = ParsingF.Line(ref csv);
        var parsed = new CsvParser(ParsingF.Optional(ref expr, "-"));

        if (ParsingF.Optional(ref expr, "mark"))
        {
            var filt = ParsingF.DetailQuery(ref expr, session.Client);
            ParsingF.Optional(ref expr, "as");
            var marker = ParsingF.Token(ref expr);
            ParsingF.Eof(expr);
            if (string.IsNullOrWhiteSpace(marker))
                throw new FormatException("格式错误");

            parsed.Parse(csv);
            yield return $"{parsed.Items.Count} parsed";
            await foreach (var s in RunMark(session, filt, parsed, marker))
                yield return s + "\n";
        }
        else if (ParsingF.Optional(ref expr, "unmark"))
        {
            var filt = ParsingF.DetailQuery(ref expr, session.Client);
            ParsingF.Eof(expr);
            await foreach (var s in RunUnmark(session, filt))
                yield return s + "\n";
        }
        else if (ParsingF.Optional(ref expr, "check"))
        {
            var filt = ParsingF.DetailQuery(ref expr, session.Client);
            ParsingF.Eof(expr);
            parsed.Parse(csv);
            yield return $"{parsed.Items.Count} parsed";
            await foreach (var s in RunCheck(session, filt, parsed))
                yield return s + "\n";
        }
        else
        {
            ParsingF.Optional(ref expr, "auto");
            var filt = ParsingF.DetailQuery(ref expr, session.Client);
            ParsingF.Optional(ref expr, "as");
            var marker = ParsingF.Token(ref expr);
            ParsingF.Eof(expr);
            if (string.IsNullOrWhiteSpace(marker))
                throw new FormatException("格式错误");

            parsed.Parse(csv);
            yield return $"{parsed.Items.Count} parsed";
            var markerFilt = new StmtVoucherDetailQuery(
                filt.VoucherQuery,
                new IntersectQueries<IDetailQueryAtom>(
                    filt.ActualDetailFilter(),
                    new StmtDetailQuery(marker)));
            var nullFilt = new StmtVoucherDetailQuery(
                filt.VoucherQuery,
                new IntersectQueries<IDetailQueryAtom>(
                    filt.ActualDetailFilter(),
                    new StmtDetailQuery("")));
            var nmFilt = new StmtVoucherDetailQuery(
                filt.VoucherQuery,
                new IntersectQueries<IDetailQueryAtom>(
                    filt.ActualDetailFilter(),
                    new UnionQueries<IDetailQueryAtom>(
                        new StmtDetailQuery(""),
                        new StmtDetailQuery(marker))));
            await foreach (var s in RunUnmark(session, markerFilt))
                yield return s + "\n";
            await foreach (var s in RunMark(session, nullFilt, parsed, marker))
                yield return s + "\n";
            await foreach (var s in RunCheck(session, nmFilt, parsed))
                yield return s + "\n";
        }
    }

    private async IAsyncEnumerable<string> RunMark(Session session, IVoucherDetailQuery filt, CsvParser parsed,
        string marker)
    {
        if (filt.IsDangerous())
            throw new SecurityException("检测到弱检索式");

        var marked = 0;
        var remarked = 0;
        var converted = 0;
        var res = await session.Accountant.SelectVouchersAsync(filt.VoucherQuery).ToListAsync();
        var ops = new List<Voucher>();
        foreach (var b in parsed.Items)
        {
            bool Trial(bool date)
            {
                var resx = date
                    ? res.Where(v => v.Date == b.Date)
                    : res.OrderBy(v => v.Date.HasValue
                        ? Math.Abs((v.Date.Value - b.Date).TotalDays)
                        : double.PositiveInfinity);
                var voucher = resx
                    .FirstOrDefault(v => v.Details.Any(d
                        => (d.Fund!.Value - b.Fund).IsZero() && d.IsMatch(filt.ActualDetailFilter())));
                if (voucher == null)
                    return false;

                var o = voucher.Details.First(d
                    => (d.Fund!.Value - b.Fund).IsZero() && d.IsMatch(filt.ActualDetailFilter()));
                if (o.Remark == null)
                    marked++;
                else if (o.Remark == marker)
                    remarked++;
                else
                    converted++;

                o.Remark = marker;
                ops.Add(voucher);
                return true;
            }

            if (Trial(true) || Trial(false))
                continue;

            yield return b.Raw;
        }

        await session.Accountant.UpsertAsync(ops);

        yield return $"{marked} marked";
        yield return $"{remarked} remarked";
        yield return $"{converted} converted";
    }

    private async IAsyncEnumerable<string> RunUnmark(Session session, IVoucherDetailQuery filt)
    {
        if (filt.IsDangerous())
            throw new SecurityException("检测到弱检索式");

        var cnt = 0;
        var cntAll = 0;
        var res = session.Accountant.SelectVouchersAsync(filt.VoucherQuery);
        var ops = new List<Voucher>();
        await foreach (var v in res)
        {
            foreach (var d in v.Details)
            {
                if (!d.IsMatch(filt.ActualDetailFilter()))
                    continue;

                cntAll++;
                if (d.Remark == null)
                    continue;

                d.Remark = null;
                cnt++;
            }

            ops.Add(v);
        }

        await session.Accountant.UpsertAsync(ops);
        yield return $"{cntAll} selected";
        yield return $"{cnt} unmarked";
    }

    private async IAsyncEnumerable<string> RunCheck(Session session, IVoucherDetailQuery filt, CsvParser parsed)
    {
        if (filt.IsDangerous())
            throw new SecurityException("检测到弱检索式");

        var res = session.Accountant.SelectVouchersAsync(filt.VoucherQuery);
        var lst = new List<BankItem>(parsed.Items);
        await foreach (var v in res)
        foreach (var d in v.Details)
        {
            if (!d.IsMatch(filt.ActualDetailFilter()))
                continue;

            var obj1 = lst.FirstOrDefault(b => (b.Fund - d.Fund!.Value).IsZero() && b.Date == v.Date);
            if (obj1 != null)
            {
                lst.Remove(obj1);
                continue;
            }

            var obj2 = lst
                .Where(b => (b.Fund - d.Fund!.Value).IsZero())
                .OrderBy(b => Math.Abs((b.Date - v.Date!.Value).TotalDays))
                .FirstOrDefault();
            if (obj2 != null)
            {
                lst.Remove(obj2);
                continue;
            }

            yield return $"{v.ID.Quotation('^')} {v.Date.AsDate()} {d.Content} {d.Fund.AsCurrency(d.Currency)}";
        }

        foreach (var b in lst)
            yield return b.Raw;
    }

    private sealed class StmtVoucherDetailQuery : IVoucherDetailQuery
    {
        public StmtVoucherDetailQuery(IQueryCompounded<IVoucherQueryAtom> v, IQueryCompounded<IDetailQueryAtom> d)
        {
            VoucherQuery = v;
            DetailEmitFilter = new StmtEmit { DetailFilter = d };
        }

        public IQueryCompounded<IVoucherQueryAtom> VoucherQuery { get; }

        public IEmit DetailEmitFilter { get; }
    }

    private class StmtEmit : IEmit
    {
        public IQueryCompounded<IDetailQueryAtom> DetailFilter { get; init; }
    }

    private sealed class StmtDetailQuery : IDetailQueryAtom
    {
        public StmtDetailQuery(string marker)
            => Filter = new() { Remark = marker };

        public TitleKind? Kind => null;

        public VoucherDetail Filter { get; }

        public int Dir => 0;

        public string ContentPrefix => null;

        public string RemarkPrefix => null;

        public bool IsDangerous() => Filter.IsDangerous();

        public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
    }
}
