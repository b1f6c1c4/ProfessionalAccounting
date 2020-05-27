using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL.Util
{
    internal abstract class SubtotalResult<TC> : ISubtotalResult<TC> where TC : ISubtotalResult
    {
        public ISubtotalResults<TC> Items { get; set; }
        public double Fund { get; set; }
        public abstract T Accept<T>(ISubtotalVisitor<T> visitor);
    }

    internal interface IResultFactory<T, TC> where T : IEnumerable<Balance> where TC : ISubtotalResult
    {
        IEnumerable<T> Group(IEnumerable<Balance> bs);
        SubtotalResult<TC> Create(T grp);
    }

    internal abstract class ResultFactory<T, TC> : IResultFactory<IGrouping<T, Balance>, TC>
        where TC : ISubtotalResult
    {
        public IEnumerable<IGrouping<T, Balance>> Group(IEnumerable<Balance> bs) => bs.GroupBy(Selector);
        protected abstract T Selector(Balance b);
        public abstract SubtotalResult<TC> Create(IGrouping<T, Balance> grp);
    }

    internal class SubtotalRoot<TC> : SubtotalResult<TC>, ISubtotalRoot<TC> where TC : ISubtotalResult
    {
        public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
    }

    internal class RootFactory<TC> : IResultFactory<IEnumerable<Balance>, TC> where TC : ISubtotalResult
    {
        public IEnumerable<IEnumerable<Balance>> Group(IEnumerable<Balance> bs) => new[] { bs };

        public SubtotalResult<TC> Create(IEnumerable<Balance> grp) => new SubtotalRoot<TC>();
    }

    internal class SubtotalDate<TC> : SubtotalResult<TC>, ISubtotalDate<TC> where TC : ISubtotalResult
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

    internal class DateFactory<TC> : ResultFactory<DateTime?, TC> where TC : ISubtotalResult
    {
        private readonly SubtotalLevel m_Level;
        public DateFactory(SubtotalLevel level) => m_Level = level;

        protected override DateTime? Selector(Balance b) => b.Date;
        public override SubtotalResult<TC> Create(IGrouping<DateTime?, Balance> grp) => new SubtotalDate<TC>(grp.Key, m_Level);
    }

    internal class SubtotalUser<TC> : SubtotalResult<TC>, ISubtotalUser<TC> where TC : ISubtotalResult
    {
        public SubtotalUser(string user) => User = user;
        public string User { get; }

        public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
    }

    internal class UserFactory<TC> : ResultFactory<string, TC> where TC : ISubtotalResult
    {
        protected override string Selector(Balance b) => b.User;
        public override SubtotalResult<TC> Create(IGrouping<string, Balance> grp) => new SubtotalUser<TC>(grp.Key);
    }

    internal class SubtotalCurrency<TC> : SubtotalResult<TC>, ISubtotalCurrency<TC> where TC : ISubtotalResult
    {
        public SubtotalCurrency(string currency) => Currency = currency;
        public string Currency { get; }

        public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
    }

    internal class CurrencyFactory<TC> : ResultFactory<string, TC> where TC : ISubtotalResult
    {
        protected override string Selector(Balance b) => b.Currency;
        public override SubtotalResult<TC> Create(IGrouping<string, Balance> grp) => new SubtotalCurrency<TC>(grp.Key);
    }

    internal class SubtotalTitle<TC> : SubtotalResult<TC>, ISubtotalTitle<TC> where TC : ISubtotalResult
    {
        public SubtotalTitle(int? title) => Title = title;
        public int? Title { get; }

        public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
    }

    internal class TitleFactory<TC> : ResultFactory<int?, TC> where TC : ISubtotalResult
    {
        protected override int? Selector(Balance b) => b.Title;
        public override SubtotalResult<TC> Create(IGrouping<int?, Balance> grp) => new SubtotalTitle<TC>(grp.Key);
    }

    internal class SubtotalSubTitle<TC> : SubtotalResult<TC>, ISubtotalSubTitle<TC> where TC : ISubtotalResult
    {
        public SubtotalSubTitle(int? subTitle) => SubTitle = subTitle;
        public int? SubTitle { get; }

        public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
    }

    internal class SubTitleFactory<TC> : ResultFactory<int?, TC> where TC : ISubtotalResult
    {
        protected override int? Selector(Balance b) => b.SubTitle;
        public override SubtotalResult<TC> Create(IGrouping<int?, Balance> grp) => new SubtotalSubTitle<TC>(grp.Key);
    }

    internal class SubtotalContent<TC> : SubtotalResult<TC>, ISubtotalContent<TC> where TC : ISubtotalResult
    {
        public SubtotalContent(string content) => Content = content;
        public string Content { get; }

        public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
    }

    internal class ContentFactory<TC> : ResultFactory<string, TC> where TC : ISubtotalResult
    {
        protected override string Selector(Balance b) => b.Content;
        public override SubtotalResult<TC> Create(IGrouping<string, Balance> grp) => new SubtotalContent<TC>(grp.Key);
    }

    internal class SubtotalRemark<TC> : SubtotalResult<TC>, ISubtotalRemark<TC> where TC : ISubtotalResult
    {
        public SubtotalRemark(string remark) => Remark = remark;
        public string Remark { get; }

        public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
    }

    internal class RemarkFactory<TC> : ResultFactory<string, TC> where TC : ISubtotalResult
    {
        protected override string Selector(Balance b) => b.Remark;
        public override SubtotalResult<TC> Create(IGrouping<string, Balance> grp) => new SubtotalRemark<TC>(grp.Key);
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

        private List<ISubtotalResult> Group<TC>(IEnumerable<Balance> raw) where TC : ISubtotalResult
        {
            List<ISubtotalResult> Invoke<T>(IResultFactory<T, TC> f) where T : IEnumerable<Balance>
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
                        return Invoke(new RootFactory<>());
                    case SubtotalLevel.Title:
                        return Invoke(new TitleFactory());
                    case SubtotalLevel.SubTitle:
                        return Invoke(new SubTitleFactory());
                    case SubtotalLevel.Content:
                        return Invoke(new ContentFactory());
                    case SubtotalLevel.Remark:
                        return Invoke(new RemarkFactory());
                    case SubtotalLevel.User:
                        return Invoke(new UserFactory());
                    case SubtotalLevel.Currency:
                        return Invoke(new CurrencyFactory());
                    case SubtotalLevel.Day:
                    case SubtotalLevel.Week:
                    case SubtotalLevel.Month:
                    case SubtotalLevel.Year:
                        return Invoke(new DateFactory(level));
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
