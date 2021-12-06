using System;
using AccountingServer.BLL.Parsing;
using AccountingServer.Entities;

namespace AccountingServer.Shell
{
    internal static class ParseHelper
    {
        /// <summary>
        ///     ƥ��EOF
        /// </summary>
        /// <param name="facade">ռλ��</param>
        /// <param name="expr">���ʽ</param>
        public static void Eof(this FacadeBase facade, string expr)
        {
            if (!string.IsNullOrWhiteSpace(expr))
                throw new ArgumentException("�﷨����", nameof(expr));
        }

        /// <summary>
        ///     ƥ���ѡ�ķ��㳤���ַ���
        /// </summary>
        /// <param name="facade">ռλ��</param>
        /// <param name="expr">���ʽ</param>
        /// <param name="opt">�ַ���</param>
        /// <returns>�Ƿ�ƥ��</returns>
        public static bool Optional(this FacadeBase facade, ref string expr, string opt)
        {
            expr = expr.TrimStart();
            if (!expr.StartsWith(opt, StringComparison.Ordinal))
                return false;
            expr = expr.Substring(opt.Length);
            return true;
        }

        /// <summary>
        ///     ƥ���ѡ��ð�ſ�ʼ�ļ���ƾ֤����ʽ
        /// </summary>
        /// <param name="facade">ռλ��</param>
        /// <param name="expr">���ʽ</param>
        /// <returns>����ƾ֤����ʽ</returns>
        public static IQueryCompunded<IVoucherQueryAtom> OptColVouchers(this FacadeBase facade, ref string expr)
            => Optional(facade, ref expr, ":") ? facade.VoucherQuery(ref expr) : null;
    }
}
