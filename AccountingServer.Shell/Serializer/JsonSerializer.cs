using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AccountingServer.Shell.Serializer
{
    /// <summary>
    ///     Json表达式
    /// </summary>
    public class JsonSerializer : IEntitiesSerializer
    {
        private const string TheToken = "new Voucher";

        /// <inheritdoc />
        public string PresentVoucher(Voucher voucher)
            => voucher == null
                ? $"{TheToken}{{\n\n}}"
                : TheToken + PresentVoucherJson(voucher).ToString(Formatting.Indented);

        /// <inheritdoc />
        public string PresentVoucherDetail(VoucherDetail detail)
            => PresentVoucherDetailJson(detail).ToString(Formatting.Indented);

        /// <inheritdoc />
        public Voucher ParseVoucher(string expr)
        {
            if (expr.StartsWith(TheToken, StringComparison.OrdinalIgnoreCase))
                expr = expr.Substring(TheToken.Length);

            var obj = JObject.Parse(expr);
            var dateStr = obj["date"]?.Value<string>();
            DateTime? date = null;
            if (dateStr != null)
                date = ClientDateTime.Parse(dateStr);
            var detail = obj["detail"];
            var typeStr = obj["type"]?.Value<string>();
            var type = VoucherType.Ordinary;
            if (typeStr != null)
                Enum.TryParse(typeStr, out type);

            return new Voucher
                {
                    ID = obj["id"]?.Value<string>(),
                    Date = date,
                    Remark = obj["remark"]?.Value<string>(),
                    Type = type,
                    Details = detail == null ? new List<VoucherDetail>() : detail.Select(ParseVoucherDetail).ToList()
                };
        }

        /// <inheritdoc />
        public VoucherDetail ParseVoucherDetail(string expr) => ParseVoucherDetail(JObject.Parse(expr));

        public string PresentAsset(Asset asset) => throw new NotImplementedException();
        public Asset ParseAsset(string str) => throw new NotImplementedException();
        public string PresentAmort(Amortization amort) => throw new NotImplementedException();
        public Amortization ParseAmort(string str) => throw new NotImplementedException();

        public string PresentVouchers(IEnumerable<Voucher> vouchers)
            => new JArray(vouchers.Select(PresentVoucherJson)).ToString(Formatting.Indented);

        public string PresentVoucherDetails(IEnumerable<VoucherDetail> details)
            => new JArray(details.Select(PresentVoucherDetailJson)).ToString(Formatting.Indented);

        private static JObject PresentVoucherJson(Voucher voucher)
            => new JObject
                {
                    { "id", voucher.ID },
                    { "date", voucher.Date?.ToString("yyyyy-MM-dd") },
                    { "remark", voucher.Remark },
                    { "type", voucher.Type?.ToString() },
                    { "detail", new JArray(voucher.Details.Select(PresentVoucherDetailJson)) }
                };

        private static JObject PresentVoucherDetailJson(VoucherDetail detail)
            => new JObject
                {
                    { "currency", detail.Currency },
                    { "title", detail.Title },
                    { "subtitle", detail.SubTitle },
                    { "content", detail.Content },
                    { "remark", detail.Remark },
                    { "fund", detail.Fund }
                };

        private static VoucherDetail ParseVoucherDetail(JToken obj)
            => new VoucherDetail
                {
                    Currency = obj["currency"]?.Value<string>(),
                    Title = obj["title"]?.Value<int?>(),
                    SubTitle = obj["subtitle"]?.Value<int?>(),
                    Content = obj["content"]?.Value<string>(),
                    Remark = obj["remark"]?.Value<string>(),
                    Fund = obj["fund"]?.Value<double?>()
                };
    }
}
