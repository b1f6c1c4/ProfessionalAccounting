using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AccountingServer.DAL.Serializer;
using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using static AccountingServer.DAL.MongoDbNative;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     MongoDb数据访问类
    /// </summary>
    [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
    internal class MongoDbAdapter : IDbAdapter
    {
        private static readonly BsonDocument ProjectDetails = new BsonDocument
            {
                ["_id"] = false,
                ["detail"] = true
            };

        private static readonly BsonDocument ProjectDate = new BsonDocument
            {
                ["_id"] = false,
                ["detail"] = true,
                ["date"] = true
            };

        private static readonly BsonDocument ProjectDetail = new BsonDocument
            {
                ["currency"] = "$detail.currency",
                ["title"] = "$detail.title",
                ["subtitle"] = "$detail.subtitle",
                ["content"] = "$detail.content",
                ["fund"] = "$detail.fund",
                ["remark"] = "$detail.remark"
            };

        private static readonly BsonDocument ProjectYear;
        private static readonly BsonDocument ProjectMonth;
        private static readonly BsonDocument ProjectWeek;

        private static readonly BsonDocument FilterNonZero = new BsonDocument
            {
                ["total"] = new BsonDocument
                    {
                        ["$not"] = new BsonDocument
                            {
                                ["$gt"] = -VoucherDetail.Tolerance,
                                ["$lt"] = +VoucherDetail.Tolerance
                            }
                    }
            };

        static MongoDbAdapter()
        {
            BsonSerializer.RegisterSerializer(new VoucherSerializer());
            BsonSerializer.RegisterSerializer(new VoucherDetailSerializer());
            BsonSerializer.RegisterSerializer(new AssetSerializer());
            BsonSerializer.RegisterSerializer(new AssetItemSerializer());
            BsonSerializer.RegisterSerializer(new AmortizationSerializer());
            BsonSerializer.RegisterSerializer(new AmortItemSerializer());
            BsonSerializer.RegisterSerializer(new BalanceSerializer());

            BsonDocument MakeProject(BsonValue days)
                => new BsonDocument
                    {
                        ["_id"] = false,
                        ["detail"] = true,
                        ["date"] = new BsonDocument
                            {
                                ["$switch"] = new BsonDocument
                                    {
                                        ["branches"] = new BsonArray
                                            {
                                                new BsonDocument
                                                    {
                                                        ["case"] = new BsonDocument
                                                            {
                                                                ["$eq"] = new BsonArray
                                                                    {
                                                                        new BsonDocument
                                                                            {
                                                                                ["$type"] = "$date"
                                                                            },
                                                                        "missing"
                                                                    }
                                                            },
                                                        ["then"] = BsonNull.Value
                                                    }
                                            },
                                        ["default"] = new BsonDocument
                                            {
                                                ["$subtract"] =
                                                    new BsonArray
                                                        {
                                                            "$date",
                                                            new BsonDocument
                                                                {
                                                                    ["$multiply"] =
                                                                        new BsonArray
                                                                            {
                                                                                days,
                                                                                24 * 60 * 60 * 1000
                                                                            }
                                                                }
                                                        }
                                            }
                                    }
                            }
                    };

            ProjectYear = MakeProject(
                new BsonDocument
                    {
                        ["$subtract"] = new BsonArray
                            {
                                new BsonDocument
                                    {
                                        ["$dayOfYear"] = "$date"
                                    },
                                1
                            }
                    });
            ProjectMonth = MakeProject(
                new BsonDocument
                    {
                        ["$subtract"] = new BsonArray
                            {
                                new BsonDocument
                                    {
                                        ["$dayOfMonth"] = "$date"
                                    },
                                1
                            }
                    });
            ProjectWeek = MakeProject(
                new BsonDocument
                    {
                        ["$mod"] = new BsonArray
                            {
                                new BsonDocument
                                    {
                                        ["$add"] = new BsonArray
                                            {
                                                new BsonDocument
                                                    {
                                                        ["$dayOfWeek"] = "$date"
                                                    },
                                                5
                                            }
                                    },
                                7
                            }
                    });
        }

        public MongoDbAdapter(string uri)
        {
            var url = new MongoUrl(uri);
            m_Client = new MongoClient(MongoClientSettings.FromUrl(url));

            m_Db = m_Client.GetDatabase(url.DatabaseName ?? "accounting");

            m_Vouchers = m_Db.GetCollection<Voucher>("voucher");
            m_Assets = m_Db.GetCollection<Asset>("asset");
            m_Amortizations = m_Db.GetCollection<Amortization>("amortization");
        }

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

        #region Voucher

        /// <inheritdoc />
        public Voucher SelectVoucher(string id) =>
            m_Vouchers.FindSync(GetNQuery<Voucher>(id)).FirstOrDefault();

        /// <inheritdoc />
        public IEnumerable<Voucher> SelectVouchers(IQueryCompunded<IVoucherQueryAtom> query) =>
            m_Vouchers.Find(query.Accept(new MongoDbNativeVoucher())).Sort(Builders<Voucher>.Sort.Ascending("date"))
                .ToEnumerable();

        private static FilterDefinition<BsonDocument> GetChk(IVoucherDetailQuery query)
        {
            var visitor = new MongoDbNativeDetailUnwinded();
            return query.DetailEmitFilter != null
                ? query.DetailEmitFilter.DetailFilter.Accept(visitor)
                : (query.VoucherQuery is IVoucherQueryAtom dQuery
                    ? dQuery.DetailFilter.Accept(visitor)
                    : throw new ArgumentException("不指定细目映射检索式时记账凭证检索式为复合检索式", nameof(query)));
        }

        /// <inheritdoc />
        public IEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query)
        {
            var preF = query.VoucherQuery.Accept(new MongoDbNativeVoucher());
            var chk = GetChk(query);
            var srt = Builders<Voucher>.Sort.Ascending("date");
            return m_Vouchers.Aggregate().Match(preF).Sort(srt).Project(ProjectDetails).Unwind("detail").Match(chk)
                .Project(ProjectDetail).ToEnumerable().Select(b => BsonSerializer.Deserialize<VoucherDetail>(b));
        }

        /// <inheritdoc />
        public IEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query)
        {
            var level = query.Subtotal.Levels.Aggregate(SubtotalLevel.None, (total, l) => total | l);
            if (query.Subtotal.AggrType != AggregationType.None)
                level |= SubtotalLevel.Day;
            if (query.Subtotal.EquivalentDate.HasValue)
                level |= SubtotalLevel.Currency;

            var preF = query.VoucherEmitQuery.VoucherQuery.Accept(new MongoDbNativeVoucher());
            var chk = GetChk(query.VoucherEmitQuery);

            BsonDocument pprj;
            if (!level.HasFlag(SubtotalLevel.Day))
                pprj = ProjectDetails;
            else if (!level.HasFlag(SubtotalLevel.Week))
                pprj = ProjectDate;
            else if (level.HasFlag(SubtotalLevel.Year))
                pprj = ProjectYear;
            else if (level.HasFlag(SubtotalLevel.Month))
                pprj = ProjectMonth;
            else // if (level.HasFlag(SubtotalLevel.Week))
                pprj = ProjectWeek;

            var prj = new BsonDocument();
            if (level.HasFlag(SubtotalLevel.Day))
                prj["date"] = "$date";
            if (level.HasFlag(SubtotalLevel.Currency))
                prj["currency"] = "$detail.currency";
            if (level.HasFlag(SubtotalLevel.Title))
                prj["title"] = "$detail.title";
            if (level.HasFlag(SubtotalLevel.SubTitle))
                prj["subtitle"] = "$detail.subtitle";
            if (level.HasFlag(SubtotalLevel.Content))
                prj["content"] = "$detail.content";
            if (level.HasFlag(SubtotalLevel.Remark))
                prj["remark"] = "$detail.remark";

            BsonDocument grp;
            if (query.Subtotal.GatherType == GatheringType.Count)
                grp = new BsonDocument
                    {
                        ["_id"] = prj,
                        ["count"] = new BsonDocument { ["$sum"] = 1 }
                    };
            else
                grp = new BsonDocument
                    {
                        ["_id"] = prj,
                        ["total"] = new BsonDocument { ["$sum"] = "$detail.fund" }
                    };

            var fluent = m_Vouchers.Aggregate().Match(preF).Project(pprj).Unwind("detail").Match(chk).Group(grp);
            if (query.Subtotal.AggrType != AggregationType.ChangedDay &&
                query.Subtotal.GatherType == GatheringType.NonZero)
                fluent = fluent.Match(FilterNonZero);
            return fluent.ToEnumerable().Select(b => BsonSerializer.Deserialize<Balance>(b));
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
            var res = m_Vouchers.DeleteMany(query.Accept(new MongoDbNativeVoucher()));
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
            m_Assets.FindSync(filter.Accept(new MongoDbNativeDistributed<Asset>()))
                .ToEnumerable();

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
            var res = m_Assets.DeleteMany(filter.Accept(new MongoDbNativeDistributed<Asset>()));
            return res.DeletedCount;
        }

        #endregion

        #region Amortization

        /// <inheritdoc />
        public Amortization SelectAmortization(Guid id) =>
            m_Amortizations.FindSync(GetNQuery<Amortization>(id)).FirstOrDefault();

        /// <inheritdoc />
        public IEnumerable<Amortization> SelectAmortizations(IQueryCompunded<IDistributedQueryAtom> filter) =>
            m_Amortizations.FindSync(filter.Accept(new MongoDbNativeDistributed<Amortization>()))
                .ToEnumerable();

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
            var res = m_Amortizations.DeleteMany(filter.Accept(new MongoDbNativeDistributed<Amortization>()));
            return res.DeletedCount;
        }

        #endregion
    }
}
