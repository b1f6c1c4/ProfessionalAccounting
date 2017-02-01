using System;
using System.Collections.Generic;
using AccountingServer.Entities;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     实体表达式
    /// </summary>
    public static class ExpressionHelper
    {
        /// <summary>
        ///     解析记账凭证表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>记账凭证</returns>
        public static Voucher GetVoucher(ref string expr)
        {
            var id = Parsing.Quoted(ref expr, '^');
            var date = Parsing.UniqueTime(ref expr) ?? DateTime.Today;
            var remark = Parsing.Quoted(ref expr, '%');
            var typeT = VoucherType.General;
            var type = Parsing.Token(ref expr, false, t => Enum.TryParse(t, out typeT)) != null ? (VoucherType?)typeT : null;
            var currency = Parsing.Token(ref expr, false, t => t.StartsWith("@", StringComparison.Ordinal));

            var lst = new List<VoucherDetail>();
            VoucherDetail d;
            while ((d = GetVoucherDetail(ref expr)) != null)
                lst.Add(d);

            return new Voucher
                {
                    ID = id,
                    Remark = remark,
                    Type = type,
                    Currency = currency,
                    Date = date,
                    Details = lst
                };
        }

        /// <summary>
        ///     解析细目表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>细目</returns>
        public static VoucherDetail GetVoucherDetail(ref string expr)
        {
            var title = Parsing.Title(ref expr);
            if (title == null)
                return null;

            var lst = new List<string>();
            double? fund;

            while (true)
            {
                if ((fund = Parsing.Double(ref expr)) != null)
                    break;

                if (Parsing.Optional(ref expr, "null"))
                    break;

                if (lst.Count > 2)
                    throw new ArgumentException("语法错误", nameof(expr));

                lst.Add(Parsing.Token(ref expr));
            }

            var content = lst.Count >= 1 ? lst[0] : null;
            var remark = lst.Count >= 2 ? lst[1] : null;

            return new VoucherDetail
                {
                    Title = title.Title,
                    SubTitle = title.SubTitle,
                    Content = content,
                    Fund = fund,
                    Remark = remark
                };
        }
    }
}
