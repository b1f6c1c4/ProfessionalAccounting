using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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
        private MongoCollection<Voucher> m_Vouchers;

        /// <summary>
        ///     资产集合
        /// </summary>
        private MongoCollection<Asset> m_Assets;

        /// <summary>
        ///     是否已经连接到数据库
        /// </summary>
        public bool Connected { get; private set; }

        public MongoDbAdapter() { Connected = false; }

        static MongoDbAdapter()
        {
            BsonSerializer.RegisterSerializer(typeof(Voucher), new VoucherSerializer());
            BsonSerializer.RegisterSerializer(typeof(VoucherDetail), new VoucherDetailSerializer());
            BsonSerializer.RegisterSerializer(typeof(Asset), new AssetSerializer());
            BsonSerializer.RegisterSerializer(typeof(AssetItem), new AssetItemSerializer());
        }

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

            m_Db = m_Server.GetDatabase("accounting");

            m_Vouchers = m_Db.GetCollection<Voucher>("voucher");
            m_Assets = m_Db.GetCollection<Asset>("asset");

            Connected = true;
        }

        public void Disconnect()
        {
            if (!Connected)
                return;

            m_Db = null;
            m_Vouchers = null;
            m_Assets = null;

            m_Server.Disconnect();
            m_Server = null;
            m_Client = null;

            Connected = false;
        }

        public void Backup()
        {
            var startinfo = new ProcessStartInfo
                                {
                                    FileName = "cmd.exe",
                                    Arguments =
                                        "/c " +
                                        "mongodump -h 127.0.0.1 -d accounting -o \"C:\\Users\\b1f6c1c4\\OneDrive\\Backup\"",
                                    UseShellExecute = false,
                                    RedirectStandardInput = false,
                                    RedirectStandardOutput = true,
                                    CreateNoWindow = true
                                };

            var process = Process.Start(startinfo);
            if (process == null)
                throw new Exception();
        }

        /// <summary>
        ///     按编号唯一查询
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>Bson查询</returns>
        private static IMongoQuery GetUniqueQuery(string id)
        {
            return Query.EQ("_id", ObjectId.Parse(id));
        }

        /// <summary>
        ///     按编号唯一查询
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>Bson查询</returns>
        private static IMongoQuery GetUniqueQuery(Guid? id)
        {
            return Query.EQ("_id", id.HasValue ? id.Value.ToBsonValue() as BsonValue : BsonNull.Value);
        }

        /// <summary>
        ///     按过滤器查询
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>Bson查询</returns>
        private static IMongoQuery GetQuery(Voucher filter)
        {
            if (filter == null)
                return null;

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

            return And(lst);
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
                return rng.Nullable ? null : Query.NE("date", BsonNull.Value);

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
                return null;

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

            return And(lst);
        }

        /// <summary>
        ///     按过滤器和细目过滤器查询
        /// </summary>
        /// <param name="vfilter">过滤器</param>
        /// <param name="filter">细目过滤器</param>
        /// <returns>Bson查询</returns>
        private static IMongoQuery GetQuery(Voucher vfilter, VoucherDetail filter)
        {
            var queryVoucher = GetQuery(vfilter);
            var queryFilter = GetQuery(filter);
            return queryFilter != null ? And(queryVoucher, Query.ElemMatch("detail", queryFilter)) : queryVoucher;
        }

        /// <summary>
        ///     按细目过滤器和日期查询
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>Bson查询</returns>
        private static IMongoQuery GetQuery(VoucherDetail filter, DateFilter rng)
        {
            var queryVoucher = GetQuery(rng);
            var queryFilter = GetQuery(filter);
            return queryFilter != null ? And(queryVoucher, Query.ElemMatch("detail", queryFilter)) : queryVoucher;
        }

        /// <summary>
        ///     按过滤器、细目过滤器和日期查询
        /// </summary>
        /// <param name="vfilter">过滤器</param>
        /// <param name="filter">细目过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>Bson查询</returns>
        private static IMongoQuery GetQuery(Voucher vfilter, VoucherDetail filter, DateFilter rng)
        {
            var queryVoucher = And(GetQuery(vfilter), GetQuery(rng));
            var queryFilter = GetQuery(filter);
            return queryFilter != null ? And(queryVoucher, Query.ElemMatch("detail", queryFilter)) : queryVoucher;
        }

        /// <summary>
        ///     按资产过滤器查询
        /// </summary>
        /// <param name="filter">资产过滤器</param>
        /// <returns>Bson查询</returns>
        private static IMongoQuery GetQuery(Asset filter)
        {
            if (filter == null)
                return null;

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
            if (filter.DepreciationExpenseTitle != null)
                lst.Add(
                        Query.EQ(
                                 "exptitle",
                                 filter.DepreciationExpenseSubTitle != null
                                     ? filter.DepreciationExpenseTitle * 100 + filter.DepreciationExpenseSubTitle
                                     : filter.DepreciationExpenseTitle));
            if (filter.DevaluationExpenseTitle != null)
                lst.Add(
                        Query.EQ(
                                 "exvtitle",
                                 filter.DevaluationExpenseSubTitle != null
                                     ? filter.DevaluationExpenseTitle * 100 + filter.DevaluationExpenseSubTitle
                                     : filter.DevaluationExpenseTitle));
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

            return And(lst);
        }

        private static IMongoQuery And(params IMongoQuery[] queries)
        {
            var lst = queries.ToList();
            return And(lst);
        }

        private static IMongoQuery And(ICollection<IMongoQuery> queries)
        {
            while (queries.Remove(null)) { }
            return queries.Any() ? Query.And(queries) : null;
        }


        public Voucher SelectVoucher(string id) { return m_Vouchers.FindOneById(ObjectId.Parse(id)); }

        public IEnumerable<Voucher> FilteredSelect(Voucher filter, DateFilter rng)
        {
            return m_Vouchers.Find(And(GetQuery(filter), GetQuery(rng)));
        }

        public bool DeleteVoucher(string id)
        {
            var res = m_Vouchers.Remove(GetUniqueQuery(id));
            return res.DocumentsAffected == 1;
        }

        public long FilteredDelete(Voucher filter)
        {
            var res = m_Vouchers.Remove(GetQuery(filter));
            return res.DocumentsAffected;
        }

        public bool Upsert(Voucher entity)
        {
            var res = m_Vouchers.Save(entity);
            return res.DocumentsAffected <= 1;
        }

        public IEnumerable<Voucher> FilteredSelect(VoucherDetail filter, DateFilter rng)
        {
            return m_Vouchers.Find(GetQuery(filter, rng));
        }

        public IEnumerable<Voucher> FilteredSelect(Voucher vfilter, VoucherDetail filter, DateFilter rng)
        {
            return m_Vouchers.Find(GetQuery(vfilter, filter, rng));
        }

        public IEnumerable<Voucher> FilteredSelect(IEnumerable<VoucherDetail> filters, DateFilter rng)
        {
            return m_Vouchers.Find(
                                   And(
                                       GetQuery(rng),
                                       Query.Or(
                                                filters.Where(filter => filter.Item == null).Select(GetQuery)
                                                       .Where(query => query != null)
                                                       .Select(query => Query.ElemMatch("detail", query)))));
        }

        public IEnumerable<VoucherDetail> FilteredSelectDetails(VoucherDetail filter, DateFilter rng)
        {
            return FilteredSelect(filter, rng).SelectMany(v => v.Details).Where(d => d.IsMatch(filter));
        }

        public long FilteredDelete(Voucher vfilter, VoucherDetail filter)
        {
            var res = m_Vouchers.Remove(GetQuery(vfilter, filter));
            return res.DocumentsAffected;
        }

        public long FilteredDelete(VoucherDetail filter)
        {
            var count = 0L;
            var v = FilteredSelect(filter, DateFilter.Unconstrained);
            foreach (var voucher in v)
            {
                var l = voucher.Details.Count();
                voucher.Details = voucher.Details.Where(d => !d.IsMatch(filter)).ToArray();
                l -= voucher.Details.Count();
                var res = m_Vouchers.Save(voucher.ToBsonDocument());
                if (res.DocumentsAffected == 1)
                    count += l;
            }
            return count;
        }


        public Asset SelectAsset(Guid id) { return m_Assets.FindOne(Query.EQ("_id", id.ToBsonValue())); }

        public IEnumerable<Asset> FilteredSelect(Asset filter) { return m_Assets.Find(GetQuery(filter)); }

        public bool Insert(Asset entity)
        {
            var res = m_Assets.Save(entity);
            return res.DocumentsAffected == 1;
        }

        public bool DeleteAsset(Guid id)
        {
            var res = m_Assets.Remove(GetUniqueQuery(id));
            return res.DocumentsAffected == 1;
        }

        public bool Upsert(Asset entity)
        {
            var res = m_Assets.Save(entity);
            return res.DocumentsAffected == 1;
        }

        public long FilteredDelete(Asset filter)
        {
            var res = m_Assets.Remove(GetQuery(filter));
            return res.DocumentsAffected;
        }
    }
}
