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

        protected GatheringType Ga;

        /// <summary>
        ///     执行分类汇总
        /// </summary>
        /// <param name="raw">分类汇总结果</param>
        /// <param name="ga">汇总类型</param>
        /// <returns>分类汇总结果</returns>
        public string PresentSubtotal(ISubtotalResult raw, GatheringType ga)
        {
            Ga = ga;
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
}
