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

        private IReadOnlyList<SubtotalLevel> m_Levels;
        protected StringBuilder Sb;

        /// <inheritdoc />
        public string PresentSubtotal(ISubtotalResult raw, ISubtotal par)
        {
            m_Levels = par.ActualLevels();
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

            IEnumerable<ISubtotalResult> items;
            if (Depth < m_Levels.Count)
                switch (m_Levels[Depth])
                {
                    case SubtotalLevel.Title:
                        items = sub.Items.Cast<ISubtotalTitle>().OrderBy(s => s.Title);
                        break;
                    case SubtotalLevel.SubTitle:
                        items = sub.Items.Cast<ISubtotalSubTitle>().OrderBy(s => s.SubTitle);
                        break;
                    case SubtotalLevel.Content:
                        items = sub.Items.Cast<ISubtotalContent>().OrderBy(s => s.Content);
                        break;
                    case SubtotalLevel.Remark:
                        items = sub.Items.Cast<ISubtotalRemark>().OrderBy(s => s.Remark);
                        break;
                    case SubtotalLevel.Currency:
                        items = sub.Items.Cast<ISubtotalCurrency>()
                            .OrderBy(s => s.Currency == BaseCurrency.Now ? null : s.Currency);
                        break;
                    case SubtotalLevel.Day:
                    case SubtotalLevel.Week:
                    case SubtotalLevel.Month:
                    case SubtotalLevel.Year:
                        items = sub.Items.Cast<ISubtotalDate>().OrderBy(s => s.Date);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            else
                items = sub.Items;

            Depth++;
            foreach (var item in items)
                item.Accept(this);

            Depth--;
        }
    }
}
