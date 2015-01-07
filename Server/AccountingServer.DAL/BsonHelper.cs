using System;
using System.Linq;
using AccountingServer.Entities;
using MongoDB.Bson;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     Bson与实体之间的转换
    /// </summary>
    internal static class BsonHelper
    {
        private static bool ContainsNotNull(this BsonDocument doc, string key)
        {
            return doc.Contains(key) && !doc[key].IsBsonNull;
        }

        /// <summary>
        ///     将ObjectId编号转换为字符串编号
        /// </summary>
        /// <param name="id">ObjectId编号</param>
        /// <returns>字符串编号</returns>
        public static string Wrap(this ObjectId id)
        {
            return id.ToString();
        }

        /// <summary>
        ///     将字符串编号转换为ObjectId编号
        /// </summary>
        /// <param name="id">字符串编号</param>
        /// <returns>ObjectId编号</returns>
        public static ObjectId UnWrap(this string id)
        {
            return ObjectId.Parse(id);
        }

        /// <summary>
        ///     将记账凭证转换为Bson
        /// </summary>
        /// <param name="voucher">记账凭证</param>
        /// <returns>Bson</returns>
        public static BsonDocument ToBsonDocument(this Voucher voucher)
        {
            var doc = new BsonDocument { { "date", voucher.Date } };
            if (voucher.Type != VoucherType.Ordinal)
                switch (voucher.Type)
                {
                    case VoucherType.Amortization:
                        doc.Add("special", "amorz");
                        break;
                    case VoucherType.AnnualCarry:
                        doc.Add("special", "acarry");
                        break;
                    case VoucherType.Carry:
                        doc.Add("special", "carry");
                        break;
                    case VoucherType.Depreciation:
                        doc.Add("special", "dep");
                        break;
                    case VoucherType.Devalue:
                        doc.Add("special", "dev");
                        break;
                    case VoucherType.Uncertain:
                        doc.Add("special", "unc");
                        break;
                }
            if (voucher.Details != null)
            {
                var arr = new BsonArray(voucher.Details.Length);
                foreach (var detail in voucher.Details)
                    arr.Add(detail.ToBsonDocument());
                doc.Add("detail", arr);
            }
            if (voucher.Remark != null)
                doc.Add("remark", voucher.Remark);

            return doc;
        }

        /// <summary>
        ///     将细目转换为Bson
        /// </summary>
        /// <param name="detail">细目</param>
        /// <returns>Bson</returns>
        private static BsonDocument ToBsonDocument(this VoucherDetail detail)
        {
            var val = new BsonDocument { { "title", detail.Title } };
            if (detail.SubTitle.HasValue)
                val.Add("subtitle", detail.SubTitle);
            if (detail.Content != null)
                val.Add("content", detail.Content);
            val.Add("fund", detail.Fund);
            if (detail.Remark != null)
                val.Add("remark", detail.Remark);
            return val;
        }

        /// <summary>
        ///     将资产转换为Bson
        /// </summary>
        /// <param name="asset">资产</param>
        /// <returns>Bson</returns>
        public static BsonDocument ToBsonDocument(this Asset asset)
        {
            var doc = new BsonDocument
                          {
                              { "_id", asset.ID.HasValue ? asset.ID.Value.ToBsonValue() : BsonNull.Value },
                              { "name", asset.Name },
                              { "date", asset.Date },
                              { "value", asset.Value },
                              { "salvge", asset.Salvge },
                              { "life", asset.Life },
                              { "title", asset.Title },
                              { "deptitle", asset.DepreciationTitle },
                              { "devtitle", asset.DevaluationTitle },
                              {
                                  "exptitle",
                                  asset.DepreciationExpenseSubTitle.HasValue
                                      ? asset.DepreciationExpenseTitle * 100 + asset.DevaluationExpenseSubTitle
                                      : asset.DepreciationExpenseTitle
                              },
                              {
                                  "exvtitle",
                                  asset.DevaluationExpenseSubTitle.HasValue
                                      ? asset.DevaluationExpenseTitle * 100 + asset.DevaluationExpenseSubTitle
                                      : asset.DevaluationExpenseTitle
                              }
                          };
            if (asset.Method != DepreciationMethod.None)
                switch (asset.Method)
                {
                    case DepreciationMethod.StraightLine:
                        doc.Add("method", "sl");
                        break;
                    case DepreciationMethod.SumOfTheYear:
                        doc.Add("method", "sy");
                        break;
                    case DepreciationMethod.DoubleDeclineMethod:
                        doc.Add("method", "dd");
                        break;
                }
            if (asset.Schedule != null)
            {
                var arr = new BsonArray(asset.Schedule.Length);
                foreach (var item in asset.Schedule)
                    arr.Add(item.ToBsonDocument());
                doc.Add("schedule", arr);
            }
            if (asset.Remark != null)
                doc.Add("remark", asset.Remark);

            return doc;
        }

        /// <summary>
        ///     将折旧表条目转换为Bson
        /// </summary>
        /// <param name="item">条目</param>
        /// <returns>Bson</returns>
        private static BsonDocument ToBsonDocument(this AssetItem item)
        {
            var val = new BsonDocument();
            if (item.VoucherID != null)
                val.Add("voucher", item.VoucherID.UnWrap());
            if (item.Date != null)
                val.Add("date", item.Date);
            if (item.Remark != null)
                val.Add("remark", item.Remark);

            if (item is AcquisationItem)
                val.Add("acq", (item as AcquisationItem).OrigValue);
            else if (item is DepreciateItem)
                val.Add("dep", (item as DepreciateItem).Amount);
            else if (item is DevalueItem)
                val.Add("devto", (item as DevalueItem).FairValue);
            else if (item is DispositionItem)
                val.Add("dispo", (item as DispositionItem).NetValue);

            return val;
        }

        /// <summary>
        ///     从Bson还原记账凭证
        /// </summary>
        /// <param name="doc">Bson</param>
        /// <returns>记账凭证，若<paramref name="doc" />为<c>null</c>则为<c>null</c></returns>
        public static Voucher ToVoucher(this BsonDocument doc)
        {
            if (doc == null)
                return null;

            var voucher = new Voucher { ID = doc["_id"].AsObjectId.Wrap() };
            if (doc.ContainsNotNull("date"))
                voucher.Date = doc["date"].IsBsonNull ? (DateTime?)null : doc["date"].AsLocalTime;
            voucher.Type = VoucherType.Ordinal;
            if (doc.ContainsNotNull("special"))
                switch (doc["special"].AsString)
                {
                    case "amorz":
                        voucher.Type = VoucherType.Amortization;
                        break;
                    case "acarry":
                        voucher.Type = VoucherType.AnnualCarry;
                        break;
                    case "carry":
                        voucher.Type = VoucherType.Carry;
                        break;
                    case "dep":
                        voucher.Type = VoucherType.Depreciation;
                        break;
                    case "dev":
                        voucher.Type = VoucherType.Devalue;
                        break;
                    case "unc":
                        voucher.Type = VoucherType.Uncertain;
                        break;
                }
            if (doc.ContainsNotNull("detail"))
                voucher.Details = doc["detail"].AsBsonArray.Select(t => ToVoucherDetail(t.AsBsonDocument)).ToArray();
            if (doc.ContainsNotNull("remark"))
                voucher.Remark = doc["remark"].AsString;

            return voucher;
        }

        /// <summary>
        ///     从Bson还原细目
        /// </summary>
        /// <param name="ddoc">Bson</param>
        /// <returns>细目</returns>
        private static VoucherDetail ToVoucherDetail(this BsonDocument ddoc)
        {
            var detail = new VoucherDetail();
            if (ddoc.ContainsNotNull("title"))
                detail.Title = ddoc["title"].AsInt32;
            if (ddoc.ContainsNotNull("subtitle"))
                detail.SubTitle = ddoc["subtitle"].AsInt32;
            if (ddoc.ContainsNotNull("content"))
                detail.Content = ddoc["content"].AsString;
            if (ddoc.ContainsNotNull("fund"))
                detail.Fund = ddoc["fund"].AsDouble;
            if (ddoc.ContainsNotNull("remark"))
                detail.Remark = ddoc["remark"].AsString;
            return detail;
        }

        /// <summary>
        ///     从Bson还原资产
        /// </summary>
        /// <param name="doc">Bson</param>
        /// <returns>资产，若<paramref name="doc" />为<c>null</c>则为<c>null</c></returns>
        public static Asset ToAsset(this BsonDocument doc)
        {
            if (doc == null)
                return null;

            var asset = new Asset { ID = doc["_id"].AsGuid };
            if (doc.ContainsNotNull("name"))
                asset.Name = doc["name"].AsString;
            if (doc.ContainsNotNull("date"))
                asset.Date = doc["date"].AsLocalTime;
            if (doc.ContainsNotNull("value"))
                asset.Value = doc["value"].AsDouble;
            if (doc.ContainsNotNull("salvge"))
                asset.Salvge = doc["salvge"].AsDouble;
            if (doc.ContainsNotNull("life"))
                asset.Life = doc["life"].AsInt32;
            if (doc.ContainsNotNull("title"))
                asset.Title = doc["title"].AsInt32;
            if (doc.ContainsNotNull("deptitle"))
                asset.DepreciationTitle = doc["deptitle"].AsInt32;
            if (doc.ContainsNotNull("devtitle"))
                asset.DevaluationTitle = doc["devtitle"].AsInt32;
            if (doc.ContainsNotNull("exptitle"))
            {
                var expenseTitle = doc["exptitle"].AsInt32;
                asset.DepreciationExpenseTitle = expenseTitle / 100;
                asset.DepreciationExpenseSubTitle = expenseTitle % 100;
            }
            if (doc.ContainsNotNull("exvtitle"))
            {
                var expenseTitle = doc["exvtitle"].AsInt32;
                asset.DevaluationExpenseTitle = expenseTitle / 100;
                asset.DevaluationExpenseSubTitle = expenseTitle % 100;
            }
            asset.Method = DepreciationMethod.None;
            if (doc.ContainsNotNull("method"))
                switch (doc["method"].AsString)
                {
                    case "sl":
                        asset.Method = DepreciationMethod.StraightLine;
                        break;
                    case "sy":
                        asset.Method = DepreciationMethod.SumOfTheYear;
                        break;
                    case "dd":
                        asset.Method = DepreciationMethod.DoubleDeclineMethod;
                        break;
                }
            if (doc.ContainsNotNull("schedule"))
                asset.Schedule = doc["schedule"].AsBsonArray.Select(t => t.AsBsonDocument.ToAssetItem()).ToArray();
            if (doc.ContainsNotNull("remark"))
                asset.Remark = doc["remark"].AsString;

            return asset;
        }

        /// <summary>
        ///     从Bson还原折旧表条目
        /// </summary>
        /// <param name="ddoc">Bson</param>
        /// <returns>条目</returns>
        private static AssetItem ToAssetItem(this BsonDocument ddoc)
        {
            AssetItem item;
            if (ddoc.ContainsNotNull("acq"))
                item = new AcquisationItem { OrigValue = ddoc["acq"].AsDouble };
            else if (ddoc.ContainsNotNull("dep"))
                item = new DepreciateItem { Amount = ddoc["dep"].AsDouble };
            else if (ddoc.ContainsNotNull("devto"))
                item = new DevalueItem { FairValue = ddoc["devto"].AsDouble };
            else if (ddoc.ContainsNotNull("dispo"))
                item = new DevalueItem { FairValue = ddoc["dispo"].AsDouble };
            else
                throw new InvalidOperationException();

            if (ddoc.ContainsNotNull("voucher"))
                item.VoucherID = ddoc["voucher"].AsObjectId.Wrap();
            if (ddoc.ContainsNotNull("date"))
                item.Date = ddoc["date"].AsBsonDateTime.ToLocalTime();
            if (ddoc.ContainsNotNull("remark"))
                item.Remark = ddoc["remark"].AsString;

            return item;
        }

        public static BsonValue ToBsonValue(this Guid id) { return new BsonBinaryData(id); }
    }
}
