using System;
using System.Text.RegularExpressions;

namespace AccountingServer.BLL
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
                s = String.Empty;

            if (length - s.Length - Reg.Matches(s).Count < 0)
                length = s.Length + Reg.Matches(s).Count;

            return s + new String(chr, length - s.Length - Reg.Matches(s).Count);
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
                s = String.Empty;

            if (length - s.Length - Reg.Matches(s).Count < 0)
                length = s.Length + Reg.Matches(s).Count;

            return new String(chr, length - s.Length - Reg.Matches(s).Count) + s;
        }

        /// <summary>
        ///     使用分隔符连接字符串
        /// </summary>
        /// <param name="path">原字符串</param>
        /// <param name="token">要连接上的字符串</param>
        /// <param name="interval">分隔符</param>
        /// <returns>新字符串</returns>
        public static string Merge(this string path, string token, string interval = "-")
        {
            if (path.Length == 0)
                return token;

            return path + interval + token;
        }
    }
}
