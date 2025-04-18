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
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell.Plugins.Statement;

/// <summary>
///     自动对账
/// </summary>
public class Statement : PluginBase
{
    static Statement()
        => Cfg.RegisterType<StmtTargets>("Statement");

    /// <inheritdoc />
    public override async IAsyncEnumerable<string> Execute(string expr, Context ctx)
    {
        var csv = expr;
        expr = ParsingF.Line(ref csv);

        if (ParsingF.Optional(ref expr, "mark"))
        {
            var nm = ParsingF.Token(ref expr);
            var tgt = Cfg.Get<StmtTargets>().Targets.Single(t => t.Name == nm);
            var parsed = new CsvParser(tgt.Reversed);
            var filt = ParsingF.DetailQuery(tgt.Query, ctx.Client);
            if (!ParsingF.Optional(ref expr, "as"))
                throw new FormatException("格式错误");
            var marker = ParsingF.Token(ref expr);
            ParsingF.Eof(expr);
            if (string.IsNullOrWhiteSpace(marker))
                throw new FormatException("格式错误");

            parsed.Parse(ref csv);
            var sb = new StringBuilder();
            ctx.Identity.WillInvoke($"$stmt$ {nm}");
            await RunMark(ctx, sb, filt, parsed, marker);
            yield return sb.ToString();

            yield break;
        }

        if (ParsingF.Optional(ref expr, "unmark"))
        {
            var nm = ParsingF.Token(ref expr);
            var tgt = Cfg.Get<StmtTargets>().Targets.Single(t => t.Name == nm);
            var filt = ParsingF.DetailQuery(tgt.Query, ctx.Client);
            ParsingF.Eof(expr);
            var sb = new StringBuilder();
            ctx.Identity.WillInvoke($"$stmt$ {nm}");
            await RunUnmark(ctx, sb, filt);
            yield return sb.ToString();

            yield break;
        }

        if (ParsingF.Optional(ref expr, "check"))
        {
            var nm = ParsingF.Token(ref expr);
            var tgt = Cfg.Get<StmtTargets>().Targets.Single(t => t.Name == nm);
            var filt = ParsingF.DetailQuery(tgt.Query, ctx.Client);
            ParsingF.Eof(expr);
            var sb = new StringBuilder();
            ctx.Identity.WillInvoke($"$stmt$ {nm}");
            await RunCheck(ctx, sb, filt, tgt);
            yield return sb.ToString();

            yield break;
        }

        if (ParsingF.Optional(ref expr, "xml"))
        {
            var filt = ParsingF.DetailQuery(ref expr, ctx.Client);
            ParsingF.Eof(expr);
            var sb = new StringBuilder();
            var tgt = new Target()
                {
                    Lengths = new(),
                    Overlaps = new(),
                    Skips = new(),
                };
            ctx.Identity.WillInvoke("$stmt$ xml");
            await RunCheck(ctx, sb, filt, tgt, true);
            yield return sb.ToString();

            yield break;
        }

        {
            var nm = ParsingF.Token(ref expr);
            var tgt = Cfg.Get<StmtTargets>().Targets.Single(t => t.Name == nm);
            var parsed = new CsvParser(tgt.Reversed);
            var filt = ParsingF.DetailQuery(tgt.Query, ctx.Client);
            if (!ParsingF.Optional(ref expr, "as"))
                throw new FormatException("格式错误");
            var marker = ParsingF.Token(ref expr);
            var force = ParsingF.Optional(ref expr, "--force");
            ParsingF.Eof(expr);
            if (string.IsNullOrWhiteSpace(marker))
                throw new FormatException("格式错误");

            parsed.Parse(ref csv);
            var markerFilt = new VoucherDetailQuery(
                filt.VoucherQuery,
                new IntersectQueries<IDetailQueryAtom>(
                    filt.ActualDetailFilter(),
                    new SimpleDetailQuery { Filter = new() { Remark = marker } }));
            var nullFilt = new VoucherDetailQuery(
                filt.VoucherQuery,
                new IntersectQueries<IDetailQueryAtom>(
                    filt.ActualDetailFilter(),
                    new SimpleDetailQuery { Filter = new() { Remark = "" } }));
            var sb = new StringBuilder();
            sb.AppendLine($"{parsed.Items.Count} parsed");
            ctx.Identity.WillInvoke($"$stmt$ {nm}");
            await using var vir = ctx.Accountant.Virtualize();
            await RunUnmark(ctx, sb, markerFilt);
            if (await RunMark(ctx, sb, nullFilt, parsed, marker)
                || await RunCheck(ctx, sb, filt, tgt) && !force)
            {
                sb.AppendLine("ABORT, rewind by unmark them back");
                await RunUnmark(ctx, sb, markerFilt);

                yield return "\x0c Transaction Aborted \x0c\n";
                yield return sb.ToString();
                yield return $"Virtualizer: {vir}\n";
                yield return "\x0c Attached a copy of original CSV, ready for resubmit. \x0c\n";
                yield return csv;
            }
            else
            {
                yield return sb.ToString();
                yield return $"Virtualizer: {vir}\n";
            }
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<string> ListHelp()
    {
        await foreach (var s in base.ListHelp())
            yield return s;

        foreach (var tgt in Cfg.Get<StmtTargets>().Targets)
            yield return $"{tgt.Name,4} {(tgt.Reversed ? '-' : ' ')} {tgt.Query}\n";
    }

    private static async ValueTask<bool> RunMark(Context ctx, StringBuilder sb,
        IVoucherDetailQuery filt, CsvParser parsed, string marker)
    {
        if (filt.IsDangerous())
            throw new SecurityException("检测到弱检索式");

        var hasViolation = false;
        var marked = 0;
        var remarked = 0;
        var converted = 0;
        var res = await ctx.Accountant.SelectVouchersAsync(filt.VoucherQuery).ToListAsync();
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

        await ctx.Accountant.UpsertAsync(ops);

        sb.AppendLine($"{marked} marked");
        sb.AppendLine($"{remarked} remarked");
        sb.AppendLine($"{converted} converted");

        return hasViolation;
    }

    private static async ValueTask RunUnmark(Context ctx, StringBuilder sb, IVoucherDetailQuery filt)
    {
        if (filt.IsDangerous())
            throw new SecurityException("检测到弱检索式");

        var cnt = 0;
        var cntAll = 0;
        var res = ctx.Accountant.SelectVouchersAsync(filt.VoucherQuery);
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

        await ctx.Accountant.UpsertAsync(ops);
        sb.AppendLine($"{cntAll} selected");
        sb.AppendLine($"{cnt} unmarked");
    }

    [Flags]
    public enum IncidentType
    {
        None = 0,
        Error = 0x1000,

        // Errors
        NonMonotonic = 0x1004,
        PeriodTooLong = 0x1003,
        OverlapTooMuch = 0x1002,
        UnknownVoucher = 0x1001, // "" found otherwise

        // Warnings
        PeriodUnmarked = 0x2, // "" found between two non-adjacent period
        FinalOverlap = 0x1, // "" found before last period ends (but after 2nd last period ends)
    };

    public readonly record struct Incident(IncidentType Ty, int From, int To, ISpec Spec);

    public static IEnumerable<Incident> CalculateIncidents(IList<(string, DateTime)> lst, Target tgt)
    {
        var incidents = new List<Incident>();

        // Obtain period range (both inclusive)
        string lbs = null, ubs = null;
        var reg = new Regex(@"^20[1-2][0-9](?:0[1-9]|1[0-2])$");
        foreach (var (r, _) in lst)
        {
            if (string.IsNullOrEmpty(r))
                continue;
            if (!reg.IsMatch(r))
                throw new ApplicationException($"Invalid remark: {r}");
            if (lbs == null || string.Compare(lbs, r) > 0)
                lbs = r;
            if (ubs == null || string.Compare(ubs, r) < 0)
                ubs = r;
        }
        if (lbs == null || ubs == null)
            yield break;

        // Create buckets for each period
        var lb = DateTimeParser.ParseExact($"{lbs}01", "yyyyMMdd");
        var ub = DateTimeParser.ParseExact($"{ubs}01", "yyyyMMdd");
        var rngs = new List<(string, int?, int?)>(); // inclusive
        for (var c = lb; c <= ub; c = c.AddMonths(1))
            rngs.Add(($"{c:yyyyMM}", null, null));

        // Obtain range for each period
        for (var i = 0; i < lst.Count; i++)
        {
            var (r, _) = lst[i];
            if (string.IsNullOrEmpty(r))
                continue;
            var c = DateTimeParser.ParseExact($"{r}01", "yyyyMMdd");
            int id;
            for (id = 0; id < rngs.Count; id++)
                if (c == lb.AddMonths(id))
                    break;
            var (period, lbi, ubi) = rngs[id];
            if (!lbi.HasValue)
                lbi = ubi = i;
            else if (lbi > i)
                lbi = i;
            else if (ubi < i)
                ubi = i;
            rngs[id] = (period, lbi, ubi);
        }

        // Check for monotonicity
        var last_lb = -1;
        var last_ub = -1;
        var last_last_ub = -1;
        var flag = false;
        foreach (var (period, lbi, ubi) in rngs)
        {
            if (!lbi.HasValue)
                continue;

            if (last_ub > ubi!.Value || last_lb > lbi!.Value)
            {
                flag = true;
                yield return new(IncidentType.NonMonotonic, lbi.Value, last_ub, null);
            }
            else if (last_last_ub >= lbi)
            {
                flag = true;
                yield return new(IncidentType.NonMonotonic, last_last_ub, lbi.Value, null);
            }

            last_last_ub = last_ub;
            last_lb = lbi.Value;
            last_ub = ubi.Value;
        }
        if (flag)
            yield break;

        // Estimate empty periods, detect ""s, check overlap
        for (var j = 0; j < rngs.Count; j++)
        {
            var (period1, lbi1, ubi1) = rngs[j];
            if (!lbi1.HasValue)
                continue;

            var lbd1 = lst[lbi1!.Value].Item2;
            var ubd1 = lst[ubi1!.Value].Item2;

            var ubi0 = j >= 1 ? rngs[j - 1].Item3 : null;
            var lbi2 = j < rngs.Count - 1 ? rngs[j + 1].Item2 : null;

            //       <==>
            // 000000    111111
            if (ubi0.HasValue && ubi0.Value < lbi1!.Value - 1)
                yield return new(IncidentType.UnknownVoucher, ubi0!.Value + 1, lbi1!.Value - 1, null);

            //     <====>
            // 00001110001111
            if (ubi0.HasValue)
            {
                var (period0, _, _) = rngs[j - 1];
                var ubd0 = lst[ubi0.Value].Item2;
                var d = (int)(ubd0 - lbd1).TotalDays;
                var tol = tgt.Overlaps.SingleOrDefault(p => p.From == period0 && p.To == period1)?.Tolerance ?? 1;
                if (d > tol)
                    yield return new(IncidentType.OverlapTooMuch,
                            lbi1.Value, ubi0.Value, new OverlapSpec { From = period0, To = period1, Tolerance = d });
            }

            int center;
            if (!ubi0.HasValue && !lbi2.HasValue) // LH + RH
            {
                var d = lbd1 + (ubd1 - lbd1) / 2;
                var l = lbi1.Value;
                var r = ubi1.Value + 1;
                center = l;
                while (r - l > 1)
                {
                    center = (l + r) / 2;
                    if (lst[center].Item2 >= d)
                        r = center;
                    else
                        l = center;
                }
            }
            else if (ubi0.HasValue && !lbi2.HasValue) // RH
                center = ubi0.Value;
            else if (!ubi0.HasValue && lbi2.HasValue) // LH
            {
                center = lbi2.Value;
                //          *
                //    111112 122222
                for (var i = lbi2.Value; i < ubi1.Value; i++)
                    if (lst[i].Item1 == null)
                        yield return new(IncidentType.UnknownVoucher, i, i, null);
            }
            else
            {
                //         *
                // 00001111 1112222
                //           *
                // 0000111112 12222
                for (var i = Math.Max(ubi0.Value, lbi1.Value); i < ubi1.Value; i++)
                    if (lst[i].Item1 == null)
                        yield return new(IncidentType.UnknownVoucher, i, i, null);

                continue;
            }

            if (!lbi2.HasValue) // RH
            {
                for (var i = center; i < ubi1.Value; i++)
                {
                    var (r, d) = lst[i];
                    if (r == null)
                    {
                        //        <==>
                        // ~~~1111  11     2
                        var days = (int)(ubd1 - d).TotalDays;
                        var tol = tgt.Overlaps.SingleOrDefault(p => p.From == period1 && string.IsNullOrEmpty(p.To))?.Tolerance ?? 1;
                        if (days > tol)
                            yield return new(
                                        j == rngs.Count - 1 ? IncidentType.FinalOverlap : IncidentType.OverlapTooMuch,
                                        i, ubi1.Value, new OverlapSpec { From = period1, Tolerance = days });
                        break;
                    }
                }
            }
            if (!ubi0.HasValue) // LH
            {
                for (var i = center; i > lbi1.Value; i--)
                {
                    var (r, d) = lst[i];
                    if (r == null)
                    {
                        //       <==>
                        // 0     11  1111~~~~
                        var days = (int)(d - lbd1).TotalDays;
                        var tol = tgt.Overlaps.SingleOrDefault(p => string.IsNullOrEmpty(p.From) && p.To == period1)?.Tolerance ?? 1;
                        if (days > tol)
                            yield return new(
                                    IncidentType.OverlapTooMuch, lbi1.Value, i, new OverlapSpec { To = period1, Tolerance = days });
                        break;
                    }
                }
            }
        }

        // Check long periods
        foreach (var (period, lbi, ubi) in rngs)
        {
            if (!lbi.HasValue)
                continue;

            var lbd = lst[lbi!.Value].Item2;
            var ubd = lst[ubi!.Value].Item2;
            var d = (int)(ubd - lbd).TotalDays + 1; // both inclusive
            var tol = tgt.Lengths.SingleOrDefault(p => p.Period == period)?.Tolerance ?? 1;
            if (d > 31 + tol)
                yield return new(IncidentType.PeriodTooLong, lbi.Value, ubi.Value, new LengthSpec { Period = period, Tolerance = d - 31 });
        }

        // Check empty periods
        for (var j = 0; j < rngs.Count; j++)
        {
            var (period1, lbi1, ubi1) = rngs[j];
            if (lbi1.HasValue)
                continue;

            var (period0, _, ubi0) = rngs[j - 1]; // last period, guaranteed non-empty

            int k;
            for (k = j + 1; k < rngs.Count; k++)
                if (rngs[k].Item2.HasValue)
                    break;

            var (period2, lbi2, ubi2) = rngs[k]; // first non-empty period after this

            if (lbi2!.Value - ubi0!.Value > 1)
                yield return new(IncidentType.PeriodUnmarked, ubi0!.Value + 1, lbi2!.Value - 1, null);

            j = k;
        }
    }

    private async ValueTask<bool> RunCheck(Context ctx, StringBuilder sb,
        IVoucherDetailQuery filt, Target tgt, bool pure = false)
    {
        if (filt.IsDangerous())
            throw new SecurityException("检测到弱检索式");

        var res = await ctx.Accountant.SelectVouchersAsync(filt.VoucherQuery)
            .Where(v => !tgt.Skips.Any(s => s.ID == v.ID))
            .Where(static v => v.Date.HasValue)
            .SelectMany(v => v.Details.Where(d => d.IsMatch(filt.ActualDetailFilter()))
                .Where(d => d.Remark != "reconciliation")
                .Select(d => (v.ID, v.Date, Fund: d.Fund.AsFund(d.Currency), d.Remark))
                .ToAsyncEnumerable())
            .OrderBy(static tuple => tuple.Date).ThenBy(static tuple => tuple.Remark)
            .ToListAsync();

        string Serialize(ISpec spec)
        {
            var en = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            var settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true };
            var xs = new XmlSerializer(spec.GetType());
            using var sw = new StringWriter();
            using var w = XmlWriter.Create(sw, settings);
            xs.Serialize(w, spec, en);
            return sw.ToString();
        }

        if (pure)
            sb.AppendLine("  <Target name=\"\" query=\"\">");

        var flag = false;
        foreach (var incident in CalculateIncidents(res.Select(static f => (f.Remark, f.Date!.Value)).ToList(), tgt))
        {
            if (pure)
            {
                if (incident.Spec == null)
                    sb.AppendLine($"    <!-- {incident.Ty} -->");
                else
                    sb.AppendLine($"    {Serialize(incident.Spec)}");
                continue;
            }

            if (incident.Ty.HasFlag(IncidentType.Error))
            {
                sb.Append("VIOLATION: ");
                flag = true;
            }
            else
                sb.Append("WARNING: ");

            sb.Append(incident.Ty.ToString());
            if (incident.Spec != null)
                sb.AppendLine($"    {Serialize(incident.Spec)}");
            else
                sb.AppendLine();

            for (var i = Math.Max(0, incident.From - 4); i < Math.Min(res.Count, incident.To + 5); i++)
            {
                sb.Append($"{res[i].ID.Quotation('^')} {res[i].Date.AsDate()} ");
                sb.Append($"{res[i].Remark.Quotation('"').CPadRight(8)} {res[i].Fund.CPadLeft(13)}");
                if (i == incident.From && i == incident.To)
                    sb.Append(" (*)");
                if (i == incident.From)
                    sb.Append("  ^");
                else if (i == incident.To)
                    sb.Append("  v");
                else if (i > incident.From && i < incident.To)
                    sb.Append("  |");
                sb.AppendLine();
            }
        }

        if (pure)
            sb.AppendLine($"  </Target>");

        return flag;
    }
}
