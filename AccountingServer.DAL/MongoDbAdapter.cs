using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using static AccountingServer.DAL.MongoDbQueryHelper;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     MongoDb数据访问类
    /// </summary>
    public class MongoDbAdapter : IDbAdapter
    {
        #region Member

        /// <summary>
        ///     MongoDb客户端
        /// </summary>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private MongoClient m_Client;

        /// <summary>
        ///     MongoDb数据库
        /// </summary>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private IMongoDatabase m_Db;

        /// <summary>
        ///     记账凭证集合
        /// </summary>
        private IMongoCollection<Voucher> m_Vouchers;

        /// <summary>
        ///     资产集合
        /// </summary>
        private IMongoCollection<Asset> m_Assets;

        /// <summary>
        ///     摊销集合
        /// </summary>
        private IMongoCollection<Amortization> m_Amortizations;

        /// <summary>
        ///     命名查询模板集合
        /// </summary>
        private IMongoCollection<BsonDocument> m_NamedQueryTemplates;

        #endregion

        /// <summary>
        ///     注册Bson序列化器
        /// </summary>
        static MongoDbAdapter()
        {
            BsonSerializer.RegisterSerializer(new VoucherSerializer());
            BsonSerializer.RegisterSerializer(new VoucherDetailSerializer());
            BsonSerializer.RegisterSerializer(new AssetSerializer());
            BsonSerializer.RegisterSerializer(new AssetItemSerializer());
            BsonSerializer.RegisterSerializer(new AmortizationSerializer());
            BsonSerializer.RegisterSerializer(new AmortItemSerializer());
            BsonSerializer.RegisterSerializer(new BalanceSerializer());
        }

        #region Server

        /// <inheritdoc />
        public bool Connected { get; private set; }

        /// <inheritdoc />
        public void Connect()
        {
            m_Client = new MongoClient("mongodb://localhost");

            m_Db = m_Client.GetDatabase("accounting");

            m_Vouchers = m_Db.GetCollection<Voucher>("voucher");
            m_Assets = m_Db.GetCollection<Asset>("asset");
            m_Amortizations = m_Db.GetCollection<Amortization>("amortization");
            m_NamedQueryTemplates = m_Db.GetCollection<BsonDocument>("namedQuery");

            Connected = true;
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            if (!Connected)
                return;

            m_Db = null;
            m_Vouchers = null;
            m_Assets = null;
            m_Amortizations = null;
            m_NamedQueryTemplates = null;

            m_Client = null;

            Connected = false;
        }

        #endregion

        #region Voucher

        /// <inheritdoc />
        public Voucher SelectVoucher(string id) =>
            m_Vouchers.FindSync(GetQuery<Voucher>(id)).FirstOrDefault();

        /// <inheritdoc />
        public IEnumerable<Voucher> SelectVouchers(IQueryCompunded<IVoucherQueryAtom> query) =>
            m_Vouchers.Find(GetQuery(query)).ToEnumerable();

        /// <inheritdoc />
        public IEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query)
        {
            const string reduce =
                "function(key, values) { var total = 0; for (var i = 0; i < values.length; i++) total += values[i]; return total; }";
            return
                m_Vouchers.MapReduce<Balance>(GetMapJavascript(query.VoucherEmitQuery, query.Subtotal), reduce)
                          .ToEnumerable();
        }

        /// <inheritdoc />
        public bool DeleteVoucher(string id)
        {
            var res = m_Vouchers.DeleteOne(GetQuery<Voucher>(id));
            return res.DeletedCount == 1;
        }

        /// <inheritdoc />
        public long DeleteVouchers(IQueryCompunded<IVoucherQueryAtom> query)
        {
            var res = m_Vouchers.DeleteMany(GetQuery(query));
            return res.DeletedCount;
        }

        /// <inheritdoc />
        public bool Upsert(Voucher entity) => Upsert(m_Vouchers, entity, new VoucherSerializer());

        #endregion

        #region Asset

        /// <inheritdoc />
        public Asset SelectAsset(Guid id) => m_Assets.FindSync(GetQuery<Asset>(id)).FirstOrDefault();

        /// <inheritdoc />
        public IEnumerable<Asset> SelectAssets(IQueryCompunded<IDistributedQueryAtom> filter) =>
            m_Assets.FindSync(GetQuery<Asset>(filter)).ToEnumerable();

        /// <inheritdoc />
        public bool DeleteAsset(Guid id)
        {
            var res = m_Assets.DeleteOne(GetQuery<Asset>(id));
            return res.DeletedCount == 1;
        }

        /// <inheritdoc />
        public bool Upsert(Asset entity)
        {
            var res = m_Assets.ReplaceOne(
                                          Builders<Asset>.Filter.Eq("_id", entity.ID),
                                          entity,
                                          new UpdateOptions { IsUpsert = true });
            return res.ModifiedCount <= 1;
        }

        /// <inheritdoc />
        public long DeleteAssets(IQueryCompunded<IDistributedQueryAtom> filter)
        {
            var res = m_Assets.DeleteMany(GetQuery<Asset>(filter));
            return res.DeletedCount;
        }

        #endregion

        #region Amortization

        /// <inheritdoc />
        public Amortization SelectAmortization(Guid id) =>
            m_Amortizations.FindSync(GetQuery<Amortization>(id)).FirstOrDefault();

        /// <inheritdoc />
        public IEnumerable<Amortization> SelectAmortizations(IQueryCompunded<IDistributedQueryAtom> filter) =>
            m_Amortizations.FindSync(GetQuery<Amortization>(filter)).ToEnumerable();

        /// <inheritdoc />
        public bool DeleteAmortization(Guid id)
        {
            var res = m_Amortizations.DeleteOne(GetQuery<Amortization>(id));
            return res.DeletedCount == 1;
        }

        /// <inheritdoc />
        public bool Upsert(Amortization entity)
        {
            var res = m_Amortizations.ReplaceOne(
                                                 Builders<Amortization>.Filter.Eq("_id", entity.ID),
                                                 entity,
                                                 new UpdateOptions { IsUpsert = true });
            return res.ModifiedCount <= 1;
        }

        /// <inheritdoc />
        public long DeleteAmortizations(IQueryCompunded<IDistributedQueryAtom> filter)
        {
            var res = m_Amortizations.DeleteMany(GetQuery<Amortization>(filter));
            return res.DeletedCount;
        }

        #endregion

        #region NamedQurey

        /// <inheritdoc />
        public string SelectNamedQueryTemplate(string name)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", new BsonString(name));
            var doc = m_NamedQueryTemplates.FindSync(filter).FirstOrDefault();
            return doc?["value"].AsString;
        }

        /// <inheritdoc />
        public IEnumerable<KeyValuePair<string, string>> SelectNamedQueryTemplates() =>
            m_NamedQueryTemplates
                .FindSync(FilterDefinition<BsonDocument>.Empty)
                .ToEnumerable()
                .Select(d => new KeyValuePair<string, string>(d["_id"].AsString, d["value"].AsString));

        /// <inheritdoc />
        public bool DeleteNamedQueryTemplate(string name)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", new BsonString(name));
            var res = m_NamedQueryTemplates.DeleteOne(filter);
            return res.DeletedCount == 1;
        }

        /// <inheritdoc />
        public bool Upsert(string name, string value)
        {
            var res = m_NamedQueryTemplates.ReplaceOne(
                                                       Builders<BsonDocument>.Filter.Eq("_id", name),
                                                       new BsonDocument
                                                           {
                                                               { "_id", name },
                                                               { "value", value }
                                                           },
                                                       new UpdateOptions { IsUpsert = true });
            return res.ModifiedCount == 1;
        }

        #endregion

        #region Javascript

        /// <summary>
        ///     根据分类要求，将日期进行转化
        /// </summary>
        /// <param name="subtotalLevel">分类汇总层次</param>
        /// <returns>转化的Javascript代码</returns>
        private static string GetTheDateJavascript(SubtotalLevel subtotalLevel)
        {
            var sb = new StringBuilder();
            sb.AppendLine("var theDate = this.date;");
            if (!subtotalLevel.HasFlag(SubtotalLevel.Week))
                return sb.ToString();
            sb.AppendLine("if (theDate != null && theDate != undefined) {");
            sb.AppendLine("    theDate.setHours(0);");
            sb.AppendLine("    theDate.setMinutes(0);");
            sb.AppendLine("    theDate.setSeconds(0);");
            sb.AppendLine();
            if (subtotalLevel.HasFlag(SubtotalLevel.Year))
                sb.AppendLine("    theDate.setMonth(0, 1);");
            else if (subtotalLevel.HasFlag(SubtotalLevel.Month))
                sb.AppendLine("    theDate.setDate(1);");
            else // if (subtotalLevel.HasFlag(SubtotalLevel.Week))
            {
                sb.AppendLine("    if (theDate.getDay() == 0)");
                sb.AppendLine("        theDate.setDate(theDate.getDate() - 6);");
                sb.AppendLine("    else");
                sb.AppendLine("        theDate.setDate(theDate.getDate() + 1 - theDate.getDay());");
            }
            sb.AppendLine("}");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        ///     细目映射检索式的Javascript表示
        /// </summary>
        /// <param name="emitQuery">细目映射检索式</param>
        /// <returns>Javascript表示</returns>
        private static string GetEmitFilterJavascript(IEmit emitQuery)
        {
            if (emitQuery.DetailFilter == null)
                return "function(d) { return true; }";
            return GetJavascriptFilter(emitQuery.DetailFilter);
        }

        /// <summary>
        ///     映射函数的Javascript表示
        /// </summary>
        /// <param name="query">记账凭证检索式</param>
        /// <param name="args">分类汇总层次，若为<c>null</c>表示不汇总</param>
        /// <returns>Javascript表示</returns>
        private static string GetMapJavascript(IVoucherDetailQuery query, ISubtotal args)
        {
            SubtotalLevel level;
            if (args == null)
                level = SubtotalLevel.None;
            else if (args.AggrType != AggregationType.None)
                level = args.Levels.Aggregate(SubtotalLevel.None, (total, l) => total | l) | SubtotalLevel.Day;
            else
                level = args.Levels.Aggregate(SubtotalLevel.None, (total, l) => total | l);

            var sb = new StringBuilder();
            sb.AppendLine("function() {");
            sb.AppendLine("    var chk = ");
            if (query.DetailEmitFilter != null)
                sb.Append(GetEmitFilterJavascript(query.DetailEmitFilter));
            else
            {
                var dQuery = query.VoucherQuery as IVoucherQueryAtom;
                if (dQuery == null)
                    throw new ArgumentException("不指定细目映射检索式时记账凭证检索式为复合检索式", nameof(query));
                sb.Append(GetJavascriptFilter(dQuery.DetailFilter));
            }
            sb.AppendLine(";");
            sb.AppendLine("    if ((");
            sb.Append(GetJavascriptFilter(query.VoucherQuery));
            sb.AppendLine(")(this)) {");
            sb.AppendLine(GetTheDateJavascript(level));
            sb.AppendLine("        this.detail.forEach(function(d) {");
            sb.AppendLine("            if (chk(d))");
            {
                if (args == null)
                    sb.Append("emit(d, 0);");
                else
                {
                    sb.Append("emit({");
                    if (level.HasFlag(SubtotalLevel.Day))
                        sb.Append("date: theDate,");
                    if (level.HasFlag(SubtotalLevel.Title))
                        sb.Append("title: d.title,");
                    if (level.HasFlag(SubtotalLevel.SubTitle))
                        sb.Append("subtitle: d.subtitle,");
                    if (level.HasFlag(SubtotalLevel.Content))
                        sb.Append("content: d.content,");
                    if (level.HasFlag(SubtotalLevel.Remark))
                        sb.Append("remark: d.remark,");
                    sb.Append(args.GatherType == GatheringType.Count ? "}, 1);" : "}, d.fund);");
                }
            }
            sb.AppendLine("        });");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.Replace(Environment.NewLine, string.Empty).ToString();
        }

        #endregion

        private static bool Upsert<T, TId>(IMongoCollection<T> collection, T entity, BaseSerializer<T, TId> idProvider)
        {
            if (idProvider.FillId(collection, entity))
            {
                collection.InsertOne(entity);
                return true;
            }

            var res = collection.ReplaceOne(
                                            Builders<T>.Filter.Eq("_id", idProvider.GetId(entity)),
                                            entity,
                                            new UpdateOptions { IsUpsert = true });
            return res.ModifiedCount <= 1;
        }
    }
}
