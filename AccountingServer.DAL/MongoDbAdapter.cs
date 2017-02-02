using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using AccountingServer.DAL.Serializer;
using AccountingServer.Entities;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using static AccountingServer.DAL.MongoDbNative;
using static AccountingServer.DAL.MongoDbJavascript;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     MongoDb数据访问类
    /// </summary>
    [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
    internal class MongoDbAdapter : IDbAdapter
    {
        #region Member

        /// <summary>
        ///     MongoDb客户端
        /// </summary>
        private readonly MongoClient m_Client;

        /// <summary>
        ///     MongoDb数据库
        /// </summary>
        private readonly IMongoDatabase m_Db;

        /// <summary>
        ///     记账凭证集合
        /// </summary>
        private readonly IMongoCollection<Voucher> m_Vouchers;

        /// <summary>
        ///     资产集合
        /// </summary>
        private readonly IMongoCollection<Asset> m_Assets;

        /// <summary>
        ///     摊销集合
        /// </summary>
        private readonly IMongoCollection<Amortization> m_Amortizations;

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

        public MongoDbAdapter(MongoClientSettings settings)
        {
            m_Client = new MongoClient(settings);

            m_Db = m_Client.GetDatabase("accounting");

            m_Vouchers = m_Db.GetCollection<Voucher>("voucher");
            m_Assets = m_Db.GetCollection<Asset>("asset");
            m_Amortizations = m_Db.GetCollection<Amortization>("amortization");
        }

        #region Voucher

        /// <inheritdoc />
        public Voucher SelectVoucher(string id) =>
            m_Vouchers.FindSync(GetNQuery<Voucher>(id)).FirstOrDefault();

        /// <inheritdoc />
        public IEnumerable<Voucher> SelectVouchers(IQueryCompunded<IVoucherQueryAtom> query) =>
            m_Vouchers.Find(GetNQuery(query)).ToEnumerable();

        /// <inheritdoc />
        public IEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query)
        {
            const string reduce =
                "function(key, values) { return Array.sum(values); }";
            FilterDefinition<Voucher> preFilter;
            var map = GetMapJavascript(query.VoucherEmitQuery, query.Subtotal, out preFilter);
            var options = new MapReduceOptions<Voucher, Balance> { Filter = preFilter };
            var balances = m_Vouchers.MapReduce(map, reduce, options).ToEnumerable();
            if (query.Subtotal.Levels.Contains(SubtotalLevel.Currency))
                return balances
                    .Select(
                        b =>
                        {
                            b.Currency = b.Currency ?? Voucher.BaseCurrency;
                            return b;
                        });

            return balances;
        }

        /// <inheritdoc />
        public bool DeleteVoucher(string id)
        {
            var res = m_Vouchers.DeleteOne(GetNQuery<Voucher>(id));
            return res.DeletedCount == 1;
        }

        /// <inheritdoc />
        public long DeleteVouchers(IQueryCompunded<IVoucherQueryAtom> query)
        {
            var res = m_Vouchers.DeleteMany(GetNQuery(query));
            return res.DeletedCount;
        }

        /// <inheritdoc />
        public bool Upsert(Voucher entity) => Upsert(m_Vouchers, entity, new VoucherSerializer());

        #endregion

        #region Asset

        /// <inheritdoc />
        public Asset SelectAsset(Guid id) => m_Assets.FindSync(GetNQuery<Asset>(id)).FirstOrDefault();

        /// <inheritdoc />
        public IEnumerable<Asset> SelectAssets(IQueryCompunded<IDistributedQueryAtom> filter) =>
            m_Assets.FindSync(GetNQuery<Asset>(filter)).ToEnumerable();

        /// <inheritdoc />
        public bool DeleteAsset(Guid id)
        {
            var res = m_Assets.DeleteOne(GetNQuery<Asset>(id));
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
            var res = m_Assets.DeleteMany(GetNQuery<Asset>(filter));
            return res.DeletedCount;
        }

        #endregion

        #region Amortization

        /// <inheritdoc />
        public Amortization SelectAmortization(Guid id) =>
            m_Amortizations.FindSync(GetNQuery<Amortization>(id)).FirstOrDefault();

        /// <inheritdoc />
        public IEnumerable<Amortization> SelectAmortizations(IQueryCompunded<IDistributedQueryAtom> filter) =>
            m_Amortizations.FindSync(GetNQuery<Amortization>(filter)).ToEnumerable();

        /// <inheritdoc />
        public bool DeleteAmortization(Guid id)
        {
            var res = m_Amortizations.DeleteOne(GetNQuery<Amortization>(id));
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
            var res = m_Amortizations.DeleteMany(GetNQuery<Amortization>(filter));
            return res.DeletedCount;
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
        private static string GetEmitFilterJavascript(IEmit emitQuery) =>
            GetJavascriptFilter(emitQuery.DetailFilter);

        /// <summary>
        ///     映射函数的Javascript表示
        /// </summary>
        /// <param name="query">记账凭证检索式</param>
        /// <param name="args">分类汇总层次，若为<c>null</c>表示不汇总</param>
        /// <param name="preFilter">前置Native表示</param>
        /// <returns>Javascript表示</returns>
        private static string GetMapJavascript(IVoucherDetailQuery query, ISubtotal args,
            out FilterDefinition<Voucher> preFilter)
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
            preFilter = GetNativeFilter(query.VoucherQuery);
            sb.AppendLine(GetTheDateJavascript(level));
            sb.AppendLine("    var theCurrency = this.currency;");
            sb.AppendLine("    this.detail.forEach(function(d) {");
            sb.AppendLine("        if (chk(d))");
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
                    if (level.HasFlag(SubtotalLevel.Currency))
                        sb.Append("currency: theCurrency,");
                    sb.Append(args.GatherType == GatheringType.Count ? "}, 1);" : "}, d.fund);");
                }
            }
            sb.AppendLine("    });");
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
