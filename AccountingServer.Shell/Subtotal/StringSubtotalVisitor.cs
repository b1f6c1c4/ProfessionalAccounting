using System.Collections.Generic;
using System.Text;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Subtotal
{
    /// <summary>
    ///     ������ܽ��������
    /// </summary>
    internal abstract class StringSubtotalVisitor : ISubtotalVisitor
    {
        protected StringBuilder Sb;

        protected ISubtotal Par;

        /// <summary>
        ///     ִ�з������
        /// </summary>
        /// <param name="raw">������ܽ��</param>
        /// <param name="par">������ܲ���</param>
        /// <returns>������ܽ��</returns>
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
        ///     �û������ӷǿ��ַ���
        /// </summary>
        /// <param name="strings">�ַ���</param>
        /// <returns>���ַ��������޷ǿ��ַ�����Ϊ��</returns>
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
