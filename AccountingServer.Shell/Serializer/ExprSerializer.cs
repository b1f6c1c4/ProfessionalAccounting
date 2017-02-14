using System;
using System.Collections.Generic;
using System.Text;
using AccountingServer.BLL.Parsing;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Serializer
{
    /// <summary>
    ///     实体表达式
    /// </summary>
    internal class ExprSerializer : IEntitySerializer
    {
        private const string TheToken = "new Voucher {";

        /// <inheritdoc />
        public string PresentVoucher(Voucher voucher)
        {
            var sb = new StringBuilder();
            sb.Append($"@{TheToken}");
            if (voucher == null)
            {
                sb.AppendLine();
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine(voucher.ID?.Quotation('^'));
                sb.AppendLine(voucher.Date.AsDate());
                if (voucher.Remark != null)
                    sb.AppendLine(voucher.Remark.Quotation('%'));
                if (voucher.Type.HasValue &&
                    voucher.Type != VoucherType.Ordinary)
                    sb.AppendLine(voucher.Type.ToString());

                foreach (var d in voucher.Details)
                {
                    var t = TitleManager.GetTitleName(d.Title);
                    if (d.SubTitle.HasValue)
                    {
                        var s = TitleManager.GetTitleName(d.Title, d.SubTitle);
                        sb.AppendLine($"// {t}-{s}");
                    }
                    else
                        sb.AppendLine($"// {t}");
                    if (d.Currency != null &&
                        d.Currency != VoucherDetail.BaseCurrency)
                        sb.Append($"@{d.Currency} ");
                    sb.AppendLine(
                        $"T{d.Title.AsTitle()}{d.SubTitle.AsSubTitle()} {d.Content?.Quotation('\'')} {d.Remark?.Quotation('\"')} {d.Fund}");
                }
            }

            sb.AppendLine("}@");
            return sb.ToString();
        }

        /// <inheritdoc />
        public Voucher ParseVoucher(string expr)
        {
            if (!expr.StartsWith(TheToken, StringComparison.Ordinal))
                throw new FormatException("格式错误");

            expr = expr.Substring(TheToken.Length);
            var v = GetVoucher(ref expr);
            Parsing.TrimStartComment(ref expr);
            if (Parsing.Token(ref expr, false) != "}")
                throw new FormatException("格式错误");

            Parsing.Eof(expr);
            return v;
        }

        /// <summary>
        ///     解析记账凭证表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>记账凭证</returns>
        private Voucher GetVoucher(ref string expr)
        {
            Parsing.TrimStartComment(ref expr);
            var id = Parsing.Quoted(ref expr, '^');
            Parsing.TrimStartComment(ref expr);
            var date = Parsing.UniqueTime(ref expr) ?? DateTime.Today; // TODO
            Parsing.TrimStartComment(ref expr);
            var remark = Parsing.Quoted(ref expr, '%');
            Parsing.TrimStartComment(ref expr);
            var typeT = VoucherType.Ordinary;
            var type = Parsing.Token(ref expr, false, t => TryParse(t, out typeT)) != null ? (VoucherType?)typeT : null;
            Parsing.TrimStartComment(ref expr);

            var lst = new List<VoucherDetail>();
            VoucherDetail d;
            while ((d = GetVoucherDetail(ref expr)) != null)
                lst.Add(d);

            return new Voucher
                {
                    ID = id,
                    Remark = remark,
                    Type = type,
                    Date = date,
                    Details = lst
                };
        }

        /// <summary>
        ///     解析记账凭证类别表达式
        /// </summary>
        /// <param name="s">表达式</param>
        /// <param name="typeT">记账凭证类别</param>
        /// <returns>是否解析成功</returns>
        private static bool TryParse(string s, out VoucherType typeT)
        {
            switch (s)
            {
                case "Ordinary":
                    typeT = VoucherType.Ordinary;
                    return true;
                case "Carry":
                    typeT = VoucherType.Carry;
                    return true;
                case "AnnualCarry":
                    typeT = VoucherType.AnnualCarry;
                    return true;
                case "Depreciation":
                    typeT = VoucherType.Depreciation;
                    return true;
                case "Devalue":
                    typeT = VoucherType.Devalue;
                    return true;
                case "Amortization":
                    typeT = VoucherType.Amortization;
                    return true;
                case "Uncertain":
                    typeT = VoucherType.Uncertain;
                    return true;
                default:
                    typeT = VoucherType.General;
                    return false;
            }
        }

        /// <summary>
        ///     解析细目表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>细目</returns>
        protected virtual VoucherDetail GetVoucherDetail(ref string expr)
        {
            Parsing.TrimStartComment(ref expr);
            var currency = Parsing.Token(ref expr, false, t => t.StartsWith("@", StringComparison.Ordinal))?.Substring(1).ToUpperInvariant();
            Parsing.TrimStartComment(ref expr);
            var title = Parsing.Title(ref expr);
            if (title == null)
                return null;

            var lst = new List<string>();
            double? fund;

            while (true)
            {
                Parsing.TrimStartComment(ref expr);
                if ((fund = Parsing.Double(ref expr)) != null)
                    break;

                Parsing.TrimStartComment(ref expr);
                if (Parsing.Optional(ref expr, "null"))
                    break;

                if (lst.Count > 2)
                    throw new ArgumentException("语法错误", nameof(expr));

                Parsing.TrimStartComment(ref expr);
                lst.Add(Parsing.Token(ref expr));
            }

            var content = lst.Count >= 1 ? lst[0] : null;
            var remark = lst.Count >= 2 ? lst[1] : null;

            return new VoucherDetail
                {
                    Currency = currency,
                    Title = title.Title,
                    SubTitle = title.SubTitle,
                    Content = string.IsNullOrEmpty(content) ? null : content,
                    Fund = fund,
                    Remark = string.IsNullOrEmpty(remark) ? null : remark
                };
        }

        public string PresentAsset(Asset asset) { throw new NotImplementedException(); }
        public Asset ParseAsset(string str) { throw new NotImplementedException(); }
        public string PresentAmort(Amortization amort) { throw new NotImplementedException(); }
        public Amortization ParseAmort(string str) { throw new NotImplementedException(); }
    }
}
