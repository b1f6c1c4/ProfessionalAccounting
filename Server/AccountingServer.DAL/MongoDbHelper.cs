using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace AccountingServer.DAL
{
    internal class ObjectIdWrapper : IObjectID
    {
        internal ObjectId ID;
    }

    internal static class BsonHelper
    {
        public static IObjectID Wrap(this ObjectId id) { return new ObjectIdWrapper { ID = id }; }

        public static ObjectId UnWrap(this IObjectID id)
        {
            var idWrapper = id as ObjectIdWrapper;
            if (idWrapper == null)
                throw new InvalidOperationException();
            return idWrapper.ID;
        }

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


        public static BsonDocument ToBsonDocument(this VoucherDetail detail)
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

        public static Voucher ToVoucher(this BsonDocument doc)
        {
            var voucher = new Voucher { ID = doc["_id"].AsObjectId.Wrap() };
            if (doc.Contains("date"))
                voucher.Date = doc["date"].IsBsonNull ? (DateTime?)null : doc["date"].AsLocalTime;
            voucher.Type = VoucherType.Ordinal;
            if (doc.Contains("special"))
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
            if (doc.Contains("detail"))
            {
                var ddocs = doc["detail"].AsBsonArray;
                var details = new VoucherDetail[ddocs.Count];
                for (var i = 0; i < ddocs.Count; i++)
                    details[i] = ddocs[i].AsBsonDocument.ToVoucherDetail();
                voucher.Details = details;
            }
            if (doc.Contains("remark"))
                voucher.Remark = doc["remark"].AsString;

            return voucher;
        }

        public static VoucherDetail ToVoucherDetail(this BsonDocument ddoc)
        {
            var detail = new VoucherDetail();
            if (ddoc.Contains("title"))
                detail.Title = ddoc["title"].AsInt32;
            if (ddoc.Contains("subtitle"))
                detail.SubTitle = ddoc["subtitle"].AsInt32;
            if (ddoc.Contains("content"))
                detail.Content = ddoc["content"].AsString;
            if (ddoc.Contains("fund"))
                detail.Fund = ddoc["fund"].AsDouble;
            if (ddoc.Contains("remark"))
                detail.Remark = ddoc["remark"].AsString;
            return detail;
        }
    }
    
    public class MongoDbHelper : IDbHelper
    {
        private readonly MongoClient m_Client;
        private MongoServer m_Server;
        private readonly MongoDatabase m_Db;

        private readonly MongoCollection m_Vouchers;

        public MongoDbHelper()
        {
            m_Client = new MongoClient("mongodb://localhost");
            m_Server = m_Client.GetServer();
            m_Db = m_Server.GetDatabase("accounting");

            m_Vouchers = m_Db.GetCollection("voucher");

            m_Server.Connect();
        }

        public void Dispose()
        {
            if (m_Server != null)
            {
                m_Server.Disconnect();
                m_Server = null;
            }
        }

        private static IMongoQuery GetUniqueQuery(Voucher voucher) { return Query.EQ("_id", voucher.ID.UnWrap()); }

        private static IMongoQuery GetQuery(Voucher filter)
        {
            var lst = new List<IMongoQuery>();

            if (filter.Date != null)
                lst.Add(Query.EQ("date", filter.Date));
            if (filter.Type != null)
                switch (filter.Type)
                {
                    case VoucherType.Amortization:
                        lst.Add(Query.EQ("special", "amorz"));
                        break;
                    case VoucherType.AnnualCarry:
                        lst.Add(Query.EQ("special", "acarry"));
                        break;
                    case VoucherType.Carry:
                        lst.Add(Query.EQ("special", "carry"));
                        break;
                    case VoucherType.Depreciation:
                        lst.Add(Query.EQ("special", "dep"));
                        break;
                    case VoucherType.Devalue:
                        lst.Add(Query.EQ("special", "dev"));
                        break;
                    case VoucherType.Uncertain:
                        lst.Add(Query.EQ("special", "unc"));
                        break;
                }
            if (filter.Remark != null)
                lst.Add(Query.EQ("remark", filter.Remark));

            return lst.Any() ? Query.And(lst) : Query.Null;
        }

        private static IMongoQuery GetQuery(VoucherDetail filter)
        {
            var lst = new List<IMongoQuery>();

            if (filter.Title != null)
                lst.Add(Query.EQ("title", filter.Title));
            if (filter.SubTitle != null)
                lst.Add(Query.EQ("subtitle", filter.SubTitle));
            if (filter.Content != null)
                lst.Add(Query.EQ("content", filter.Content));
            if (filter.Remark != null)
                lst.Add(Query.EQ("remark", filter.Remark));
            if (filter.Fund != null)
                lst.Add(Query.EQ("fund", filter.Fund));

            return lst.Any() ? Query.And(lst) : Query.Null;
        }
        
        public Voucher SelectVoucher(IObjectID id)
        {
            return m_Vouchers.FindOneByIdAs<BsonDocument>(id.UnWrap()).ToVoucher();
        }

        public IEnumerable<Voucher> SelectVouchers(Voucher filter)
        {
            return m_Vouchers.FindAs<BsonDocument>(GetQuery(filter)).Select(d => d.ToVoucher());
        }

        public long SelectVouchersCount(Voucher filter)
        {
            return m_Vouchers.Count(GetQuery(filter));
        }

        public bool InsertVoucher(Voucher entity)
        {
            if (entity.ID == null)
                entity.ID = ObjectId.GenerateNewId().Wrap();
            var result = m_Vouchers.Insert(entity.ToBsonDocument());
            return result.Ok;
        }

        public int DeleteVouchers(Voucher filter)
        {
            var result = m_Vouchers.Remove(GetQuery(filter));
            return result.Response["n"].AsInt32;
        }

        public bool UpdateVoucher(Voucher entity)
        {
            var result = m_Vouchers.Update(GetUniqueQuery(entity), new UpdateDocument(entity.ToBsonDocument()));
            return result.Ok;
        }
        
        public IEnumerable<Voucher> SelectVouchersWithDetail(VoucherDetail filter)
        {
            return filter.Item != null
                       ? new[] { SelectVoucher(filter.Item) }
                       : m_Vouchers.FindAs<BsonDocument>(Query.ElemMatch("detail", GetQuery(filter)))
                                   .Select(d => d.ToVoucher());
        }
        public IEnumerable<Voucher> SelectVouchersWithDetail(IEnumerable<VoucherDetail> filters)
        {
            return filters.Where(filter => filter.Item != null).Select(filter => SelectVoucher(filter.Item))
                          .Concat(
                                  m_Vouchers.FindAs<BsonDocument>(
                                                                  Query.Or(
                                                                           filters.Where(filter => filter.Item == null)
                                                                                  .Select(
                                                                                          filter =>
                                                                                          Query.ElemMatch(
                                                                                                          "detail",
                                                                                                          GetQuery(
                                                                                                                   filter)))))
                                            .Select(d => d.ToVoucher()));
        }

        public IEnumerable<VoucherDetail> SelectDetails(VoucherDetail filter)
        {
            return SelectVouchersWithDetail(filter).SelectMany(v => v.Details).Where(d => d.IsMatch(filter));
        }

        public long SelectDetailsCount(VoucherDetail filter)
        {
            return SelectVouchersWithDetail(filter).SelectMany(v => v.Details).Where(d => d.IsMatch(filter)).LongCount();
        }

        public bool InsertDetail(VoucherDetail entity)
        {
            var v = SelectVoucher(entity.Item);
            var d = new VoucherDetail[v.Details.Length + 1];
            v.Details.CopyTo(d, 0);
            d[v.Details.Length] = entity;

            var result = m_Vouchers.Update(GetUniqueQuery(v), new UpdateDocument(v.ToBsonDocument()));
            return result.Ok;
        }

        public int DeleteDetails(VoucherDetail filter)
        {
            var count = 0;
            var v = SelectVouchersWithDetail(filter);
            foreach (var voucher in v)
            {
                voucher.Details = voucher.Details.Where(d => !d.IsMatch(filter)).ToArray();
                var result = m_Vouchers.Update(GetUniqueQuery(voucher), new UpdateDocument(voucher.ToBsonDocument()));
                if (result.Ok)
                    count++;
            }
            return count;
        }

        //public DbAsset SelectAsset(Guid id) { throw new NotImplementedException(); }
        //public IEnumerable<DbAsset> SelectAssets(DbAsset filter) { throw new NotImplementedException(); }
        //public bool InsertAsset(DbAsset entity) { throw new NotImplementedException(); }
        //public int DeleteAssets(DbAsset filter) { throw new NotImplementedException(); }
        //public IEnumerable<VoucherDetail> GetXBalances(VoucherDetail filter, bool noCarry = false, int? sID = null, int? eID = null, int dir = 0) { throw new NotImplementedException(); }
        //public void Depreciate() { throw new NotImplementedException(); }
        //public void Carry() { throw new NotImplementedException(); }
        //public IEnumerable<DailyBalance> GetDailyBalance(decimal title, string remark, int dir = 0) { throw new NotImplementedException(); }
        //public IEnumerable<DailyBalance> GetDailyXBalance(decimal title, int dir = 0) { throw new NotImplementedException(); }
        //public string GetFixedAssetName(Guid id) { throw new NotImplementedException(); }
    }
}
