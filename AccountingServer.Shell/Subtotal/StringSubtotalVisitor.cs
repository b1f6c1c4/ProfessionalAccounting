using System.Collections.Generic;
using System.Text;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Subtotal
{
    /// <summary>
    ///     分类汇总结果处理器
    /// </summary>
    internal abstract class StringSubtotalVisitor : ISubtotalVisitor
    {
        protected StringBuilder Sb;

        protected ISubtotal Par;

        /// <summary>
        ///     执行分类汇总
        /// </summary>
        /// <param name="raw">分类汇总结果</param>
        /// <param name="par">分类汇总参数</param>
        /// <returns>分类汇总结果</returns>
        public string PresentSubtotal(ISubtotalResult raw, ISubtotal par)
        {
            Par = par;
            Sb = new StringBuilder();
            raw?.Accept(this);
            return Sb.ToString();
        }

        public abstract void Visit(ISubtotalRoot sub);
        public abstract void Visit(ISubtotalDate sub);
        public abstract void Visit(ISubtotalCurrency sub);
        public abstract void Visit(ISubtotalTitle sub);
        public abstract void Visit(ISubtotalSubTitle sub);
        public abstract void Visit(ISubtotalContent sub);
        public abstract void Visit(ISubtotalRemark sub);
    }

    internal static class SubtotalPreHelper
    {
        /// <summary>
        ///     用换行连接非空字符串
        /// </summary>
        /// <param name="strings">字符串</param>
        /// <returns>新字符串，如无非空字符串则为空</returns>
        internal static string NotNullJoin(IEnumerable<string> strings)
        {
            var flag = false;

            var sb = new StringBuilder();
            foreach (var s in strings)
            {
                if (s == null)
                    continue;

                if (sb.Length > 0)
                    sb.AppendLine();
                sb.Append(s);
                flag = true;
            }

            return flag ? sb.ToString() : null;
        }
    }
}
