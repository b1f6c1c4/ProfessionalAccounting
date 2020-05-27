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
    internal abstract class StringSubtotalVisitor
        : ISubtotalVisitor<Nothing>, ISubtotalItemsVisitor<IEnumerable<ISubtotalResult>>, ISubtotalStringify
    {
        protected string Cu;
        protected int Depth;

        protected GatheringType Ga;

        private ISubtotal m_Par;
        protected StringBuilder Sb;
        protected IEntitiesSerializer Serializer;

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

        public abstract Nothing Visit<TC>(ISubtotalRoot<TC> sub) where TC : ISubtotalResult;
        public abstract Nothing Visit<TC>(ISubtotalDate<TC> sub) where TC : ISubtotalResult;
        public abstract Nothing Visit<TC>(ISubtotalUser<TC> sub) where TC : ISubtotalResult;
        public abstract Nothing Visit<TC>(ISubtotalCurrency<TC> sub) where TC : ISubtotalResult;
        public abstract Nothing Visit<TC>(ISubtotalTitle<TC> sub) where TC : ISubtotalResult;
        public abstract Nothing Visit<TC>(ISubtotalSubTitle<TC> sub) where TC : ISubtotalResult;
        public abstract Nothing Visit<TC>(ISubtotalContent<TC> sub) where TC : ISubtotalResult;
        public abstract Nothing Visit<TC>(ISubtotalRemark<TC> sub) where TC : ISubtotalResult;

        protected virtual void Pre() { }
        protected virtual void Post() { }

        protected void VisitChildren<TC>(ISubtotalResult<TC> sub) where TC : ISubtotalResult
        {
            if (sub.Items == null)
                return;

            Depth++;
            var items = Depth < m_Par.Levels.Count ? sub.Items.Accept(this) : sub.Items.Cast<ISubtotalResult>();
            foreach (var item in items)
                item.Accept(this);

            Depth--;
        }

        private static IComparer<string> Comparer => CultureInfo.GetCultureInfo("zh-CN").CompareInfo
            .GetStringComparer(CompareOptions.StringSort);

        public IEnumerable<ISubtotalResult> Visit<TC>(IEnumerable<ISubtotalDate<TC>> sub) where TC : ISubtotalResult
            => sub.OrderBy(s => s.Date);

        public IEnumerable<ISubtotalResult> Visit<TC>(IEnumerable<ISubtotalUser<TC>> sub) where TC : ISubtotalResult
            => sub.OrderBy(s => s.User == ClientUser.Name ? null : s.User);

        public IEnumerable<ISubtotalResult> Visit<TC>(IEnumerable<ISubtotalCurrency<TC>> sub) where TC : ISubtotalResult
            => sub.OrderBy(s => s.Currency == BaseCurrency.Now ? null : s.Currency);

        public IEnumerable<ISubtotalResult> Visit<TC>(IEnumerable<ISubtotalTitle<TC>> sub) where TC : ISubtotalResult
            => sub.OrderBy(s => s.Title);

        public IEnumerable<ISubtotalResult> Visit<TC>(IEnumerable<ISubtotalSubTitle<TC>> sub) where TC : ISubtotalResult
            => sub.OrderBy(s => s.SubTitle);

        public IEnumerable<ISubtotalResult> Visit<TC>(IEnumerable<ISubtotalContent<TC>> sub) where TC : ISubtotalResult
            => sub.OrderBy(s => s.Content, Comparer);

        public IEnumerable<ISubtotalResult> Visit<TC>(IEnumerable<ISubtotalRemark<TC>> sub) where TC : ISubtotalResult
            => sub.OrderBy(s => s.Remark, Comparer);
    }
}
