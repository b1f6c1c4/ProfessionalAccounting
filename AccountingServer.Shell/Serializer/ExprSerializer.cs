﻿using System;
using System.Collections.Generic;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell.Serializer
{
    /// <summary>
    ///     实体表达式
    /// </summary>
    public class ExprSerializer : IEntitySerializer
    {
        private const string TheToken = "new Voucher {";

        /// <inheritdoc />
        public string PresentVoucher(Voucher voucher)
        {
            var sb = new StringBuilder();
            sb.Append(TheToken);
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
                    sb.Append(PresentVoucherDetail(d));
            }

            sb.Append("}");
            return sb.ToString();
        }

        /// <inheritdoc />
        public string PresentVoucherDetail(VoucherDetail detail)
        {
            var sb = new StringBuilder();
            var t = TitleManager.GetTitleName(detail.Title);
            sb.AppendLine(
                detail.SubTitle.HasValue
                    ? $"// {t}-{TitleManager.GetTitleName(detail.Title, detail.SubTitle)}"
                    : $"// {t}");
            if (detail.Currency != BaseCurrency.Now)
                sb.Append($"@{detail.Currency} ");
            sb.Append($"T{detail.Title.AsTitle()}{detail.SubTitle.AsSubTitle()} ");
            if (detail.Content == null &&
                detail.Remark != null)
                sb.Append("''");
            else
                sb.Append(detail.Content?.Quotation('\''));
            sb.AppendLine($" {detail.Remark?.Quotation('\"')} {detail.Fund}");
            return sb.ToString();
        }

        /// <inheritdoc />
        public string PresentVoucherDetail(VoucherDetailR detail)
            => $"{detail.Voucher.Date.AsDate()} {PresentVoucherDetail((VoucherDetail)detail)}";

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

        /// <inheritdoc />
        public virtual VoucherDetail ParseVoucherDetail(string expr)
        {
            var res = ParseVoucherDetail(ref expr);
            Parsing.Eof(expr);
            return res;
        }

        public string PresentAsset(Asset asset) => throw new NotImplementedException();
        public Asset ParseAsset(string str) => throw new NotImplementedException();
        public string PresentAmort(Amortization amort) => throw new NotImplementedException();
        public Amortization ParseAmort(string str) => throw new NotImplementedException();

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
            DateTime? date = ClientDateTime.Today;
            try
            {
                date = ParsingF.UniqueTime(ref expr);
            }
            catch (Exception)
            {
                // ignore
            }

            Parsing.TrimStartComment(ref expr);
            var remark = Parsing.Quoted(ref expr, '%');
            Parsing.TrimStartComment(ref expr);
            var typeT = VoucherType.Ordinary;
            var type = Parsing.Token(ref expr, false, t => TryParse(t, out typeT)) != null ? (VoucherType?)typeT : null;
            Parsing.TrimStartComment(ref expr);

            var lst = new List<VoucherDetail>();
            VoucherDetail d;
            while ((d = ParseVoucherDetail(ref expr)) != null)
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
            // Don't use Enum.TryParse here:
            // Enum.TryParse("1001", out _) gives true
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

        public VoucherDetail ParseVoucherDetail(ref string expr)
        {
            var lst = new List<string>();

            Parsing.TrimStartComment(ref expr);
            var currency = Parsing.Token(ref expr, false, t => t.StartsWith("@", StringComparison.Ordinal))
                    ?.Substring(1)
                    .ToUpperInvariant()
                ?? BaseCurrency.Now;
            Parsing.TrimStartComment(ref expr);
            var title = Parsing.Title(ref expr);
            if (title == null)
                if (!AlternativeTitle(ref expr, lst, ref title))
                    return null;

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

            if (content == "G()")
                content = Guid.NewGuid().ToString().ToUpperInvariant();

            if (remark == "G()")
                remark = Guid.NewGuid().ToString().ToUpperInvariant();

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

        protected virtual bool AlternativeTitle(ref string expr, ICollection<string> lst, ref ITitle title) => false;
    }
}
