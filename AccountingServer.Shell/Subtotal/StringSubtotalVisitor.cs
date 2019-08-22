using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;

namespace AccountingServer.Shell.Subtotal
{
    /// <summary>
    ///     分类汇总结果处理器
    /// </summary>
    internal abstract class StringSubtotalVisitor : ISubtotalVisitor<Nothing>, ISubtotalStringify
    {
        protected string Cu;
        protected int Depth;
        protected IEntitiesSerializer Serializer;

        protected GatheringType Ga;

        private ISubtotal m_Par;
        protected StringBuilder Sb;

        /// <inheritdoc />
        public string PresentSubtotal(ISubtotalResult raw, ISubtotal par, IEntitiesSerializer serializer)
        {
            m_Par = par;
            Ga = par.GatherType;
            Cu = par.EquivalentCurrency;
            Serializer = serializer;
            Sb = new StringBuilder();
            Depth = 0;
            Pre();
            raw?.Accept(this);
            Post();
            return Sb.ToString();
        }

        protected virtual void Pre() { }
        protected virtual void Post() { }
        public abstract Nothing Visit(ISubtotalRoot sub);
        public abstract Nothing Visit(ISubtotalDate sub);
        public abstract Nothing Visit(ISubtotalUser sub);
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
            if (Depth < m_Par.Levels.Count)
            {
                var comparer = CultureInfo.GetCultureInfo("zh-CN").CompareInfo.GetStringComparer(CompareOptions.StringSort);
                switch (m_Par.Levels[Depth])
                {
                    case SubtotalLevel.Title:
                        items = sub.Items.Cast<ISubtotalTitle>().OrderBy(s => s.Title);
                        break;
                    case SubtotalLevel.SubTitle:
                        items = sub.Items.Cast<ISubtotalSubTitle>().OrderBy(s => s.SubTitle);
                        break;
                    case SubtotalLevel.Content:
                        items = sub.Items.Cast<ISubtotalContent>().OrderBy(s => s.Content, comparer);
                        break;
                    case SubtotalLevel.Remark:
                        items = sub.Items.Cast<ISubtotalRemark>().OrderBy(s => s.Remark, comparer);
                        break;
                    case SubtotalLevel.User:
                        items = sub.Items.Cast<ISubtotalUser>().OrderBy(s => s.User, comparer);
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
