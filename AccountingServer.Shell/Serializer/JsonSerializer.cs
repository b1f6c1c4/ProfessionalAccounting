using System;
using System.Linq;
using AccountingServer.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AccountingServer.Shell.Serializer
{
    /// <summary>
    ///     Json表达式
    /// </summary>
    public class JsonSerializer : IEntitySerializer
    {
        /// <inheritdoc />
        public string PresentVoucher(Voucher voucher)
            => PresentVoucherJson(voucher).ToString(Formatting.Indented);

        /// <inheritdoc />
        public string PresentVoucherDetail(VoucherDetail detail)
            => PresentVoucherDetailJson(detail).ToString(Formatting.Indented);

        /// <inheritdoc />
        public Voucher ParseVoucher(string expr)
        {
            var obj = JObject.Parse(expr);
            var dateStr = obj["date"].Value<string>();
            DateTime? date = null;
            if (dateStr != null)
                date = ClientDateTime.Parse(dateStr);

            return new Voucher
                {
                    ID = obj["id"].Value<string>(),
                    Date = date,
                    Remark = obj["remark"].Value<string>(),
                    Type = obj["type"].Value<VoucherType?>(),
                    Details = obj["detail"].Select(ParseVoucherDetail).ToList()
                };
        }

        /// <inheritdoc />
        public virtual VoucherDetail ParseVoucherDetail(string expr) => ParseVoucherDetail(JObject.Parse(expr));

        public string PresentAsset(Asset asset) => throw new NotImplementedException();
        public Asset ParseAsset(string str) => throw new NotImplementedException();
        public string PresentAmort(Amortization amort) => throw new NotImplementedException();
        public Amortization ParseAmort(string str) => throw new NotImplementedException();

        private static JObject PresentVoucherJson(Voucher voucher)
            => new JObject
                {
                    { "id", voucher.ID },
                    { "date", voucher.Date },
                    { "remark", voucher.Remark },
                    { "type", voucher.Type.HasValue ? voucher.Type.Value.ToString() : null },
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
                    Currency = obj["currency"].Value<string>(),
                    Title = obj["title"].Value<int?>(),
                    SubTitle = obj["subtitle"].Value<int?>(),
                    Content = obj["content"].Value<string>(),
                    Remark = obj["remark"].Value<string>(),
                    Fund = obj["fund"].Value<double?>()
                };
    }
}
