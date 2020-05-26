using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL.Util
{
    internal abstract class SubtotalResult : ISubtotalResult
    {
        internal List<ISubtotalResult> TheItems { get; set; }
        public double Fund { get; set; }

        public IEnumerable<ISubtotalResult> Items => TheItems?.AsReadOnly();

        public abstract T Accept<T>(ISubtotalVisitor<T> visitor);
    }

    internal interface ISubtotalResultFactory<T> where T : IEnumerable<Balance>
    {
        IEnumerable<T> Group(IEnumerable<Balance> bs);
        SubtotalResult Create(T grp);
    }

    internal abstract class SubtotalResultFactory<T> : ISubtotalResultFactory<IGrouping<T, Balance>>
    {
        public IEnumerable<IGrouping<T, Balance>> Group(IEnumerable<Balance> bs) => bs.GroupBy(Selector);
        public abstract T Selector(Balance b);
        public abstract SubtotalResult Create(IGrouping<T, Balance> grp);
    }

    internal class SubtotalRoot : SubtotalResult, ISubtotalRoot
    {
        public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
    }

    internal class SubtotalRootFactory : ISubtotalResultFactory<IEnumerable<Balance>>
    {
        public IEnumerable<IEnumerable<Balance>> Group(IEnumerable<Balance> bs) => new[] { bs };

        public SubtotalResult Create(IEnumerable<Balance> grp) => new SubtotalRoot();
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
        public override SubtotalResult Create(IGrouping<DateTime?, Balance> grp) => new SubtotalDate(grp.Key, m_Level);
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
        public override SubtotalResult Create(IGrouping<string, Balance> grp) => new SubtotalUser(grp.Key);
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
        public override SubtotalResult Create(IGrouping<string, Balance> grp) => new SubtotalCurrency(grp.Key);
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
        public override SubtotalResult Create(IGrouping<int?, Balance> grp) => new SubtotalTitle(grp.Key);
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
        public override SubtotalResult Create(IGrouping<int?, Balance> grp) => new SubtotalSubTitle(grp.Key);
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
        public override SubtotalResult Create(IGrouping<string, Balance> grp) => new SubtotalContent(grp.Key);
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
        public override SubtotalResult Create(IGrouping<string, Balance> grp) => new SubtotalRemark(grp.Key);
    }

    /// <summary>
    ///     分类汇总结果建造者
    /// </summary>
    public class SubtotalBuilder
    {
        private readonly IExchange m_Exchange;
        private readonly ISubtotal m_Par;
        private int m_Depth;
        private SubtotalLevel m_Flags;

        public SubtotalBuilder(ISubtotal par, IExchange ex)
        {
            m_Par = par;
            m_Exchange = ex;
        }

        /// <summary>
        ///     建造分类汇总结果
        /// </summary>
        /// <param name="raw">原始数据</param>
        /// <returns>分类汇总结果</returns>
        public ISubtotalResult Build(IEnumerable<Balance> raw)
        {
            m_Depth = 0;
            m_Flags = SubtotalLevel.None;
            return Group(raw).SingleOrDefault();
        }

        private List<ISubtotalResult> Group(IEnumerable<Balance> raw)
        {
            List<ISubtotalResult> Invoke<T>(ISubtotalResultFactory<T> f) where T : IEnumerable<Balance>
                => f.Group(raw).Select(g =>
                    {
                        var sub = f.Create(g);
                        if (m_Par.Levels.Count == m_Depth)
                            return BuildAggrPhase(sub, g);

                        sub.TheItems = Group(g);
                        if (!sub.TheItems.Any() &&
                            !(sub is ISubtotalRoot))
                            return null;

                        sub.Fund = sub.TheItems.Sum(isr => isr.Fund);
                        return sub;
                    }).Where(g => g != null).ToList();

            var level = m_Par.Levels[m_Depth++];
            try
            {
                m_Flags = level & SubtotalLevel.Flags;
                switch (level & SubtotalLevel.Subtotal)
                {
                    case SubtotalLevel.None:
                        return Invoke(new SubtotalRootFactory());
                    case SubtotalLevel.Title:
                        return Invoke(new SubtotalTitleFactory());
                    case SubtotalLevel.SubTitle:
                        return Invoke(new SubtotalSubTitleFactory());
                    case SubtotalLevel.Content:
                        return Invoke(new SubtotalContentFactory());
                    case SubtotalLevel.Remark:
                        return Invoke(new SubtotalRemarkFactory());
                    case SubtotalLevel.User:
                        return Invoke(new SubtotalUserFactory());
                    case SubtotalLevel.Currency:
                        return Invoke(new SubtotalCurrencyFactory());
                    case SubtotalLevel.Day:
                    case SubtotalLevel.Week:
                    case SubtotalLevel.Month:
                    case SubtotalLevel.Year:
                        return Invoke(new SubtotalDateFactory(level));
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                m_Depth--;
            }
        }

        private ISubtotalResult BuildAggrPhase(SubtotalResult sub, IEnumerable<Balance> raw)
        {
            switch (m_Par.AggrType)
            {
                case AggregationType.None:
                    sub.Fund = BuildEquiPhase(raw);
                    if (m_Flags.HasFlag(SubtotalLevel.NonZero) &&
                        sub.Fund.IsZero() &&
                        !(sub is ISubtotalRoot))
                        return null;

                    return sub;
                case AggregationType.ChangedDay:
                    sub.TheItems = raw.GroupBy(b => b.Date).OrderBy(grp => grp.Key)
                        .Select(
                            grp => new SubtotalDate(grp.Key, m_Par.AggrInterval)
                                {
                                    Fund = sub.Fund += BuildEquiPhase(grp),
                                }).Cast<ISubtotalResult>().ToList();
                    return sub;
                case AggregationType.EveryDay:
                    sub.TheItems = new List<ISubtotalResult>();
                    var initial = Prev(m_Par.EveryDayRange.Range.StartDate);
                    var ed = Next(Prev(m_Par.EveryDayRange.Range.EndDate));
                    var last = initial;

                    void Append(DateTime? curr, double oldFund, double fund)
                    {
                        if (last.HasValue &&
                            curr.HasValue)
                            while (last < curr)
                            {
                                last = Next(last.Value);
                                sub.TheItems.Add(
                                    new SubtotalDate(last, m_Par.AggrInterval)
                                        {
                                            Fund = last == curr ? fund : oldFund,
                                        });
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
                        (m_Par.EveryDayRange.Range.StartDate.HasValue || m_Par.EveryDayRange.Range.NullOnly) &&
                        m_Par.EveryDayRange.Range.Nullable;
                    foreach (var grp in raw.GroupBy(b => b.Date).OrderBy(grp => grp.Key))
                    {
                        if (flag &&
                            grp.Key != null &&
                            forcedNull)
                            sub.TheItems.Add(
                                new SubtotalDate(null, m_Par.AggrInterval) { Fund = 0 });
                        flag = false;

                        var tmp = sub.Fund;
                        sub.Fund += BuildEquiPhase(grp);

                        if (DateHelper.CompareDate(grp.Key, ed) <= 0)
                            tmp0 = sub.Fund;
                        if (grp.Key.Within(m_Par.EveryDayRange.Range))
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
            switch (m_Par.AggrInterval)
            {
                case SubtotalLevel.Day:
                    return dt.AddDays(-1);
                case SubtotalLevel.Week:
                    return dt.DayOfWeek == DayOfWeek.Sunday
                        ? dt.AddDays(-13)
                        : dt.AddDays(-6 - (int)dt.DayOfWeek);
                case SubtotalLevel.Month:
                    return new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1);
                case SubtotalLevel.Year:
                    return new DateTime(dt.Year - 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private DateTime? Next(DateTime? date)
        {
            if (!date.HasValue)
                return null;

            var dt = date.Value;
            switch (m_Par.AggrInterval)
            {
                case SubtotalLevel.Day:
                    return dt.AddDays(1);
                case SubtotalLevel.Week:
                    return dt.AddDays(7);
                case SubtotalLevel.Month:
                    return dt.AddMonths(1);
                case SubtotalLevel.Year:
                    return dt.AddYears(1);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private double BuildEquiPhase(IEnumerable<Balance> raw) => m_Par.EquivalentDate.HasValue
            ? raw.Sum(
                b => b.Fund
                    * m_Exchange.From(m_Par.EquivalentDate.Value, b.Currency)
                    * m_Exchange.To(m_Par.EquivalentDate.Value, m_Par.EquivalentCurrency))
            : raw.Sum(b => b.Fund);
    }
}
