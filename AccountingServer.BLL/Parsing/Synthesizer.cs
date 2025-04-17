/* Copyright (C) 2025 b1f6c1c4
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
using System.Text;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL.Parsing;

public class Synthesizer
    : IQueryVisitor<IDetailQueryAtom, (string, int)>,
      IQueryVisitor<IVoucherQueryAtom, (string, int)>
{
    private Synthesizer() { }

    public static string Synth(IQueryCompounded<IDetailQueryAtom> dq) => dq == null ? "(-U)"
        : dq.Accept<(string, int)>(new Synthesizer()).Item1;

    public static string Synth(IQueryCompounded<IVoucherQueryAtom> vq) => vq == null ? "{-U}"
        : vq.Accept<(string, int)>(new Synthesizer()).Item1;

    public static string Synth(IVoucherDetailQuery vdq) => vdq.DetailEmitFilter == null
        ? Synth(vdq.ActualDetailFilter())
        : $"{Synth(vdq.VoucherQuery)} {Synth(vdq.DetailEmitFilter)}";

    public static string Synth(DateFilter dr) => dr.AsDateRange();

    public static string Synth(IEmit e) => $": {Synth(e.DetailFilter)}";

    public static string Synth(IGroupedQuery q) => $"{Synth(q.VoucherEmitQuery)} {Synth(q.Subtotal)}";

    public static string Synth(IVoucherGroupedQuery q) => $"{Synth(q.VoucherQuery)} {Synth(q.Subtotal)}";

    public static string Synth(ISubtotal sub)
    {
        var sb = new StringBuilder();

        switch (sub.GatherType)
        {
            case GatheringType.Sum:
                if (sub.Levels.Count > 0 && sub.Levels[0].HasFlag(SubtotalLevel.NonZero))
                    sb.Append("`");
                else
                    sb.Append("``");
                break;
            case GatheringType.Count: sb.Append("!"); break;
            case GatheringType.VoucherCount: sb.Append("!!"); break;
        }

        foreach (var l in sub.Levels)
            switch (l & ~SubtotalLevel.NonZero)
            {
                case SubtotalLevel.VoucherRemark: sb.Append('R'); break;
                case SubtotalLevel.TitleKind: sb.Append('K'); break;
                case SubtotalLevel.Title: sb.Append('t'); break;
                case SubtotalLevel.SubTitle: sb.Append('s'); break;
                case SubtotalLevel.Content: sb.Append('c'); break;
                case SubtotalLevel.Remark: sb.Append('r'); break;
                case SubtotalLevel.Currency: sb.Append('C'); break;
                case SubtotalLevel.User: sb.Append('U'); break;
                case SubtotalLevel.Day: sb.Append('d'); break;
                case SubtotalLevel.Week: sb.Append('w'); break;
                case SubtotalLevel.Month: sb.Append('m'); break;
                case SubtotalLevel.Quarter: sb.Append('q'); break;
                case SubtotalLevel.Year: sb.Append('y'); break;
                case SubtotalLevel.Value: sb.Append('V'); break;
                default: throw new ArgumentOutOfRangeException();
            }

        if (sub.AggrType != AggregationType.None)
        {
            switch (sub.AggrInterval)
            {
                case SubtotalLevel.Day: sb.Append('D'); break;
                case SubtotalLevel.Week: sb.Append('W'); break;
                case SubtotalLevel.Month: sb.Append('M'); break;
                case SubtotalLevel.Quarter: sb.Append('Q'); break;
                case SubtotalLevel.Year: sb.Append('Y'); break;
                default: throw new ArgumentOutOfRangeException();
            }

            switch (sub.AggrType)
            {
                case AggregationType.ChangedDay: break;
                case AggregationType.EveryDay: sb.Append($"{Synth(sub.EveryDayRange)}"); break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        if (sub.EquivalentCurrency != null)
        {
            sb.Append($"X{sub.EquivalentCurrency.AsCurrency()}");
            if (sub.EquivalentDate != null)
                sb.Append($"[{sub.EquivalentDate.AsDate()}]");
        }

        return sb.ToString();
    }

    public (string, int) Visit(IDetailQueryAtom query)
    {
        var sb = new StringBuilder();

        if (query.Filter.User != null)
            sb.Append($"{query.Filter.User.AsUser()} ");
        else
            sb.Append("U ");

        if (query.Filter.Currency != null)
            sb.Append($"{query.Filter.Currency.AsCurrency()} ");

        if (query.Kind != null)
            sb.Append($"{query.Kind} ");

        if (query.Filter.Title != null)
        {
            sb.Append(query.Filter.Title.AsTitle());
            if (query.Filter.SubTitle != null)
                sb.Append($"{query.Filter.SubTitle.AsSubTitle()} ");
            else
                sb.Append(' ');
        }
        else if (query.Filter.SubTitle != null)
            sb.Append($"T0000{query.Filter.SubTitle.AsSubTitle()}");

        if (query.Filter.Content != null)
            sb.Append($"{query.Filter.Content.Quotation('\'')} ");

        if (query.ContentPrefix != null)
            sb.Append($"{query.ContentPrefix.Quotation('\'')}.* ");

        if (query.Filter.Remark != null)
            sb.Append($"{query.Filter.Remark.Quotation('"')} ");

        if (query.RemarkPrefix != null)
            sb.Append($"{query.RemarkPrefix.Quotation('"')}.* ");

        if (query.Filter.Fund != null)
            if (query.Filter.Fund.Value < -VoucherDetail.Tolerance)
                sb.Append($"={query.Filter.Fund.Value:R} ");
            else if (query.Filter.Fund.Value > +VoucherDetail.Tolerance)
                sb.Append($"=+{query.Filter.Fund.Value:R} ");
            else
                sb.Append("=0 ");

        if (query.Dir == +1)
            sb.Append("> ");
        else if (query.Dir == -1)
            sb.Append("< ");

        sb.Remove(sb.Length - 1, 1);
        return (sb.ToString(), 0);
    }

    public (string, int) Visit(IVoucherQueryAtom query)
    {
        var sb = new StringBuilder();

        sb.Append($"{query.DetailFilter.Accept<(string, int)>(this).Item1} ");

        if (query.ForAll)
            sb.Append("A ");

        if (query.Range.StartDate.HasValue || query.Range.EndDate.HasValue || query.Range.NullOnly)
            sb.Append($"{Synth(query.Range)} ");

        if (query.VoucherFilter.ID != null)
            sb.Append($"{query.VoucherFilter.ID.Quotation('^')} ");

        if (query.VoucherFilter.Remark != null)
            sb.Append($"{query.VoucherFilter.Remark.Quotation('%')} ");

        if (query.RemarkPrefix != null)
            sb.Append($"{query.RemarkPrefix.Quotation('%')}.* ");

        if (query.VoucherFilter.Type != null)
            if (query.VoucherFilter.Type == VoucherType.General)
                sb.Append("G ");
            else
                sb.Append($"{query.VoucherFilter.Type} ");

        sb.Remove(sb.Length - 1, 1);
        return (sb.ToString(), 0);
    }

    public (string, int) Visit(IQueryAry<IDetailQueryAtom> query)
    {
        static string Q(string s, bool cond)
            => cond ? $"({s})" : s;

        var (f1, l1) = query.Filter1.Accept<(string, int)>(this);
        switch (query.Operator)
        {
            case OperatorType.None:
            case OperatorType.Identity:
                return (f1, l1);
            case OperatorType.Complement:
                return ($"-{Q(f1, l1 <= -1)}", -2);
        }
        var (f2, l2) = (query.Filter2 ?? query.Filter1).Accept<(string, int)>(this);
        switch (query.Operator)
        {
            case OperatorType.Union:
                return ($"{Q(f1, l1 <= -2)} + {Q(f2, l2 <= -2)}", -1);
            case OperatorType.Subtract:
                return ($"{Q(f1, l1 <= -2)} - {Q(f2, l2 <= -1)}", -2);
            case OperatorType.Intersect:
                return ($"{Q(f1, l1 <= -1)} * {Q(f2, l2 <= -1)}", 1);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public (string, int) Visit(IQueryAry<IVoucherQueryAtom> query)
    {
        static string Q(string s, bool cond)
            => cond ? $"{{{s}}}" : s;

        var (f1, l1) = query.Filter1.Accept<(string, int)>(this);
        switch (query.Operator)
        {
            case OperatorType.None:
            case OperatorType.Identity:
                return (f1, l1);
            case OperatorType.Complement:
                return ($"-{Q(f1, l1 <= 2)}", 1);
        }
        var (f2, l2) = (query.Filter2 ?? query.Filter1).Accept<(string, int)>(this);
        switch (query.Operator)
        {
            case OperatorType.Union:
                return ($"{Q(f1, l1 <= 0)} + {Q(f2, l2 <= 0)}", 2);
            case OperatorType.Subtract:
                return ($"{Q(f1, l1 <= 0)} - {Q(f2, l2 <= 2)}", 1);
            case OperatorType.Intersect:
                return ($"{Q(f1, l1 <= 2)} * {Q(f2, l2 <= 2)}", 3);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
