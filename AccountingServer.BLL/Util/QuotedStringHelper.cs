using System;
using Antlr4.Runtime.Tree;

namespace AccountingServer.BLL.Util
{
    /// <summary>
    ///     带引号的字符串辅助类
    /// </summary>
    public static class QuotedStringHelper
    {
        /// <summary>
        ///     给字符串添加引号
        /// </summary>
        /// <param name="unquoted">原字符串</param>
        /// <param name="chr">引号</param>
        /// <returns>字符串</returns>
        public static string Quotation(this string unquoted, char chr) =>
            $"{chr}{unquoted.Replace(new string(chr, 1), new string(chr, 2))}{chr}";

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
                throw new ArgumentException("格式错误", nameof(quoted));

            var chr = quoted[0];
            if (quoted[quoted.Length - 1] != chr)
                throw new ArgumentException("格式错误", nameof(quoted));

            var s = quoted.Substring(1, quoted.Length - 2);
            return s.Replace($"{chr}{chr}", $"{chr}");
        }
    }
}
