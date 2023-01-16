/* Copyright (C) 2020-2023 b1f6c1c4
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
using System.Text;
using System.Threading.Tasks;
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

        if (ParsingF.Optional(ref expr, "mark"))
        {
            var parsed = new CsvParser(ParsingF.Optional(ref expr, "-"));
            var filt = ParsingF.DetailQuery(ref expr, session.Client);
            ParsingF.Optional(ref expr, "as");
            var marker = ParsingF.Token(ref expr);
            ParsingF.Eof(expr);
            if (string.IsNullOrWhiteSpace(marker))
                throw new FormatException("格式错误");

            parsed.Parse(ref csv);
            var sb = new StringBuilder();
            await RunMark(session, sb, filt, parsed, marker);
            yield return sb.ToString();

            yield break;
        }

        if (ParsingF.Optional(ref expr, "unmark"))
        {
            var filt = ParsingF.DetailQuery(ref expr, session.Client);
            ParsingF.Eof(expr);
            var sb = new StringBuilder();
            await RunUnmark(session, sb, filt);
            yield return sb.ToString();

            yield break;
        }

        if (ParsingF.Optional(ref expr, "check"))
        {
            var filt = ParsingF.DetailQuery(ref expr, session.Client);
            var tolerance = 1;
            if (ParsingF.Optional(ref expr, "tol"))
                tolerance = (int)ParsingF.DoubleF(ref expr);
            ParsingF.Eof(expr);
            var sb = new StringBuilder();
            await RunCheck(session, sb, filt, tolerance);
            yield return sb.ToString();
        }

        {
            var parsed = new CsvParser(ParsingF.Optional(ref expr, "-"));
            var filt = ParsingF.DetailQuery(ref expr, session.Client);
            if (!ParsingF.Optional(ref expr, "as"))
                throw new FormatException("格式错误");
            var marker = ParsingF.Token(ref expr);
            var tolerance = 1;
            if (ParsingF.Optional(ref expr, "tol"))
                tolerance = (int)ParsingF.DoubleF(ref expr);
            ParsingF.Eof(expr);
            if (string.IsNullOrWhiteSpace(marker))
                throw new FormatException("格式错误");

            parsed.Parse(ref csv);
            var markerFilt = new StmtVoucherDetailQuery(
                filt.VoucherQuery,
                new IntersectQueries<IDetailQueryAtom>(
                    filt.ActualDetailFilter(),
                    new SimpleDetailQuery { Filter = new() { Remark = marker } }));
            var nullFilt = new StmtVoucherDetailQuery(
                filt.VoucherQuery,
                new IntersectQueries<IDetailQueryAtom>(
                    filt.ActualDetailFilter(),
                    new SimpleDetailQuery { Filter = new() { Remark = "" } }));
            var sb = new StringBuilder();
            sb.AppendLine($"{parsed.Items.Count} parsed");
            await using var vir = session.Accountant.Virtualize();
            await RunUnmark(session, sb, markerFilt);
            if (await RunMark(session, sb, nullFilt, parsed, marker)
                || await RunCheck(session, sb, filt, tolerance))
            {
                sb.AppendLine("ABORT, rewind by unmark them back");
                await RunUnmark(session, sb, markerFilt);

                yield return "\x0c Transaction Aborted \x0c\n";
                yield return sb.ToString();
                yield return $"Virtualizer: {vir.CachedVouchers}\n";
                yield return "\x0c Attached a copy of original CSV, ready for resubmit. \x0c\n";
                yield return csv;
            }
            else
            {
                yield return sb.ToString();
                yield return $"Virtualizer: {vir.CachedVouchers}\n";
            }
        }
    }

    private static async ValueTask<bool> RunMark(Session session, StringBuilder sb,
        IVoucherDetailQuery filt, CsvParser parsed, string marker)
    {
        if (filt.IsDangerous())
            throw new SecurityException("检测到弱检索式");

        var hasViolation = false;
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
                    : res.Where(v => v.Date.HasValue && Math.Abs((v.Date.Value - b.Date).TotalDays) <= 60)
                        .OrderBy(v => Math.Abs((v.Date.Value - b.Date).TotalDays));
                var voucher = resx
                    .FirstOrDefault(v => v.Details.Any(d
                        => (b.Currency == null || d.Currency == b.Currency) &&
                        (d.Fund!.Value - b.Fund).IsZero() && d.IsMatch(filt.ActualDetailFilter())));
                if (voucher == null)
                    return false;

                var o = voucher.Details.First(d
                    => (b.Currency == null || d.Currency == b.Currency) &&
                    (d.Fund!.Value - b.Fund).IsZero() && d.IsMatch(filt.ActualDetailFilter()));
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

            hasViolation = true;
            sb.Append("[No voucher found]: ");
            sb.AppendLine(b.Raw);
        }

        await session.Accountant.UpsertAsync(ops);

        sb.AppendLine($"{marked} marked");
        sb.AppendLine($"{remarked} remarked");
        sb.AppendLine($"{converted} converted");

        return hasViolation;
    }

    private static async ValueTask RunUnmark(Session session, StringBuilder sb, IVoucherDetailQuery filt)
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
        sb.AppendLine($"{cntAll} selected");
        sb.AppendLine($"{cnt} unmarked");
    }

    private static async ValueTask<bool> RunCheck(Session session, StringBuilder sb,
        IVoucherDetailQuery filt, int tolerance)
    {
        if (filt.IsDangerous())
            throw new SecurityException("检测到弱检索式");

        var res = await session.Accountant.SelectVouchersAsync(filt.VoucherQuery)
            .SelectMany(v => v.Details.Where(d => d.IsMatch(filt.ActualDetailFilter()))
                .Select(d => (v.ID, v.Date, Fund: d.Fund.AsCurrency(d.Currency), d.Remark))
                .ToAsyncEnumerable())
            .OrderBy(static tuple => tuple.Date).ThenBy(static tuple => tuple.Remark)
            .ToListAsync();

        var isFirst = true;
        int? newId = null, violationId = null;
        DateTime? newDate = null;
        string oldR = null, newR = null;
        var noMatch = 0;
        for (var i = 0; i < res.Count; i++)
        {
            switch (res[i].Remark)
            {
                case "reconciliation":
                    continue;
                case null:
                    noMatch++;
                    break;
            }

            var tuple = res[i];
            if (isFirst)
            {
                isFirst = false;
                oldR = tuple.Remark;
                continue;
            }

            if (!newId.HasValue)
            {
                if (tuple.Remark == newR)
                    // do nothing
                    continue;

                // first change
                newId = i;
                newDate = tuple.Date;
                newR = tuple.Remark;

                continue;
            }

            if (tuple.Remark == newR)
                // do nothing
                continue;

            if (tuple.Remark == oldR)
            {
                var diff = (tuple.Date - newDate)?.TotalDays;
                if (diff > tolerance && violationId != newId)
                {
                    violationId = newId;
                    sb.AppendLine($"VIOLATION: Found between period {oldR.Quotation('"')} and {newR.Quotation('"')} ");
                    sb.AppendLine($"           Date tolerance = {tolerance}, where date difference = {diff}");
                    for (var vid = Math.Max(0, newId.Value - 4); vid < Math.Min(res.Count, i + 4); vid++)
                    {
                        sb.Append($"{res[vid].ID.Quotation('^')} {res[vid].Date.AsDate()} ");
                        sb.Append($"{res[vid].Remark.Quotation('"').CPadRight(8)} {res[vid].Fund.CPadLeft(13)}");
                        if (vid == i || vid == newId)
                            sb.Append(" (*)");
                        sb.AppendLine();
                    }
                }

                continue;
            }

            newId = i;
            newDate = tuple.Date;
            oldR = newR;
            newR = tuple.Remark;
        }

        if (noMatch != 0)
            sb.AppendLine($"WARNING: {noMatch} vouchers do NOT have a matching entry.");

        return violationId.HasValue;
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
}
