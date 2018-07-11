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
    internal abstract class StringSubtotalVisitor : ISubtotalVisitor
    {
        protected StringBuilder Sb;

        protected GatheringType Ga;

        protected int Depth;

        private ISubtotal m_Par;

        /// <summary>
        ///     执行分类汇总
        /// </summary>
        /// <param name="raw">分类汇总结果</param>
        /// <param name="par">参数</param>
        /// <returns>分类汇总结果</returns>
        public string PresentSubtotal(ISubtotalResult raw, ISubtotal par)
        {
            m_Par = par;
            Ga = par.GatherType;
            Sb = new StringBuilder();
            Depth = 0;
            raw?.Accept(this);
            return Sb.ToString();
        }

        protected void VisitChildren(ISubtotalResult sub)
        {
            if (sub.Items == null)
                return;

            IEnumerable<ISubtotalResult> items;
            if (Depth < m_Par.Levels.Count)
                switch (m_Par.Levels[Depth])
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

        public abstract void Visit(ISubtotalRoot sub);
        public abstract void Visit(ISubtotalDate sub);
        public abstract void Visit(ISubtotalCurrency sub);
        public abstract void Visit(ISubtotalTitle sub);
        public abstract void Visit(ISubtotalSubTitle sub);
        public abstract void Visit(ISubtotalContent sub);
        public abstract void Visit(ISubtotalRemark sub);
    }
}
