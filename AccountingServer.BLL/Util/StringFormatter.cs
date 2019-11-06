using System.Text.RegularExpressions;

namespace AccountingServer.BLL.Util
{
    /// <summary>
    ///     字符串格式化
    /// </summary>
    public static class StringFormatter
    {
        private static readonly Regex Reg = new Regex(@"[\uFF00-\uFFFF\u4e00-\u9fa5￥]");

        /// <summary>
        ///     左对齐补至指定长度
        /// </summary>
        /// <param name="s">待格式化的字符串</param>
        /// <param name="length">长度</param>
        /// <param name="chr">补码</param>
        /// <returns>格式化的字符串</returns>
        public static string CPadRight(this string s, int length, char chr = ' ')
        {
            if (s == null)
                s = string.Empty;

            if (length - s.Length - Reg.Matches(s).Count < 0)
                length = s.Length + Reg.Matches(s).Count;

            return s + new string(chr, length - s.Length - Reg.Matches(s).Count);
        }


        /// <summary>
        ///     右对齐补至指定长度
        /// </summary>
        /// <param name="s">待格式化的字符串</param>
        /// <param name="length">长度</param>
        /// <param name="chr">补码</param>
        /// <returns>格式化的字符串</returns>
        public static string CPadLeft(this string s, int length, char chr = ' ')
        {
            if (s == null)
                s = string.Empty;

            if (length - s.Length - Reg.Matches(s).Count < 0)
                length = s.Length + Reg.Matches(s).Count;

            return new string(chr, length - s.Length - Reg.Matches(s).Count) + s;
        }
    }
}
