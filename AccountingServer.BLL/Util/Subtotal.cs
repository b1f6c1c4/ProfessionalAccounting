using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL.Util
{
    internal abstract class SubtotalResult : ISubtotalResult
    {
        public double Fund { get; set; }

        public IEnumerable<ISubtotalResult> Items => TheItems?.AsReadOnly();

        public List<ISubtotalResult> TheItems { get; set; }

        public abstract void Accept(ISubtotalVisitor visitor);
        public abstract T Accept<T>(ISubtotalVisitor<T> visitor);
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

    internal class SubtotalCurrency : SubtotalResult, ISubtotalCurrency
    {
        public SubtotalCurrency(string currency) => Currency = currency;
        public string Currency { get; }

        public override void Accept(ISubtotalVisitor visitor) => visitor.Visit(this);
        public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
    }

    internal class SubtotalTitle : SubtotalResult, ISubtotalTitle
    {
        public SubtotalTitle(int? title) => Title = title;
        public int? Title { get; }

        public override void Accept(ISubtotalVisitor visitor) => visitor.Visit(this);
        public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
    }

    internal class SubtotalSubTitle : SubtotalResult, ISubtotalSubTitle
    {
        public SubtotalSubTitle(int? subTitle) => SubTitle = subTitle;
        public int? SubTitle { get; }

        public override void Accept(ISubtotalVisitor visitor) => visitor.Visit(this);
        public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
    }

    internal class SubtotalContent : SubtotalResult, ISubtotalContent
    {
        public SubtotalContent(string content) => Content = content;
        public string Content { get; }

        public override void Accept(ISubtotalVisitor visitor) => visitor.Visit(this);
        public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
    }

    internal class SubtotalRemark : SubtotalResult, ISubtotalRemark
    {
        public SubtotalRemark(string remark) => Remark = remark;
        public string Remark { get; }

        public override void Accept(ISubtotalVisitor visitor) => visitor.Visit(this);
        public override T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
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
            if (m_Par.AggrType != AggregationType.ChangedDay &&
                m_Par.GatherType == GatheringType.NonZero)
                raw = raw.Where(b => !b.Fund.IsZero());

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
            sub.Fund = sub.TheItems.Sum(isr => isr.Fund);
            return sub;
        }

        private void BuildChildren(SubtotalResult sub, IEnumerable<Balance> raw, SubtotalLevel level)
        {
            switch (level)
            {
                case SubtotalLevel.Title:
                    sub.TheItems = raw.GroupBy(b => b.Title)
                        .Select(g => Build(new SubtotalTitle(g.Key), g)).ToList();
                    break;
                case SubtotalLevel.SubTitle:
                    sub.TheItems = raw.GroupBy(b => b.SubTitle)
                        .Select(g => Build(new SubtotalSubTitle(g.Key), g)).ToList();
                    break;
                case SubtotalLevel.Content:
                    sub.TheItems = raw.GroupBy(b => b.Content)
                        .Select(g => Build(new SubtotalContent(g.Key), g)).ToList();
                    break;
                case SubtotalLevel.Remark:
                    sub.TheItems = raw.GroupBy(b => b.Remark)
                        .Select(g => Build(new SubtotalRemark(g.Key), g)).ToList();
                    break;
                case SubtotalLevel.Currency:
                    sub.TheItems = raw.GroupBy(b => b.Currency)
                        .Select(g => Build(new SubtotalCurrency(g.Key), g)).ToList();
                    break;
                case SubtotalLevel.Day:
                    sub.TheItems = raw.GroupBy(b => b.Date)
                        .Select(g => Build(new SubtotalDate(g.Key, SubtotalLevel.Day), g)).ToList();
                    break;
                case SubtotalLevel.Week:
                    sub.TheItems = raw.GroupBy(b => b.Date)
                        .Select(g => Build(new SubtotalDate(g.Key, SubtotalLevel.Week), g)).ToList();
                    break;
                case SubtotalLevel.Month:
                    sub.TheItems = raw.GroupBy(b => b.Date)
                        .Select(g => Build(new SubtotalDate(g.Key, SubtotalLevel.Month), g)).ToList();
                    break;
                case SubtotalLevel.Year:
                    sub.TheItems = raw.GroupBy(b => b.Date)
                        .Select(g => Build(new SubtotalDate(g.Key, SubtotalLevel.Year), g)).ToList();
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
                    return sub;
                case AggregationType.ChangedDay:
                    sub.TheItems = raw.GroupBy(b => b.Date)
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
                    foreach (var grp in raw.GroupBy(b => b.Date))
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
