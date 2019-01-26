using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Serializer
{
    /// <summary>
    ///     Csv表达式
    /// </summary>
    public class CsvSerializer : IEntitiesSerializer
    {
        private readonly string m_Sep = "\t";
        private readonly List<ColumnSpec> m_Specs = new List<ColumnSpec>();

        public CsvSerializer(string spec)
        {
            {
                var sep = Parsing.Quoted(ref spec, '\'');
                if (sep != null)
                    m_Sep = sep;
            }

            if (string.IsNullOrWhiteSpace(spec))
            {
                m_Specs.Add(ColumnSpec.VoucherID);
                m_Specs.Add(ColumnSpec.VoucherDate);
                m_Specs.Add(ColumnSpec.Currency);
                m_Specs.Add(ColumnSpec.Title);
                m_Specs.Add(ColumnSpec.TitleComment);
                m_Specs.Add(ColumnSpec.SubTitle);
                m_Specs.Add(ColumnSpec.SubTitleComment);
                m_Specs.Add(ColumnSpec.Content);
                m_Specs.Add(ColumnSpec.Remark);
                m_Specs.Add(ColumnSpec.Fund);
                return;
            }

            while (!string.IsNullOrWhiteSpace(spec))
            {
                var sep = Parsing.Quoted(ref spec, '\'');
                if (sep != null)
                {
                    m_Sep = sep;
                    continue;
                }

                var s = Parsing.Token(ref spec, false);
                switch (s)
                {
                    case "id":
                        m_Specs.Add(ColumnSpec.VoucherID);
                        break;
                    case "d":
                    case "date":
                        m_Specs.Add(ColumnSpec.VoucherDate);
                        break;
                    case "type":
                        m_Specs.Add(ColumnSpec.VoucherType);
                        break;
                    case "C":
                    case "currency":
                        m_Specs.Add(ColumnSpec.Currency);
                        break;
                    case "t":
                    case "title":
                        m_Specs.Add(ColumnSpec.Title);
                        break;
                    case "t'":
                        m_Specs.Add(ColumnSpec.TitleComment);
                        break;
                    case "s":
                    case "subtitle":
                        m_Specs.Add(ColumnSpec.SubTitle);
                        break;
                    case "s'":
                        m_Specs.Add(ColumnSpec.SubTitleComment);
                        break;
                    case "c":
                    case "content":
                        m_Specs.Add(ColumnSpec.Content);
                        break;
                    case "r":
                    case "remark":
                        m_Specs.Add(ColumnSpec.Remark);
                        break;
                    case "v":
                    case "fund":
                        m_Specs.Add(ColumnSpec.Fund);
                        break;
                }
            }
        }

        /// <inheritdoc />
        public string PresentVoucher(Voucher voucher)
            => voucher == null ? "" : PresentVouchers(new[] { voucher });

        /// <inheritdoc />
        public string PresentVoucherDetail(VoucherDetail detail) => PresentVoucherDetails(new[] { detail });

        /// <inheritdoc />
        public string PresentVoucherDetail(VoucherDetailR detail) => PresentVoucherDetails(new[] { detail });

        /// <inheritdoc />
        public string PresentVouchers(IEnumerable<Voucher> vouchers)
            => PresentVoucherDetails(vouchers.SelectMany(v => v.Details.Select(d => new VoucherDetailR(v, d))));

        /// <inheritdoc />
        public string PresentVoucherDetails(IEnumerable<VoucherDetail> details)
            => Present(
                details.Select(d => new VoucherDetailR(null, d)),
                m_Specs.Where(s => !s.HasFlag(ColumnSpec.Voucher)).ToList());

        /// <inheritdoc />
        public string PresentVoucherDetails(IEnumerable<VoucherDetailR> details)
            => Present(details, m_Specs);

        public Voucher ParseVoucher(string str) => throw new NotImplementedException();
        public VoucherDetail ParseVoucherDetail(string str) => throw new NotImplementedException();
        public string PresentAsset(Asset asset) => throw new NotImplementedException();
        public Asset ParseAsset(string str) => throw new NotImplementedException();
        public string PresentAmort(Amortization amort) => throw new NotImplementedException();
        public Amortization ParseAmort(string str) => throw new NotImplementedException();

        public string PresentAssets(IEnumerable<Asset> assets) => throw new NotImplementedException();
        public string PresentAmorts(IEnumerable<Amortization> amorts) => throw new NotImplementedException();

        /// <summary>
        ///     将带记账凭证的细目转换为Csv表示
        /// </summary>
        /// <param name="details">细目</param>
        /// <param name="spec">列</param>
        /// <returns>Csv表示</returns>
        private string Present(IEnumerable<VoucherDetailR> details, IList<ColumnSpec> spec)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < spec.Count; i++)
            {
                var s = spec[i];
                if (i > 0)
                    sb.Append(m_Sep);
                sb.Append(s);
            }

            sb.AppendLine();

            foreach (var d in details)
            {
                for (var i = 0; i < spec.Count; i++)
                {
                    if (i > 0)
                        sb.Append(m_Sep);

                    var s = spec[i];
                    switch (s)
                    {
                        case ColumnSpec.VoucherID:
                            sb.Append(d.Voucher.ID);
                            break;
                        case ColumnSpec.VoucherDate:
                            sb.Append(d.Voucher.Date.AsDate());
                            break;
                        case ColumnSpec.VoucherType:
                            sb.Append(d.Voucher.Type);
                            break;
                        case ColumnSpec.Currency:
                            sb.Append(d.Currency);
                            break;
                        case ColumnSpec.Title:
                            sb.Append(d.Title.AsTitle());
                            break;
                        case ColumnSpec.TitleComment:
                            sb.Append(TitleManager.GetTitleName(d.Title));
                            break;
                        case ColumnSpec.SubTitle:
                            sb.Append(d.SubTitle.AsSubTitle());
                            break;
                        case ColumnSpec.SubTitleComment:
                            sb.Append(TitleManager.GetTitleName(d.Title, d.SubTitle));
                            break;
                        case ColumnSpec.Content:
                            sb.Append(d.Content.Quotation('\''));
                            break;
                        case ColumnSpec.Remark:
                            sb.Append(d.Remark.Quotation('"'));
                            break;
                        case ColumnSpec.Fund:
                            sb.Append($"{d.Fund:R}");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        [Flags]
        private enum ColumnSpec
        {
            Voucher = 0x1000,
            VoucherID = 0x1001,
            VoucherDate = 0x1002,
            VoucherType = 0x1003,
            Currency = 0x4,
            Title = 0x5,
            TitleComment = 0x6,
            SubTitle = 0x7,
            SubTitleComment = 0x8,
            Content = 0x9,
            Remark = 0xa,
            Fund = 0xb
        }
    }
}
