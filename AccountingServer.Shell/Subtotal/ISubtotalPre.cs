using System.Collections.Generic;
using System.Text;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Subtotal
{
    /// <summary>
    ///     分类汇总结果处理器
    /// </summary>
    internal interface ISubtotalPre
    {
        /// <summary>
        ///     执行分类汇总
        /// </summary>
        /// <param name="res">分类汇总结果</param>
        /// <returns>分类汇总结果</returns>
        string PresentSubtotal(IEnumerable<Balance> res);

        /// <summary>
        ///     分类汇总参数
        /// </summary>
        ISubtotal SubtotalArgs { set; }
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
