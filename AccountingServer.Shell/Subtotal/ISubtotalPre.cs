using System.Collections.Generic;
using System.Text;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Subtotal
{
    /// <summary>
    ///     ������ܽ��������
    /// </summary>
    internal interface ISubtotalPre
    {
        /// <summary>
        ///     ִ�з������
        /// </summary>
        /// <param name="res">������ܽ��</param>
        /// <returns>������ܽ��</returns>
        string PresentSubtotal(IEnumerable<Balance> res);

        /// <summary>
        ///     ������ܲ���
        /// </summary>
        ISubtotal SubtotalArgs { set; }
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
