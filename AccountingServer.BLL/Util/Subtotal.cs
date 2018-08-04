using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL.Util
{
    internal abstract class SubtotalResult : ISubtotalResult
    {
        public List<ISubtotalResult> TheItems { get; set; }
        public double Fund { get; set; }

        public IEnumerable<ISubtotalResult> Items => TheItems?.AsReadOnly();

        public abstract void Accept(ISubtotalVisitor visitor);
        public abstract T Accept<T>(ISubtotalVisitor<T> visitor);
    }

    internal abstract class SubtotalResultFactory<T>
    {
        public abstract T Selector(Balance b);
        public abstract SubtotalResult Create(IGrouping<T, Balance> grp);
    }

    internal class SubtotalRoot : SubtotalResult, ISubtotalRoot
    {
        public override void Accept(ISubtotalVisitor visitor) => visitor.Visit(this);
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

        public override void Accept(ISubtotalVisitor visitor) => visitor.Visit(this);
        public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
    }

    internal class SubtotalDateFactory : SubtotalResultFactory<DateTime?>
    {
        private readonly SubtotalLevel m_Level;
        public SubtotalDateFactory(SubtotalLevel level) => m_Level = level;

        public override DateTime? Selector(Balance b) => b.Date;
        public override SubtotalResult Create(IGrouping<DateTime?, Balance> grp) => new SubtotalDate(grp.Key, m_Level);
    }

    internal class SubtotalCurrency : SubtotalResult, ISubtotalCurrency
    {
        public SubtotalCurrency(string currency) => Currency = currency;
        public string Currency { get; }

        public override void Accept(ISubtotalVisitor visitor) => visitor.Visit(this);
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

        public override void Accept(ISubtotalVisitor visitor) => visitor.Visit(this);
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

        public override void Accept(ISubtotalVisitor visitor) => visitor.Visit(this);
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

        public override void Accept(ISubtotalVisitor visitor) => visitor.Visit(this);
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

        public override void Accept(ISubtotalVisitor visitor) => visitor.Visit(this);
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
        private readonly ISubtotal m_Par;
        private int m_Depth;

        public SubtotalBuilder(ISubtotal par) => m_Par = par;

        /// <summary>
        ///     建造分类汇总结果
        /// </summary>
        /// <param name="raw">原始数据</param>
        /// <returns>分类汇总结果</returns>
        public ISubtotalResult Build(IEnumerable<Balance> raw)
        {
            m_Depth = 0;
            return Build(new SubtotalRoot(), raw);
        }

        private ISubtotalResult Build(SubtotalResult sub, IEnumerable<Balance> raw)
        {
            if (m_Par.Levels.Count == m_Depth)
                return BuildAggrPhase(sub, raw);

            var level = m_Par.Levels[m_Depth];
            m_Depth++;
            BuildChildren(sub, raw, level);
            m_Depth--;
            if (!sub.TheItems.Any() &&
                !(sub is ISubtotalRoot))
                return null;

            sub.Fund = sub.TheItems.Sum(isr => isr.Fund);
            return sub;
        }

        private void BuildChildren(SubtotalResult sub, IEnumerable<Balance> raw, SubtotalLevel level)
        {
            List<ISubtotalResult> Invoke<T>(SubtotalResultFactory<T> f) =>
                raw.GroupBy(f.Selector).Select(g => Build(f.Create(g), g)).Where(g => g != null).ToList();

            switch (level)
            {
                case SubtotalLevel.Title:
                    sub.TheItems = Invoke(new SubtotalTitleFactory());
                    break;
                case SubtotalLevel.SubTitle:
                    sub.TheItems = Invoke(new SubtotalSubTitleFactory());
                    break;
                case SubtotalLevel.Content:
                    sub.TheItems = Invoke(new SubtotalContentFactory());
                    break;
                case SubtotalLevel.Remark:
                    sub.TheItems = Invoke(new SubtotalRemarkFactory());
                    break;
                case SubtotalLevel.Currency:
                    sub.TheItems = Invoke(new SubtotalCurrencyFactory());
                    break;
                case SubtotalLevel.Day:
                case SubtotalLevel.Week:
                case SubtotalLevel.Month:
                case SubtotalLevel.Year:
                    sub.TheItems = Invoke(new SubtotalDateFactory(level));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ISubtotalResult BuildAggrPhase(SubtotalResult sub, IEnumerable<Balance> raw)
        {
            switch (m_Par.AggrType)
            {
                case AggregationType.None:
                    sub.Fund = BuildEquiPhase(raw);
                    if (m_Par.GatherType == GatheringType.NonZero &&
                        sub.Fund.IsZero() &&
                        !(sub is ISubtotalRoot))
                        return null;

                    return sub;
                case AggregationType.ChangedDay:
                    sub.TheItems = raw.GroupBy(b => b.Date).OrderBy(grp => grp.Key)
                        .Select(
                            grp => new SubtotalDate(grp.Key, SubtotalLevel.None)
                                {
                                    Fund = sub.Fund += BuildEquiPhase(grp)
                                }).Cast<ISubtotalResult>().ToList();
                    return sub;
                case AggregationType.EveryDay:
                    sub.TheItems = new List<ISubtotalResult>();
                    var last = m_Par.EveryDayRange.Range.StartDate?.AddDays(-1);

                    void Append(DateTime? curr, double oldFund, double fund)
                    {
                        if (last.HasValue &&
                            curr.HasValue)
                            while (last < curr)
                            {
                                last = last.Value.AddDays(1);
                                sub.TheItems.Add(
                                    new SubtotalDate(last, SubtotalLevel.None)
                                        {
                                            Fund = last == curr ? fund : oldFund
                                        });
                            }
                        else
                            sub.TheItems.Add(
                                new SubtotalDate(curr, SubtotalLevel.None)
                                    {
                                        Fund = fund
                                    });

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
                                new SubtotalDate(null, SubtotalLevel.None)
                                    {
                                        Fund = 0
                                    });
                        flag = false;

                        var tmp = sub.Fund;
                        sub.Fund += BuildEquiPhase(grp);

                        if (DateHelper.CompareDate(grp.Key, m_Par.EveryDayRange.Range.EndDate) <= 0)
                            tmp0 = sub.Fund;
                        if (grp.Key.Within(m_Par.EveryDayRange.Range))
                            Append(grp.Key, tmp, sub.Fund);
                    }

                    if (flag && forcedNull)
                        sub.TheItems.Add(
                            new SubtotalDate(null, SubtotalLevel.None)
                                {
                                    Fund = 0
                                });

                    if (m_Par.EveryDayRange.Range.EndDate.HasValue &&
                        DateHelper.CompareDate(last, m_Par.EveryDayRange.Range.EndDate) < 0)
                        Append(m_Par.EveryDayRange.Range.EndDate, tmp0, tmp0);
                    else if (m_Par.EveryDayRange.Range.StartDate.HasValue &&
                        last == m_Par.EveryDayRange.Range.StartDate.Value.AddDays(-1))
                        Append(m_Par.EveryDayRange.Range.StartDate, sub.Fund, sub.Fund);

                    return sub;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private double BuildEquiPhase(IEnumerable<Balance> raw) => m_Par.EquivalentDate.HasValue
            ? raw.Sum(b => b.Fund * ExchangeFactory.Instance.From(m_Par.EquivalentDate.Value, b.Currency))
            : raw.Sum(b => b.Fund);
    }
}
