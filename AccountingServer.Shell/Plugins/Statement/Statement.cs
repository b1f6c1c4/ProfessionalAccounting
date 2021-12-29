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
using System.Security;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell.Plugins.Statement;

/// <summary>
///     自动对账
/// </summary>
internal class Statement : PluginBase
{
    /// <inheritdoc />
    public override IQueryResult Execute(string expr, Session session)
    {
        var csv = expr;
        expr = ParsingF.Line(ref csv);
        var parsed = new CsvParser(ParsingF.Optional(ref expr, "-"));

        var sb = new StringBuilder();
        if (ParsingF.Optional(ref expr, "mark"))
        {
            var filt = ParsingF.DetailQuery(ref expr, session.Client);
            ParsingF.Optional(ref expr, "as");
            var marker = ParsingF.Token(ref expr);
            ParsingF.Eof(expr);
            if (string.IsNullOrWhiteSpace(marker))
                throw new FormatException("格式错误");

            parsed.Parse(csv);
            sb.AppendLine($"{parsed.Items.Count} parsed");
            RunMark(session, filt, parsed, marker, sb);
        }
        else if (ParsingF.Optional(ref expr, "unmark"))
        {
            var filt = ParsingF.DetailQuery(ref expr, session.Client);
            ParsingF.Eof(expr);
            RunUnmark(session, filt, sb);
        }
        else if (ParsingF.Optional(ref expr, "check"))
        {
            var filt = ParsingF.DetailQuery(ref expr, session.Client);
            ParsingF.Eof(expr);
            parsed.Parse(csv);
            sb.AppendLine($"{parsed.Items.Count} parsed");
            RunCheck(session, filt, parsed, sb);
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
            sb.AppendLine($"{parsed.Items.Count} parsed");
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
            RunUnmark(session, markerFilt, sb);
            RunMark(session, nullFilt, parsed, marker, sb);
            RunCheck(session, nmFilt, parsed, sb);
        }

        return new PlainText(sb.ToString());
    }

    private void RunMark(Session session, IVoucherDetailQuery filt, CsvParser parsed, string marker, StringBuilder sb)
    {
        if (filt.IsDangerous())
            throw new SecurityException("检测到弱检索式");

        var marked = 0;
        var remarked = 0;
        var converted = 0;
        var res = session.Accountant.SelectVouchers(filt.VoucherQuery).ToList();
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

            sb.AppendLine(b.Raw);
        }

        session.Accountant.Upsert(ops);

        sb.AppendLine($"{marked} marked");
        sb.AppendLine($"{remarked} remarked");
        sb.AppendLine($"{converted} converted");
    }

    private void RunUnmark(Session session, IVoucherDetailQuery filt, StringBuilder sb)
    {
        if (filt.IsDangerous())
            throw new SecurityException("检测到弱检索式");

        var cnt = 0;
        var cntAll = 0;
        var res = session.Accountant.SelectVouchers(filt.VoucherQuery);
        var ops = new List<Voucher>();
        foreach (var v in res)
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

        session.Accountant.Upsert(ops);
        sb.AppendLine($"{cntAll} selected");
        sb.AppendLine($"{cnt} unmarked");
    }

    private void RunCheck(Session session, IVoucherDetailQuery filt, CsvParser parsed, StringBuilder sb)
    {
        if (filt.IsDangerous())
            throw new SecurityException("检测到弱检索式");

        var res = session.Accountant.SelectVouchers(filt.VoucherQuery);
        var lst = new List<BankItem>(parsed.Items);
        foreach (var v in res)
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

            sb.AppendLine($"{v.ID.Quotation('^')} {v.Date.AsDate()} {d.Content} {d.Fund.AsCurrency(d.Currency)}");
        }

        foreach (var b in lst)
            sb.AppendLine(b.Raw);
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
