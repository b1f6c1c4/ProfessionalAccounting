using System;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.Entities;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     扩展的字符串匹配
    /// </summary>
    public static class ParseHelper
    {
        /// <summary>
        ///     匹配带括号和连续字符串
        /// </summary>
        /// <param name="facade">占位符</param>
        /// <param name="expr">表达式</param>
        /// <param name="allow">允许括号</param>
        /// <param name="predicate">是否有效</param>
        /// <returns>字符串</returns>
        public static string Token(this FacadeBase facade, ref string expr, bool allow = true, Func<string, bool> predicate = null)
        {
            expr = expr.TrimStart();
            if (expr.Length == 0)
                return null;

            if (allow)
                if (expr[0] == '\'' ||
                    expr[0] == '"')
                    return facade.Quoted(ref expr);

            var id = 1;
            while (id < expr.Length)
            {
                if (char.IsWhiteSpace(expr[id]))
                    break;

                id++;
            }

            var t = expr.Substring(0, id);
            if (!predicate?.Invoke(t) == true)
                return null;

            expr = expr.Substring(id);
            return t;
        }

        /// <summary>
        ///     匹配可选的数
        /// </summary>
        /// <param name="facade">占位符</param>
        /// <param name="expr">表达式</param>
        /// <returns>数</returns>
        public static double? Double(this FacadeBase facade, ref string expr)
        {
            var d = double.NaN;
            if (facade.Token(ref expr, false, t => double.TryParse(t, out d)) != null)
                return d;

            return null;
        }

        /// <summary>
        ///     匹配数
        /// </summary>
        /// <param name="facade">占位符</param>
        /// <param name="expr">表达式</param>
        /// <returns>数</returns>
        public static double DoubleF(this FacadeBase facade, ref string expr)
            => double.Parse(facade.Token(ref expr, false));

        /// <summary>
        ///     匹配可选的非零长度字符串
        /// </summary>
        /// <param name="facade">占位符</param>
        /// <param name="expr">表达式</param>
        /// <param name="opt">字符串</param>
        /// <returns>是否匹配</returns>
        // ReSharper disable once UnusedParameter.Global
        public static bool Optional(this FacadeBase facade, ref string expr, string opt)
        {
            expr = expr.TrimStart();
            if (!expr.StartsWith(opt, StringComparison.Ordinal))
                return false;

            expr = expr.Substring(opt.Length);
            return true;
        }

        /// <summary>
        ///     匹配带引号的字符串
        /// </summary>
        /// <param name="facade">占位符</param>
        /// <param name="expr">表达式</param>
        /// <param name="c">引号（若为空表示任意）</param>
        // ReSharper disable once UnusedParameter.Global
        public static string Quoted(this FacadeBase facade, ref string expr, char? c = null)
        {
            expr = expr.TrimStart();
            if (expr.Length < 1)
                return null;

            var ch = expr[0];
            if (c != null &&
                ch != c)
                return null;

            var id = 0;
            while (true)
            {
                id = expr.IndexOf(ch, id + 1);
                if (id < 0)
                    throw new ArgumentException("语法错误", nameof(expr));

                if (id == expr.Length - 1)
                    break;
                if (expr[id + 1] != ch)
                    break;
            }

            var s = expr.Substring(0, id + 1);
            expr = expr.Substring(id + 1);
            return s.Dequotation();
        }

        /// <summary>
        ///     匹配可选的冒号开始的记账凭证检索式
        /// </summary>
        /// <param name="facade">占位符</param>
        /// <param name="expr">表达式</param>
        /// <returns>记账凭证检索式</returns>
        public static IQueryCompunded<IVoucherQueryAtom> OptColVouchers(this FacadeBase facade, ref string expr)
            => Optional(facade, ref expr, ":") ? facade.VoucherQuery(ref expr) : null;
    }
}
