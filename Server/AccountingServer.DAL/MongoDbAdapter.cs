using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     MongoDb数据访问类
    /// </summary>
    public class MongoDbAdapter : IDbAdapter, IDbServer
    {
        /// <summary>
        ///     MongoDb客户端
        /// </summary>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private MongoClient m_Client;

        /// <summary>
        ///     MongoDb服务器
        /// </summary>
        private MongoServer m_Server;

        /// <summary>
        ///     MongoDb数据库
        /// </summary>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private MongoDatabase m_Db;

        /// <summary>
        ///     记账凭证集合
        /// </summary>
        private MongoCollection m_Vouchers;

        /// <summary>
        ///     资产集合
        /// </summary>
        private MongoCollection m_Assets;

        public void Launch()
        {
            var startinfo = new ProcessStartInfo
                                {
                                    FileName = "cmd.exe",
                                    Arguments =
                                        "/c " +
                                        "mongod --config \"C:\\Users\\b1f6c1c4\\Documents\\tjzh\\Account\\mongod.conf\"",
                                    UseShellExecute = false,
                                    RedirectStandardInput = false,
                                    RedirectStandardOutput = true,
                                    CreateNoWindow = true
                                };

            var process = Process.Start(startinfo);
            if (process == null)
                throw new Exception();
        }

        public void Connect()
        {
            m_Client = new MongoClient("mongodb://localhost");
            m_Server = m_Client.GetServer();
            m_Server.Connect();

            m_Db = m_Server.GetDatabase("accounting");

            m_Vouchers = m_Db.GetCollection("voucher");
            m_Assets = m_Db.GetCollection("asset");
        }

        public void Disconnect()
        {
            if (m_Server == null)
                return;

            m_Db = null;
            m_Vouchers = null;
            m_Assets = null;

            m_Server.Disconnect();
            m_Server = null;
            m_Client = null;
        }

        public void Shutdown()
        {
            if (m_Server == null)
                return;

            m_Db = null;
            m_Vouchers = null;
            m_Assets = null;
            m_Client = null;

            m_Server.Disconnect();

            try
            {
                m_Server.Shutdown();
            }
            catch (EndOfStreamException) { }
            m_Server = null;
        }

        /// <summary>
        ///     按编号唯一查询
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>Bson查询</returns>
        private static IMongoQuery GetUniqueQuery(string id)
        {
            return Query.EQ("_id", id.UnWrap());
        }

        /// <summary>
        ///     按记账凭证的编号唯一查询
        /// </summary>
        /// <param name="voucher">记账凭证</param>
        /// <returns>Bson查询</returns>
        private static IMongoQuery GetUniqueQuery(Voucher voucher)
        {
            return GetUniqueQuery(voucher.ID);
        }

        /// <summary>
        ///     按编号唯一查询
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>Bson查询</returns>
        private static IMongoQuery GetUniqueQuery(Guid? id)
        {
            return Query.EQ("_id", id.HasValue ? id.Value.ToBsonValue() : BsonNull.Value);
        }

        /// <summary>
        ///     按资产的编号唯一查询
        /// </summary>
        /// <param name="asset">资产</param>
        /// <returns>Bson查询</returns>
        private static IMongoQuery GetUniqueQuery(Asset asset)
        {
            return GetUniqueQuery(asset.ID);
        }

        /// <summary>
        ///     按过滤器查询
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
                lst.Add(
                        filter.Remark == String.Empty
                            ? Query.EQ("remark", BsonNull.Value)
                            : Query.EQ("remark", filter.Remark));

            return lst.Any() ? Query.And(lst) : Query.Null;
        }

        /// <summary>
        ///     按日期查询
        /// </summary>
        /// <param name="rng">日期过滤器</param>
        /// <returns>Bson查询</returns>
        private static IMongoQuery GetQuery(DateFilter rng)
        {
            if (rng.NullOnly)
                return Query.EQ("date", BsonNull.Value);

            IMongoQuery q;
            if (rng.Constrained)
                q = Query.And(Query.GTE("date", rng.StartDate), Query.LTE("date", rng.EndDate));
            else if (rng.StartDate.HasValue)
                q = Query.GTE("date", rng.StartDate);
            else if (rng.EndDate.HasValue)
                q = Query.LTE("date", rng.EndDate);
            else
                return rng.Nullable ? Query.Null : Query.NE("date", BsonNull.Value);

            return rng.Nullable ? Query.Or(q, Query.EQ("date", BsonNull.Value)) : q;
        }

        /// <summary>
        ///     按细目过滤器查询
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <returns>Bson查询</returns>
        private static IMongoQuery GetQuery(VoucherDetail filter)
        {
            if (filter == null)
                return Query.Null;

            var lst = new List<IMongoQuery>();

            if (filter.Title != null)
                lst.Add(Query.EQ("title", filter.Title));
            if (filter.SubTitle != null)
                lst.Add(
                        filter.SubTitle == 0
                            ? Query.EQ("subtitle", BsonNull.Value)
                            : Query.EQ("subtitle", filter.SubTitle));
            if (filter.Content != null)
                lst.Add(
                        filter.Content == String.Empty
                            ? Query.EQ("content", BsonNull.Value)
                            : Query.EQ("content", filter.Content));
            if (filter.Remark != null)
                lst.Add(
                        filter.Remark == String.Empty
                            ? Query.EQ("remark", BsonNull.Value)
                            : Query.EQ("remark", filter.Remark));
            if (filter.Fund != null)
                lst.Add(Query.EQ("fund", filter.Fund));

            return lst.Any() ? Query.And(lst) : Query.Null;
        }

        /// <summary>
        ///     按过滤器查询
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>Bson查询</returns>
        private static IMongoQuery GetQuery(Asset filter)
        {
            if (filter == null)
                return Query.Null;

            var lst = new List<IMongoQuery>();

            if (filter.ID != null)
                lst.Add(Query.EQ("_id", filter.ID.Value.ToBsonValue()));
            if (filter.Name != null)
                lst.Add(
                        filter.Name == String.Empty
                            ? Query.EQ("name", BsonNull.Value)
                            : Query.EQ("name", filter.Name));
            if (filter.Date != null)
                lst.Add(Query.EQ("date", filter.Date));
            if (filter.Value != null)
                lst.Add(Query.EQ("value", filter.Value));
            if (filter.Salvge != null)
                lst.Add(Query.EQ("salvge", filter.Salvge));
            if (filter.Life != null)
                lst.Add(Query.EQ("life", filter.Life));
            if (filter.Title != null)
                lst.Add(Query.EQ("title", filter.Title));
            if (filter.DepreciationTitle != null)
                lst.Add(Query.EQ("deptitle", filter.DepreciationTitle));
            if (filter.DevaluationTitle != null)
                lst.Add(Query.EQ("devtitle", filter.DevaluationTitle));
            if (filter.ExpenseTitle != null)
                lst.Add(
                        Query.EQ(
                                 "exptitle",
                                 filter.ExpenseSubTitle != null
                                     ? filter.ExpenseTitle * 100 + filter.ExpenseSubTitle
                                     : filter.ExpenseTitle));
            if (filter.Method.HasValue)
                switch (filter.Method.Value)
                {
                    case DepreciationMethod.StraightLine:
                        lst.Add(Query.EQ("method", "sl"));
                        break;
                    case DepreciationMethod.SumOfTheYear:
                        lst.Add(Query.EQ("method", "sy"));
                        break;
                    case DepreciationMethod.DoubleDeclineMethod:
                        lst.Add(Query.EQ("method", "dd"));
                        break;
                }

            return lst.Any() ? Query.And(lst) : Query.Null;
        }


        public Voucher SelectVoucher(string id)
        {
            return m_Vouchers.FindOneByIdAs<BsonDocument>(id.UnWrap()).ToVoucher();
        }

        public IEnumerable<Voucher> FilteredSelect(Voucher filter, DateFilter rng)
        {
            return
                m_Vouchers.FindAs<BsonDocument>(Query.And(GetQuery(filter), GetQuery(rng)))
                          .Select(d => d.ToVoucher());
        }

        public long FilteredCount(Voucher filter) { return m_Vouchers.Count(GetQuery(filter)); }

        public bool Insert(Voucher entity)
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

        public int FilteredDelete(Voucher filter)
        {
            var result = m_Vouchers.Remove(GetQuery(filter));
            return result.Response["n"].AsInt32;
        }

        public bool Update(Voucher entity)
        {
            var result = m_Vouchers.Update(GetUniqueQuery(entity), new UpdateDocument(entity.ToBsonDocument()));
            return result.Ok;
        }

        public IEnumerable<Voucher> FilteredSelect(VoucherDetail filter)
        {
            if (filter.Item != null)
                return new[] { SelectVoucher(filter.Item) };

            var queryFilter = GetQuery(filter);
            var query = queryFilter != Query.Null ? Query.ElemMatch("detail", queryFilter) : Query.Null;

            return m_Vouchers.FindAs<BsonDocument>(query)
                             .Select(d => d.ToVoucher());
        }

        public IEnumerable<Voucher> FilteredSelect(VoucherDetail filter, DateFilter rng)
        {
            if (filter.Item != null)
                return new[] { SelectVoucher(filter.Item) };

            var queryFilter = GetQuery(filter);
            var query = queryFilter != Query.Null
                            ? Query.And(
                                        GetQuery(rng),
                                        Query.ElemMatch("detail", queryFilter))
                            : GetQuery(rng);

            return m_Vouchers.FindAs<BsonDocument>(query).Select(d => d.ToVoucher());
        }

        public IEnumerable<Voucher> FilteredSelect(IEnumerable<VoucherDetail> filters, DateFilter rng)
        {
            return filters.Where(filter => filter.Item != null).Select(filter => SelectVoucher(filter.Item))
                          .Concat(
                                  m_Vouchers.FindAs<BsonDocument>(
                                                                  Query.And(
                                                                            GetQuery(rng),
                                                                            Query.Or(
                                                                                     filters.Where(
                                                                                                   filter =>
                                                                                                   filter.Item == null)
                                                                                            .Select(GetQuery)
                                                                                            .Where(
                                                                                                   query =>
                                                                                                   query != Query.Null)
                                                                                            .Select(
                                                                                                    query =>
                                                                                                    Query.ElemMatch(
                                                                                                                    "detail",
                                                                                                                    query)))))
                                            .Select(d => d.ToVoucher()));
        }

        public IEnumerable<VoucherDetail> FilteredSelectDetails(VoucherDetail filter, DateFilter rng)
        {
            return
                FilteredSelect(filter, rng)
                    .SelectMany(v => v.Details)
                    .Where(d => d.IsMatch(filter));
        }

        public long FilteredCount(VoucherDetail filter)
        {
            return FilteredSelect(filter).SelectMany(v => v.Details).Where(d => d.IsMatch(filter)).LongCount();
        }

        public bool Insert(VoucherDetail entity)
        {
            var v = SelectVoucher(entity.Item);
            var d = new VoucherDetail[v.Details.Length + 1];
            v.Details.CopyTo(d, 0);
            d[v.Details.Length] = entity;

            var result = m_Vouchers.Update(GetUniqueQuery(v), new UpdateDocument(v.ToBsonDocument()));
            return result.Ok;
        }

        public int FilteredDelete(VoucherDetail filter)
        {
            var count = 0;
            var v = FilteredSelect(filter);
            foreach (var voucher in v)
            {
                voucher.Details = voucher.Details.Where(d => !d.IsMatch(filter)).ToArray();
                var result = m_Vouchers.Update(GetUniqueQuery(voucher), new UpdateDocument(voucher.ToBsonDocument()));
                if (result.Ok)
                    count++;
            }
            return count;
        }


        public Asset SelectAsset(Guid id)
        {
            return m_Assets.FindOneAs<BsonDocument>(Query.EQ("_id", id.ToBsonValue())).ToAsset();
        }

        public IEnumerable<Asset> FilteredSelect(Asset filter)
        {
            return m_Assets.FindAs<BsonDocument>(GetQuery(filter)).Select(d => d.ToAsset());
        }

        public bool Insert(Asset entity)
        {
            var res = m_Assets.Insert(entity.ToBsonDocument());
            return res.Ok;
        }

        public bool DeleteAsset(Guid id)
        {
            var res = m_Assets.Remove(GetUniqueQuery(id));
            return res.Ok;
        }

        public bool Update(Asset entity)
        {
            var result = m_Assets.Update(GetUniqueQuery(entity), new UpdateDocument(entity.ToBsonDocument()));
            return result.Ok;
        }

        public int FilteredDelete(Asset filter)
        {
            var result = m_Assets.Remove(GetQuery(filter));
            return result.Response["n"].AsInt32;
        }

        //public void Depreciate() { throw new NotImplementedException(); }
        //public void Carry() { throw new NotImplementedException(); }
    }
}
