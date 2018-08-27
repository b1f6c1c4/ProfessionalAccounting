using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Subtotal
{
    /// <summary>
    ///     分类汇总结果处理器
    /// </summary>
    internal abstract class StringSubtotalVisitor : ISubtotalVisitor<Nothing>, ISubtotalStringify
    {
        protected int Depth;

        protected GatheringType Ga;
        protected string Cu;

        private ISubtotal m_Par;
        protected StringBuilder Sb;

        /// <inheritdoc />
        public string PresentSubtotal(ISubtotalResult raw, ISubtotal par)
        {
            m_Par = par;
            Ga = par.GatherType;
            Cu = par.EquivalentCurrency;
            Sb = new StringBuilder();
            Depth = 0;
            raw?.Accept(this);
            return Sb.ToString();
        }

        public abstract Nothing Visit(ISubtotalRoot sub);
        public abstract Nothing Visit(ISubtotalDate sub);
        public abstract Nothing Visit(ISubtotalCurrency sub);
        public abstract Nothing Visit(ISubtotalTitle sub);
        public abstract Nothing Visit(ISubtotalSubTitle sub);
        public abstract Nothing Visit(ISubtotalContent sub);
        public abstract Nothing Visit(ISubtotalRemark sub);

        protected void VisitChildren(ISubtotalResult sub)
        {
            if (sub.Items == null)
                return;

            var items = Depth < m_Par.Levels.Count ? ResolveItems(sub) : sub.Items;

            Depth++;
            foreach (var item in items)
                item.Accept(this);

            Depth--;
        }

        private IEnumerable<ISubtotalResult> ResolveItems(ISubtotalResult sub)
        {
            switch (m_Par.Levels[Depth])
            {
                case SubtotalLevel.Title:
                    return sub.Items.Cast<ISubtotalTitle>().OrderBy(s => s.Title);
                case SubtotalLevel.SubTitle:
                    return sub.Items.Cast<ISubtotalSubTitle>().OrderBy(s => s.SubTitle);
                case SubtotalLevel.Content:
                    return sub.Items.Cast<ISubtotalContent>().OrderBy(s => s.Content);
                case SubtotalLevel.Remark:
                    return sub.Items.Cast<ISubtotalRemark>().OrderBy(s => s.Remark);
                case SubtotalLevel.Currency:
                    return sub.Items.Cast<ISubtotalCurrency>()
                        .OrderBy(s => s.Currency == BaseCurrency.Now ? null : s.Currency);
                case SubtotalLevel.Day:
                case SubtotalLevel.Week:
                case SubtotalLevel.Month:
                case SubtotalLevel.Year:
                    return sub.Items.Cast<ISubtotalDate>().OrderBy(s => s.Date);
                case SubtotalLevel.WeakWeek:
                case SubtotalLevel.WeakMonth:
                case SubtotalLevel.WeakYear:
                    Depth++;
                    var items = Depth < m_Par.Levels.Count ? ResolveItems(sub) : sub.Items;
                    Depth--;
                    return items;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
