using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using Newtonsoft.Json.Linq;

namespace AccountingServer.Shell.Subtotal
{
    /// <summary>
    ///     分类汇总结果导出
    /// </summary>
    internal class JsonSubtotal : ISubtotalVisitor<JProperty>, ISubtotalItemsVisitor<string>, ISubtotalStringify
    {
        private int m_Depth;

        private ISubtotal m_Par;

        /// <inheritdoc />
        public string PresentSubtotal(ISubtotalResult raw, ISubtotal par, IEntitiesSerializer serializer)
        {
            m_Par = par;
            m_Depth = 0;
            return (raw?.Accept(this)?.Value as JObject)?.ToString();
        }

        JProperty ISubtotalVisitor<JProperty>.Visit<TC>(ISubtotalRoot<TC> sub)
            => new JProperty("", VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit<TC>(ISubtotalDate<TC> sub)
            => new JProperty(sub.Date.AsDate(sub.Level), VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit<TC>(ISubtotalUser<TC> sub)
            => new JProperty(sub.User, VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit<TC>(ISubtotalCurrency<TC> sub)
            => new JProperty(sub.Currency, VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit<TC>(ISubtotalTitle<TC> sub)
            => new JProperty(sub.Title.AsTitle(), VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit<TC>(ISubtotalSubTitle<TC> sub)
            => new JProperty(sub.SubTitle.AsSubTitle(), VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit<TC>(ISubtotalContent<TC> sub)
            => new JProperty(sub.Content ?? "", VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit<TC>(ISubtotalRemark<TC> sub)
            => new JProperty(sub.Remark ?? "", VisitChildren(sub));

        private JObject VisitChildren<TC>(ISubtotalResult<TC> sub) where TC : ISubtotalResult
        {
            var obj = new JObject(new JProperty("value", sub.Fund));
            if (sub.Items == null)
                return obj;

            m_Depth++;
            var field = m_Depth < m_Par.Levels.Count ? sub.Items.Accept(this) : "aggr";
            obj[field] = new JObject(sub.Items.Select(it => it.Accept(this)));
            m_Depth--;

            return obj;
        }

        public string Visit<TC>(IEnumerable<ISubtotalDate<TC>> sub) where TC : ISubtotalResult => "date";

        public string Visit<TC>(IEnumerable<ISubtotalUser<TC>> sub) where TC : ISubtotalResult => "user";

        public string Visit<TC>(IEnumerable<ISubtotalCurrency<TC>> sub) where TC : ISubtotalResult => "currency";

        public string Visit<TC>(IEnumerable<ISubtotalTitle<TC>> sub) where TC : ISubtotalResult => "title";

        public string Visit<TC>(IEnumerable<ISubtotalSubTitle<TC>> sub) where TC : ISubtotalResult => "subtitle";

        public string Visit<TC>(IEnumerable<ISubtotalContent<TC>> sub) where TC : ISubtotalResult => "content";

        public string Visit<TC>(IEnumerable<ISubtotalRemark<TC>> sub) where TC : ISubtotalResult => "remark";
    }
}
