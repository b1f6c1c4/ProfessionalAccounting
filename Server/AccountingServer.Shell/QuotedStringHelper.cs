using System;
using Antlr4.Runtime.Tree;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     带引号的字符串辅助类
    /// </summary>
    internal static class QuotedStringHelper
    {
        /// <summary>
        ///     给标识符解除引号
        /// </summary>
        /// <param name="quoted">标识符</param>
        /// <returns>原字符串</returns>
        public static string Dequotation(this ITerminalNode quoted)
        {
            return quoted == null ? null : quoted.GetText().Dequotation();
        }

        /// <summary>
        ///     给字符串解除引号
        /// </summary>
        /// <param name="quoted">字符串</param>
        /// <returns>原字符串</returns>
        public static string Dequotation(this string quoted)
        {
            if (quoted == null)
                return null;
            if (quoted.Length == 0)
                return quoted;
            if (quoted.Length == 1)
                throw new ArgumentException("格式错误", "quoted");

            var chr = quoted[0];
            if (quoted[quoted.Length - 1] != chr)
                throw new ArgumentException("格式错误", "quoted");

            var s = quoted.Substring(1, quoted.Length - 2);
            return s.Replace(String.Format("{0}{0}", chr), String.Format("{0}", chr));
        }
    }
}
