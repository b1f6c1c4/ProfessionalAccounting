using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace AccountingServer.DAL
{
    /// <summary>
    /// Bson与实体之间的转换
    /// </summary>
    internal static class BsonHelper
    {
        /// <summary>
        /// 将ObjectId编号转换为字符串编号
        /// </summary>
        /// <param name="id">ObjectId编号</param>
        /// <returns>字符串编号</returns>
        public static string Wrap(this ObjectId id) { return id.ToString(); }

        /// <summary>
        /// 将字符串编号转换为ObjectId编号
        /// </summary>
        /// <param name="id">字符串编号</param>
        /// <returns>ObjectId编号</returns>
        public static ObjectId UnWrap(this string id) { return ObjectId.Parse(id); }

        /// <summary>
        /// 将记账凭证转换为Bson
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
        /// 将细目转换为Bson
        /// </summary>
        /// <param name="detail">细目</param>
        /// <returns>Bson</returns>
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

        /// <summary>
        /// 从Bson还原记账凭证
        /// </summary>
        /// <param name="doc">Bson</param>
        /// <returns>记账凭证，若<paramref name="doc"/>为<c>null</c>则为<c>null</c></returns>
        public static Voucher ToVoucher(this BsonDocument doc)
        {
            if (doc == null)
                return null;

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

        /// <summary>
        /// 从Bson还原细目
        /// </summary>
        /// <param name="ddoc">Bson</param>
        /// <returns>细目</returns>
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
    
    /// <summary>
    /// MongoDb数据访问类
    /// </summary>
    public class MongoDbHelper : IDbHelper
    {
        /// <summary>
        /// MongoDb客户端
        /// </summary>
        private readonly MongoClient m_Client;
        /// <summary>
        /// MongoDb服务器
        /// </summary>
        private MongoServer m_Server;
        /// <summary>
        /// MongoDb数据库
        /// </summary>
        private readonly MongoDatabase m_Db;

        /// <summary>
        /// 记账凭证集合
        /// </summary>
        private readonly MongoCollection m_Vouchers;

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public MongoDbHelper()
        {
            m_Client = new MongoClient("mongodb://localhost");
            m_Server = m_Client.GetServer();
            m_Db = m_Server.GetDatabase("accounting");

            m_Vouchers = m_Db.GetCollection("voucher");

            m_Server.Connect();
        }

        /// <summary>
        /// 从服务器断开
        /// </summary>
        public void Dispose()
        {
            if (m_Server != null)
            {
                m_Server.Disconnect();
                m_Server = null;
            }
        }

        /// <summary>
        /// 按编号唯一查询
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>Bson查询</returns>
        private static IMongoQuery GetUniqueQuery(string id) { return Query.EQ("_id", id.UnWrap()); }
        /// <summary>
        /// 按记账凭证的编号唯一查询
        /// </summary>
        /// <param name="voucher">记账凭证</param>
        /// <returns>Bson查询</returns>
        private static IMongoQuery GetUniqueQuery(Voucher voucher) { return GetUniqueQuery(voucher.ID); }
        /// <summary>
        /// 按过滤器查询
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>Bson查询</returns>
        private static IMongoQuery GetQuery(Voucher filter)
        {
            if (filter == null)
                return Query.Null;

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

        /// <summary>
        /// 按日期查询
        /// <para>若<paramref name="startDate"/>和<paramref name="endDate"/>均为<c>null</c>，则返回所有无日期的记账凭证</para>
        /// </summary>
        /// <param name="startDate">开始日期，若为<c>null</c>表示不检查最小日期，无日期亦可</param>
        /// <param name="endDate">截止日期，若为<c>null</c>表示不检查最大日期</param>
        /// <returns>Bson查询</returns>
        public static IMongoQuery GetQuery(DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue &&
                endDate.HasValue)
                return Query.And(Query.GTE("date", startDate), Query.LTE("date", endDate));
            if (startDate.HasValue)
                return Query.GTE("date", startDate);
            if (endDate.HasValue)
                return Query.Or(Query.EQ("date", null), Query.LTE("date", endDate));
            return Query.EQ("date", null);
        }

        /// <summary>
        /// 按细目过滤器查询
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <returns>Bson查询</returns>
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

        public Voucher SelectVoucher(string id)
        {
            return m_Vouchers.FindOneByIdAs<BsonDocument>(id.UnWrap()).ToVoucher();
        }

        public IEnumerable<Voucher> SelectVouchers(Voucher filter)
        {
            return m_Vouchers.FindAs<BsonDocument>(GetQuery(filter)).Select(d => d.ToVoucher());
        }

        public IEnumerable<Voucher> SelectVouchers(Voucher filter, DateTime? startDate, DateTime? endDate)
        {
            return
                m_Vouchers.FindAs<BsonDocument>(Query.And(GetQuery(filter), GetQuery(startDate, endDate)))
                          .Select(d => d.ToVoucher());
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

        public bool DeleteVoucher(string id)
        {
            var result = m_Vouchers.Remove(GetUniqueQuery(id));
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

        public IEnumerable<Voucher> SelectVouchersWithDetail(VoucherDetail filter, DateTime? startDate,
                                                             DateTime? endDate)
        {
            return filter.Item != null
                       ? new[] { SelectVoucher(filter.Item) }
                       : m_Vouchers.FindAs<BsonDocument>(
                                                         Query.And(
                                                                   GetQuery(startDate, endDate),
                                                                   Query.ElemMatch("detail", GetQuery(filter))))
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

        public IEnumerable<Voucher> SelectVouchersWithDetail(IEnumerable<VoucherDetail> filters, DateTime? startDate,
                                                             DateTime? endDate)
        {
            return filters.Where(filter => filter.Item != null).Select(filter => SelectVoucher(filter.Item))
                          .Concat(
                                  m_Vouchers.FindAs<BsonDocument>(
                                                                  Query.And(
                                                                            GetQuery(startDate, endDate),
                                                                            Query.Or(
                                                                                     filters.Where(
                                                                                                   filter =>
                                                                                                   filter.Item == null)
                                                                                            .Select(
                                                                                                    filter =>
                                                                                                    Query.ElemMatch(
                                                                                                                    "detail",
                                                                                                                    GetQuery
                                                                                                                        (
                                                                                                                         filter))))))
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
