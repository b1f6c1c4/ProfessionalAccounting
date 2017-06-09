using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL
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

    internal class SubtotalBuilder
    {
        private readonly ISubtotal m_Par;
        private int m_Depth;

        public ISubtotalResult Result { get; }

        public SubtotalBuilder(IEnumerable<Balance> raw, ISubtotal par)
        {
            if (par.AggrType != AggregationType.ChangedDay &&
                par.GatherType == GatheringType.NonZero)
                raw = raw.Where(b => !b.Fund.IsZero());

            m_Par = par;

            m_Depth = 0;
            Result = Build(new SubtotalRoot(), raw);
        }

        private ISubtotalResult Build(SubtotalResult sub, IEnumerable<Balance> raw)
        {
            if (m_Par.Levels.Count == m_Depth)
            {
                sub.Fund = raw.Sum(b => b.Fund);
                return sub;
            }

            var level = m_Par.Levels[m_Depth];

            m_Depth++;
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

            m_Depth--;
            sub.Fund = sub.TheItems.Sum(isr => isr.Fund);
            return sub;
        }
    }
}
