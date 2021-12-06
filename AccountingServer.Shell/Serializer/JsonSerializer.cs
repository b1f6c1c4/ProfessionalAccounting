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
        private const string VoucherToken = "new Voucher";
        private const string AssetToken = "new Asset";
        private const string AmortToken = "new Amortization";

        /// <inheritdoc />
        public string PresentVoucher(Voucher voucher)
            => voucher == null
                ? $"{VoucherToken}{{\n\n}}"
                : VoucherToken + PresentJson(voucher).ToString(Formatting.Indented);

        /// <inheritdoc />
        public string PresentVoucherDetail(VoucherDetail detail)
            => PresentJson(detail).ToString(Formatting.Indented);

        /// <inheritdoc />
        public string PresentVoucherDetail(VoucherDetailR detail)
            => PresentVoucherDetail((VoucherDetail)detail);

        /// <inheritdoc />
        public Voucher ParseVoucher(string str)
        {
            if (str.StartsWith(VoucherToken, StringComparison.OrdinalIgnoreCase))
                str = str.Substring(VoucherToken.Length);

            return ParseVoucher(ParseJson(str));
        }

        /// <inheritdoc />
        public VoucherDetail ParseVoucherDetail(string str) => ParseVoucherDetail(ParseJson(str));

        /// <inheritdoc />
        public string PresentAsset(Asset asset)
            => asset == null ? "null" : AssetToken + PresentJson(asset).ToString(Formatting.Indented);

        /// <inheritdoc />
        public Asset ParseAsset(string str)
        {
            if (str.StartsWith(AssetToken, StringComparison.OrdinalIgnoreCase))
                str = str.Substring(AssetToken.Length);

            var obj = ParseJson(str);
            var dateStr = obj["date"]?.Value<string>();
            DateTime? date = null;
            if (dateStr != null)
                date = ClientDateTime.Parse(dateStr);
            var schedule = obj["schedule"];
            var typeStr = obj["method"]?.Value<string>();
            var method = DepreciationMethod.StraightLine;
            if (typeStr != null)
                Enum.TryParse(typeStr, out method);

            return new Asset
                {
                    StringID = obj["id"]?.Value<string>(),
                    Name = obj["name"]?.Value<string>(),
                    Date = date,
                    User = obj["user"]?.Value<string>(),
                    Currency = obj["currency"]?.Value<string>(),
                    Value = obj["value"]?.Value<double?>(),
                    Salvage = obj["salvage"]?.Value<double?>(),
                    Life = obj["life"]?.Value<int?>(),
                    Title = obj["title"]?.Value<int?>(),
                    Method = method,
                    DepreciationTitle = obj["depreciation"]?["title"]?.Value<int?>(),
                    DepreciationExpenseTitle = obj["depreciation"]?["expense"]?["title"]?.Value<int?>(),
                    DepreciationExpenseSubTitle = obj["depreciation"]?["expense"]?["subtitle"]?.Value<int?>(),
                    DevaluationTitle = obj["devaluation"]?["title"]?.Value<int?>(),
                    DevaluationExpenseTitle = obj["devaluation"]?["expense"]?["title"]?.Value<int?>(),
                    DevaluationExpenseSubTitle = obj["devaluation"]?["expense"]?["subtitle"]?.Value<int?>(),
                    Remark = obj["remark"]?.Value<string>(),
                    Schedule = schedule == null ? new List<AssetItem>() : schedule.Select(ParseAssetItem).ToList(),
                };
        }

        /// <inheritdoc />
        public string PresentAmort(Amortization amort)
            => amort == null ? "null" : AmortToken + PresentJson(amort).ToString(Formatting.Indented);

        /// <inheritdoc />
        public Amortization ParseAmort(string str)
        {
            if (str.StartsWith(AmortToken, StringComparison.OrdinalIgnoreCase))
                str = str.Substring(AmortToken.Length);

            var obj = ParseJson(str);
            var dateStr = obj["date"]?.Value<string>();
            DateTime? date = null;
            if (dateStr != null)
                date = ClientDateTime.Parse(dateStr);
            var schedule = obj["schedule"];
            var typeStr = obj["interval"]?.Value<string>();
            var interval = AmortizeInterval.EveryDay;
            if (typeStr != null)
                Enum.TryParse(typeStr, out interval);

            return new Amortization
                {
                    StringID = obj["id"]?.Value<string>(),
                    Name = obj["name"]?.Value<string>(),
                    Date = date,
                    User = obj["user"]?.Value<string>(),
                    Value = obj["value"]?.Value<double?>(),
                    TotalDays = obj["totalDays"]?.Value<int?>(),
                    Interval = interval,
                    Template = ParseVoucher(obj["template"]),
                    Remark = obj["remark"]?.Value<string>(),
                    Schedule = schedule == null ? new List<AmortItem>() : schedule.Select(ParseAmortItem).ToList(),
                };
        }

        public string PresentVouchers(IEnumerable<Voucher> vouchers)
            => new JArray(vouchers.Select(PresentJson)).ToString(Formatting.Indented);

        public string PresentVoucherDetails(IEnumerable<VoucherDetail> details)
            => new JArray(details.Select(PresentJson)).ToString(Formatting.Indented);

        public string PresentVoucherDetails(IEnumerable<VoucherDetailR> details)
            => PresentVoucherDetails(details.Cast<VoucherDetail>());

        public string PresentAssets(IEnumerable<Asset> assets)
            => new JArray(assets.Select(PresentJson)).ToString(Formatting.Indented);

        public string PresentAmorts(IEnumerable<Amortization> amorts)
            => new JArray(amorts.Select(PresentJson)).ToString(Formatting.Indented);

        private static JObject ParseJson(string str)
        {
            try
            {
                return JObject.Parse(str);
            }
            catch (JsonReaderException e)
            {
                throw new FormatException("无法识别Json", e);
            }
        }

        private static Voucher ParseVoucher(JToken obj)
        {
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
                    Details = detail == null
                        ? new List<VoucherDetail>()
                        : detail.Select(ParseVoucherDetail).ToList(),
                };
        }

        private static AmortItem ParseAmortItem(JToken obj)
        {
            var dateStr = obj["date"]?.Value<string>();
            DateTime? date = null;
            if (dateStr != null)
                date = ClientDateTime.Parse(dateStr);

            return new AmortItem
                {
                    Date = date,
                    VoucherID = obj["voucherId"]?.Value<string>(),
                    Amount = obj["amount"].Value<double>(),
                    Remark = obj["remark"]?.Value<string>(),
                    Value = obj["value"]?.Value<double?>() ?? 0,
                };
        }

        private AssetItem ParseAssetItem(JToken obj)
        {
            var dateStr = obj["date"]?.Value<string>();
            DateTime? date = null;
            if (dateStr != null)
                date = ClientDateTime.Parse(dateStr);
            var voucherId = obj["voucherId"]?.Value<string>();
            var value = obj["value"]?.Value<double?>() ?? 0;
            var remark = obj["remark"]?.Value<string>();

            switch (obj["type"]?.Value<string>())
            {
                case "acquisition":
                    return new AcquisitionItem
                        {
                            Date = date,
                            VoucherID = voucherId,
                            Value = value,
                            Remark = remark,
                            OrigValue = obj["origValue"].Value<double>(),
                        };
                case "depreciate":
                    return new DepreciateItem
                        {
                            Date = date,
                            VoucherID = voucherId,
                            Value = value,
                            Remark = remark,
                            Amount = obj["amount"].Value<double>(),
                        };
                case "devalue":
                    return new DevalueItem
                        {
                            Date = date,
                            VoucherID = voucherId,
                            Value = value,
                            Remark = remark,
                            FairValue = obj["fairValue"].Value<double>(),
                        };
                case "disposition":
                    return new DispositionItem { Date = date, VoucherID = voucherId, Value = value, Remark = remark };
                default:
                    throw new ArgumentException("类型未知", nameof(obj));
            }
        }

        private static JObject PresentJson(Voucher voucher)
            => new JObject
                {
                    { "id", voucher.ID },
                    { "date", voucher.Date?.ToString("yyyy-MM-dd") },
                    { "remark", voucher.Remark },
                    { "type", voucher.Type?.ToString() },
                    { "detail", new JArray(voucher.Details.Select(PresentJson)) },
                };

        private static JObject PresentJson(VoucherDetail detail)
            => new JObject
                {
                    { "user", detail.User },
                    { "currency", detail.Currency },
                    { "title", detail.Title },
                    { "subtitle", detail.SubTitle },
                    { "content", detail.Content },
                    { "remark", detail.Remark },
                    { "fund", detail.Fund },
                };

        private static VoucherDetail ParseVoucherDetail(JToken obj)
            => new VoucherDetail
                {
                    User = obj["user"]?.Value<string>(),
                    Currency = obj["currency"]?.Value<string>(),
                    Title = obj["title"]?.Value<int?>(),
                    SubTitle = obj["subtitle"]?.Value<int?>(),
                    Content = obj["content"]?.Value<string>(),
                    Remark = obj["remark"]?.Value<string>(),
                    Fund = obj["fund"]?.Value<double?>(),
                };

        private static JObject PresentJson(Asset asset)
            => new JObject
                {
                    { "id", asset.StringID },
                    { "name", asset.Name },
                    { "date", asset.Date?.ToString("yyyy-MM-dd") },
                    { "user", asset.User },
                    { "currency", asset.Currency },
                    { "value", asset.Value },
                    { "salvage", asset.Salvage },
                    { "life", asset.Life },
                    { "title", asset.Title },
                    { "method", asset.Method?.ToString() },
                    {
                        "depreciation",
                        new JObject
                            {
                                { "title", asset.DepreciationTitle },
                                {
                                    "expense",
                                    new JObject
                                        {
                                            { "title", asset.DepreciationExpenseTitle },
                                            { "subtitle", asset.DepreciationExpenseSubTitle },
                                        }
                                },
                            }
                    },
                    {
                        "devaluation",
                        new JObject
                            {
                                { "title", asset.DevaluationTitle },
                                {
                                    "expense",
                                    new JObject
                                        {
                                            { "title", asset.DevaluationExpenseTitle },
                                            { "subtitle", asset.DevaluationExpenseSubTitle },
                                        }
                                },
                            }
                    },
                    { "remark", asset.Remark },
                    { "schedule", new JArray(asset.Schedule.Select(PresentJson)) },
                };

        private static JObject PresentJson(AssetItem item)
        {
            var obj = new JObject
                {
                    { "date", item.Date?.ToString("yyyy-MM-dd") },
                    { "voucherId", item.VoucherID },
                    { "value", item.Value },
                    { "remark", item.Remark },
                };

            switch (item)
            {
                case AcquisitionItem acq:
                    obj["origValue"] = acq.OrigValue;
                    obj["type"] = "acquisition";
                    break;
                case DepreciateItem dep:
                    obj["amount"] = dep.Amount;
                    obj["type"] = "depreciate";
                    break;
                case DevalueItem dev:
                    obj["fairValue"] = dev.Amount;
                    obj["type"] = "devalue";
                    break;
                case DispositionItem _:
                    obj["type"] = "disposition";
                    break;
            }

            return obj;
        }

        private static JObject PresentJson(Amortization amort)
            => new JObject
                {
                    { "id", amort.StringID },
                    { "name", amort.Name },
                    { "date", amort.Date?.ToString("yyyy-MM-dd") },
                    { "user", amort.User },
                    { "value", amort.Value },
                    { "totalDays", amort.TotalDays },
                    { "interval", amort.Interval?.ToString() },
                    { "template", PresentJson(amort.Template) },
                    { "remark", amort.Remark },
                    { "schedule", new JArray(amort.Schedule.Select(PresentJson)) },
                };

        private static JObject PresentJson(AmortItem item)
            => new JObject
                {
                    { "date", item.Date?.ToString("yyyy-MM-dd") },
                    { "voucherId", item.VoucherID },
                    { "amount", item.Amount },
                    { "value", item.Value },
                    { "remark", item.Remark },
                };
    }
}
