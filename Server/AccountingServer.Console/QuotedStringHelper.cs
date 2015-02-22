using System;
using Antlr4.Runtime.Tree;

namespace AccountingServer.Console
{
    internal static class QuotedStringHelper
    {
        public static string Dequotation(this ITerminalNode quoted)
        {
            return quoted == null ? null : quoted.GetText().Dequotation();
        }

        private static string Dequotation(this string quoted)
        {
            if (quoted == null)
                return null;
            if (quoted.Length == 0)
                return quoted;
            if (quoted.Length == 1)
                throw new InvalidOperationException();

            var chr = quoted[0];
            if (quoted[quoted.Length - 1] != chr)
                throw new InvalidOperationException();

            var s = quoted.Substring(1, quoted.Length - 2);
            return s.Replace(String.Format("{0}{0}", chr), String.Format("{0}", chr));
        }
    }
}
