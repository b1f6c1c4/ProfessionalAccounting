using System;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using Newtonsoft.Json.Linq;

namespace AccountingServer.Shell.Subtotal
{
    /// <summary>
    ///     分类汇总结果导出
    /// </summary>
    internal class JsonSubtotal : ISubtotalVisitor<JProperty>, ISubtotalStringify
    {
        private int m_Depth;

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
            m_Depth = 0;
            return (raw?.Accept(this)?.Value as JObject)?.ToString();
        }

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalRoot sub)
            => new JProperty("", VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalDate sub)
            => new JProperty(sub.Date.AsDate(sub.Level), VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalCurrency sub)
            => new JProperty(sub.Currency, VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalTitle sub)
            => new JProperty(sub.Title.AsTitle(), VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalSubTitle sub)
            => new JProperty(sub.SubTitle.AsSubTitle(), VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalContent sub)
            => new JProperty(sub.Content ?? "", VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalRemark sub)
            => new JProperty(sub.Remark ?? "", VisitChildren(sub));

        private JObject VisitChildren(ISubtotalResult sub)
        {
            var obj = new JObject(new JProperty("value", sub.Fund));
            if (sub.Items == null)
                return obj;

            string field;
            if (m_Depth < m_Par.Levels.Count)
                switch (m_Par.Levels[m_Depth])
                {
                    case SubtotalLevel.Title:
                        field = "title";
                        break;
                    case SubtotalLevel.SubTitle:
                        field = "subtitle";
                        break;
                    case SubtotalLevel.Content:
                        field = "content";
                        break;
                    case SubtotalLevel.Remark:
                        field = "remark";
                        break;
                    case SubtotalLevel.Currency:
                        field = "currency";
                        break;
                    case SubtotalLevel.Day:
                    case SubtotalLevel.Week:
                    case SubtotalLevel.Month:
                    case SubtotalLevel.Year:
                        field = "date";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            else
                field = "aggr";

            m_Depth++;
            obj[field] = new JObject(sub.Items.Select(it => it.Accept(this)));
            m_Depth--;

            return obj;
        }
    }
}
