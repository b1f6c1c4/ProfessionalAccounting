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
using System.Linq;
using System.Threading.Tasks;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL.Util;

internal abstract class SubtotalResult : ISubtotalResult
{
    public List<ISubtotalResult> TheItems { get; set; }
    public double Fund { get; set; }

    public IEnumerable<ISubtotalResult> Items => TheItems?.AsReadOnly();
    public IAsyncEnumerable<Balance> Balances { get; set; }

    public abstract T Accept<T>(ISubtotalVisitor<T> visitor);
}

internal abstract class SubtotalResultFactory<T>
{
    public abstract T Selector(Balance b);
    public SubtotalResult Create(IAsyncGrouping<T, Balance> grp)
    {
        var obj = DoCreate(grp);
        obj.Balances = grp;
        return obj;
    }
    protected abstract SubtotalResult DoCreate(IAsyncGrouping<T, Balance> grp);
}

internal class SubtotalRoot : SubtotalResult, ISubtotalRoot
{
    public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
}

internal class SubtotalDate : SubtotalResult, ISubtotalDate
{
    public SubtotalDate(DateTime? date, SubtotalLevel level)
    {
        Date = date;
        Level = level;
    }

    public SubtotalLevel Level { get; }

    public DateTime? Date { get; }

    public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
}

internal class SubtotalDateFactory : SubtotalResultFactory<DateTime?>
{
    private readonly SubtotalLevel m_Level;
    public SubtotalDateFactory(SubtotalLevel level) => m_Level = level;

    public override DateTime? Selector(Balance b) => b.Date;
    protected override SubtotalResult DoCreate(IAsyncGrouping<DateTime?, Balance> grp) => new SubtotalDate(grp.Key, m_Level);
}

internal class SubtotalVoucherRemark : SubtotalResult, ISubtotalVoucherRemark
{
    public SubtotalVoucherRemark(string vremark) => VoucherRemark = vremark;
    public string VoucherRemark { get; }

    public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
}

internal class SubtotalVoucherRemarkFactory : SubtotalResultFactory<string>
{
    public override string Selector(Balance b) => b.VoucherRemark;
    protected override SubtotalResult DoCreate(IAsyncGrouping<string, Balance> grp) => new SubtotalVoucherRemark(grp.Key);
}

internal class SubtotalVoucherType : SubtotalResult, ISubtotalVoucherType
{
    public SubtotalVoucherType(VoucherType ty) => Type = ty;
    public VoucherType? Type { get; }

    public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
}

internal class SubtotalVoucherTypeFactory : SubtotalResultFactory<VoucherType>
{
    public override VoucherType Selector(Balance b) => b.VoucherType;
    protected override SubtotalResult DoCreate(IAsyncGrouping<VoucherType, Balance> grp) => new SubtotalVoucherType(grp.Key);
}

internal class SubtotalTitleKind : SubtotalResult, ISubtotalTitleKind
{
    public SubtotalTitleKind(TitleKind? kind) => Kind = kind;
    public TitleKind? Kind { get; }

    public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
}

internal class SubtotalTitleKindFactory : SubtotalResultFactory<TitleKind?>
{
    public override TitleKind? Selector(Balance b)
        => b.Title switch {
            (< 1000) => null,
            (< 2000) => TitleKind.Asset,
            (< 3000) => TitleKind.Liability,
            (< 4000) => TitleKind.Mutual,
            (< 5000) => TitleKind.Equity,
            (< 6000) => TitleKind.Cost,
            (< 6400) => TitleKind.Revenue,
            6603 when b.SubTitle == 02 => TitleKind.Revenue,
            6603 when b.SubTitle != 02 => TitleKind.Expense,
            (< 7000) => TitleKind.Expense,
            _ => null,
        };

    protected override SubtotalResult DoCreate(IAsyncGrouping<TitleKind?, Balance> grp) => new SubtotalTitleKind(grp.Key);
}

internal class SubtotalUser : SubtotalResult, ISubtotalUser
{
    public SubtotalUser(string user) => User = user;
    public string User { get; }

    public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
}

internal class SubtotalUserFactory : SubtotalResultFactory<string>
{
    public override string Selector(Balance b) => b.User;
    protected override SubtotalResult DoCreate(IAsyncGrouping<string, Balance> grp) => new SubtotalUser(grp.Key);
}

internal class SubtotalCurrency : SubtotalResult, ISubtotalCurrency
{
    public SubtotalCurrency(string currency) => Currency = currency;
    public string Currency { get; }

    public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
}

internal class SubtotalCurrencyFactory : SubtotalResultFactory<string>
{
    public override string Selector(Balance b) => b.Currency;
    protected override SubtotalResult DoCreate(IAsyncGrouping<string, Balance> grp) => new SubtotalCurrency(grp.Key);
}

internal class SubtotalTitle : SubtotalResult, ISubtotalTitle
{
    public SubtotalTitle(int? title) => Title = title;
    public int? Title { get; }

    public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
}

internal class SubtotalTitleFactory : SubtotalResultFactory<int?>
{
    public override int? Selector(Balance b) => b.Title;
    protected override SubtotalResult DoCreate(IAsyncGrouping<int?, Balance> grp) => new SubtotalTitle(grp.Key);
}

internal class SubtotalSubTitle : SubtotalResult, ISubtotalSubTitle
{
    public SubtotalSubTitle(int? subTitle) => SubTitle = subTitle;
    public int? SubTitle { get; }

    public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
}

internal class SubtotalSubTitleFactory : SubtotalResultFactory<int?>
{
    public override int? Selector(Balance b) => b.SubTitle;
    protected override SubtotalResult DoCreate(IAsyncGrouping<int?, Balance> grp) => new SubtotalSubTitle(grp.Key);
}

internal class SubtotalContent : SubtotalResult, ISubtotalContent
{
    public SubtotalContent(string content) => Content = content;
    public string Content { get; }

    public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
}

internal class SubtotalContentFactory : SubtotalResultFactory<string>
{
    public override string Selector(Balance b) => b.Content;
    protected override SubtotalResult DoCreate(IAsyncGrouping<string, Balance> grp) => new SubtotalContent(grp.Key);
}

internal class SubtotalRemark : SubtotalResult, ISubtotalRemark
{
    public SubtotalRemark(string remark) => Remark = remark;
    public string Remark { get; }

    public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
}

internal class SubtotalRemarkFactory : SubtotalResultFactory<string>
{
    public override string Selector(Balance b) => b.Remark;
    protected override SubtotalResult DoCreate(IAsyncGrouping<string, Balance> grp) => new SubtotalRemark(grp.Key);
}

internal class SubtotalValue : SubtotalResult, ISubtotalValue
{
    public SubtotalValue(double? value) => Value = value;
    public double? Value { get; }
    public IAsyncEnumerable<Balance> Values { get; }

    public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
}

internal class SubtotalValueFactory : SubtotalResultFactory<double?>
{
    public override double? Selector(Balance b) => b.Value;
    protected override SubtotalResult DoCreate(IAsyncGrouping<double?, Balance> grp) => new SubtotalValue(grp.Key);
}

/// <summary>
///     分类汇总结果建造者
/// </summary>
public class SubtotalBuilder
{
    private readonly IHistoricalExchange m_Exchange;
    private readonly ISubtotal m_Par;
    private int m_Depth;
    private SubtotalLevel m_Flags;

    public SubtotalBuilder(ISubtotal par, IHistoricalExchange ex)
    {
        m_Par = par;
        m_Exchange = ex;
    }

    /// <summary>
    ///     建造分类汇总结果
    /// </summary>
    /// <param name="raw">原始数据</param>
    /// <returns>分类汇总结果</returns>
    public ValueTask<ISubtotalResult> Build(IAsyncEnumerable<Balance> raw)
    {
        m_Depth = 0;
        m_Flags = SubtotalLevel.None;
        return Build(new SubtotalRoot() { Balances = raw }, raw);
    }

    private async ValueTask<ISubtotalResult> Build(SubtotalResult sub, IAsyncEnumerable<Balance> raw)
    {
        if (m_Par.Levels.Count == m_Depth)
            return await BuildAggrPhase(sub, raw);

        var level = m_Par.Levels[m_Depth];
        m_Depth++;
        await BuildChildren(sub, raw, level);
        m_Depth--;
        if (!sub.TheItems.Any() &&
            sub is not ISubtotalRoot)
            return null;

        sub.Fund = sub.TheItems.Sum(static isr => isr.Fund);
        return sub;
    }

    private async ValueTask BuildChildren(SubtotalResult sub, IAsyncEnumerable<Balance> raw, SubtotalLevel level)
    {
        ValueTask<List<ISubtotalResult>> Invoke<T>(SubtotalResultFactory<T> f) =>
            raw.GroupBy(f.Selector).SelectAwait(g => Build(f.Create(g), g)).Where(static g => g != null).ToListAsync();

        m_Flags = level & SubtotalLevel.Flags;
        sub.TheItems = await ((level & SubtotalLevel.Subtotal) switch
            {
                SubtotalLevel.VoucherRemark => Invoke(new SubtotalVoucherRemarkFactory()),
                SubtotalLevel.VoucherType => Invoke(new SubtotalVoucherTypeFactory()),
                SubtotalLevel.TitleKind => Invoke(new SubtotalTitleKindFactory()),
                SubtotalLevel.Title => Invoke(new SubtotalTitleFactory()),
                SubtotalLevel.SubTitle => Invoke(new SubtotalSubTitleFactory()),
                SubtotalLevel.Content => Invoke(new SubtotalContentFactory()),
                SubtotalLevel.Remark => Invoke(new SubtotalRemarkFactory()),
                SubtotalLevel.User => Invoke(new SubtotalUserFactory()),
                SubtotalLevel.Currency => Invoke(new SubtotalCurrencyFactory()),
                SubtotalLevel.Day => Invoke(new SubtotalDateFactory(level)),
                SubtotalLevel.Week => Invoke(new SubtotalDateFactory(level)),
                SubtotalLevel.Month => Invoke(new SubtotalDateFactory(level)),
                SubtotalLevel.Quarter => Invoke(new SubtotalDateFactory(level)),
                SubtotalLevel.Year => Invoke(new SubtotalDateFactory(level)),
                SubtotalLevel.Value => Invoke(new SubtotalValueFactory()),
                _ => throw new ArgumentOutOfRangeException(),
            });
    }

    private async ValueTask<ISubtotalResult> BuildAggrPhase(SubtotalResult sub, IAsyncEnumerable<Balance> raw)
    {
        switch (m_Par.AggrType)
        {
            case AggregationType.None:
                sub.Fund = await BuildEquiPhase(raw);
                if (m_Flags.HasFlag(SubtotalLevel.NonZero) &&
                    sub.Fund.IsZero() &&
                    sub is not ISubtotalRoot)
                    return null;

                return sub;
            case AggregationType.ChangedDay:
                sub.TheItems = await raw.GroupBy(static b => b.Date).OrderBy(static grp => grp.Key)
                    .SelectAwait(
                        async grp => new SubtotalDate(grp.Key, m_Par.AggrInterval)
                            {
                                Fund = sub.Fund += await BuildEquiPhase(grp),
                            }).Cast<ISubtotalResult>().ToListAsync();
                return sub;
            case AggregationType.EveryDay:
                sub.TheItems = new();
                var initial = Prev(m_Par.EveryDayRange.StartDate);
                var ed = Next(Prev(m_Par.EveryDayRange.EndDate));
                var last = initial;

                void Append(DateTime? curr, double oldFund, double fund)
                {
                    if (last.HasValue &&
                        curr.HasValue)
                        while (last < curr)
                        {
                            last = Next(last.Value);
                            sub.TheItems.Add(
                                new SubtotalDate(last, m_Par.AggrInterval) { Fund = last == curr ? fund : oldFund });
                        }
                    else
                        sub.TheItems.Add(
                            new SubtotalDate(curr, m_Par.AggrInterval) { Fund = fund });

                    if (DateHelper.CompareDate(last, curr) < 0)
                        last = curr;
                }

                var flag = true;
                var tmp0 = 0D;
                var forcedNull =
                    (m_Par.EveryDayRange.StartDate.HasValue || m_Par.EveryDayRange.NullOnly) &&
                    m_Par.EveryDayRange.Nullable;
                foreach (var grp in raw.GroupBy(static b => b.Date).OrderBy(static grp => grp.Key).ToEnumerable())
                {
                    if (flag &&
                        grp.Key != null &&
                        forcedNull)
                        sub.TheItems.Add(
                            new SubtotalDate(null, m_Par.AggrInterval) { Fund = 0 });
                    flag = false;

                    var tmp = sub.Fund;
                    sub.Fund += await BuildEquiPhase(grp);

                    if (DateHelper.CompareDate(grp.Key, ed) <= 0)
                        tmp0 = sub.Fund;
                    if (grp.Key.Within(m_Par.EveryDayRange))
                        Append(grp.Key, tmp, sub.Fund);
                }

                if (flag && forcedNull)
                    sub.TheItems.Add(
                        new SubtotalDate(null, m_Par.AggrInterval) { Fund = 0 });
                if (ed.HasValue &&
                    DateHelper.CompareDate(last, ed) < 0)
                    Append(ed, tmp0, tmp0);
                else if (initial.HasValue &&
                         last == initial)
                    Append(Next(initial.Value), sub.Fund, sub.Fund);

                return sub;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private DateTime? Prev(DateTime? date)
    {
        if (!date.HasValue)
            return null;

        var dt = date.Value;
        return m_Par.AggrInterval switch
            {
                SubtotalLevel.Day => dt.AddDays(-1),
                SubtotalLevel.Week => dt.DayOfWeek == DayOfWeek.Sunday
                    ? dt.AddDays(-13)
                    : dt.AddDays(-6 - (int)dt.DayOfWeek),
                SubtotalLevel.Month =>
                    new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1),
                SubtotalLevel.Quarter =>
                    new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-3 - (dt.Month - 1) % 3),
                SubtotalLevel.Year => new(dt.Year - 1, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                _ => throw new ArgumentOutOfRangeException(),
            };
    }

    private DateTime? Next(DateTime? date)
    {
        if (!date.HasValue)
            return null;

        var dt = date.Value;
        return m_Par.AggrInterval switch
            {
                SubtotalLevel.Day => dt.AddDays(1),
                SubtotalLevel.Week => dt.AddDays(7),
                SubtotalLevel.Month => dt.AddMonths(1),
                SubtotalLevel.Quarter => dt.AddMonths(3),
                SubtotalLevel.Year => dt.AddYears(1),
                _ => throw new ArgumentOutOfRangeException(),
            };
    }

    private ValueTask<double> BuildEquiPhase(IAsyncEnumerable<Balance> raw) => m_Par.EquivalentDate.HasValue
        ? raw.SumAwaitAsync(
            async b => b.Currency.EndsWith('#') ? 0 : b.Fund
                * await m_Exchange.Query(m_Par.EquivalentDate.Value, b.Currency, m_Par.EquivalentCurrency))
        : raw.SumAsync(static b => b.Fund);
}
