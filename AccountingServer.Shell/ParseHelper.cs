using System;
using AccountingServer.BLL.Parsing;
using AccountingServer.Entities;

namespace AccountingServer.Shell
{
    internal static class ParseHelper
    {
        /// <summary>
        ///     匹配EOF
        /// </summary>
        /// <param name="facade">占位符</param>
        /// <param name="expr">表达式</param>
        public static void Eof(this FacadeBase facade, string expr)
        {
            if (!string.IsNullOrWhiteSpace(expr))
                throw new ArgumentException("语法错误", nameof(expr));
        }

        /// <summary>
        ///     匹配可选的非零长度字符串
        /// </summary>
        /// <param name="facade">占位符</param>
        /// <param name="expr">表达式</param>
        /// <param name="opt">字符串</param>
        /// <returns>是否匹配</returns>
        public static bool Optional(this FacadeBase facade, ref string expr, string opt)
        {
            expr = expr.TrimStart();
            if (!expr.StartsWith(opt, StringComparison.Ordinal))
                return false;
            expr = expr.Substring(opt.Length);
            return true;
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
