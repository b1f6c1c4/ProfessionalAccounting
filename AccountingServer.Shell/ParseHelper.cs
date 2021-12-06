using System;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.Entities;

namespace AccountingServer.Shell
{
    internal static class ParseHelper
    {

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
        ///     ƥ������ŵ��ַ���
        /// </summary>
        /// <param name="facade">ռλ��</param>
        /// <param name="expr">���ʽ</param>
        /// <param name="c">���ţ���Ϊ�ձ�ʾ���⣩</param>
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
                    throw new ArgumentException("�﷨����", nameof(expr));

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
        ///     ƥ���ѡ��ð�ſ�ʼ�ļ���ƾ֤����ʽ
        /// </summary>
        /// <param name="facade">ռλ��</param>
        /// <param name="expr">���ʽ</param>
        /// <returns>����ƾ֤����ʽ</returns>
        public static IQueryCompunded<IVoucherQueryAtom> OptColVouchers(this FacadeBase facade, ref string expr)
            => Optional(facade, ref expr, ":") ? facade.VoucherQuery(ref expr) : null;
    }
}
