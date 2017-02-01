using System;
using AccountingServer.Entities;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell
{
    public class ExpressionHelper
    {
        public static Voucher GetVoucher(string expr)
        {
            var id = Parsing.Quoted(ref expr, '^');
            var date = Parsing.UniqueTime(ref expr) ?? DateTime.Today;
            var remark = Parsing.Quoted(ref expr, '%');
            var typeT = VoucherType.General;
            var type = Parsing.Token(ref expr, t => Enum.TryParse(t, out typeT)) != null ? (VoucherType?)typeT : null;
            var currency = Parsing.Token(ref expr, t => t.StartsWith("@", StringComparison.Ordinal));

            return new Voucher
                {
                    ID = id,
                    Remark = remark,
                    Type = type,
                    Currency = currency,
                    Date = date
                };
        }

        public static VoucherDetail GetVoucherDetail(string expr)
        {
            var title = Parsing.Title(ref expr);
            var content = Parsing.Token(ref expr);
            var fund = Parsing.Double(ref expr);
            var remark = Parsing.Token(ref expr);

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
