/* Copyright (C) 2020-2021 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

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
        private static readonly BsonDocument ProjectDetails = new() { ["_id"] = false, ["detail"] = true };

        private static readonly BsonDocument ProjectDate = new()
            {
                ["_id"] = false, ["detail"] = true, ["date"] = true,
            };

        private static readonly BsonDocument ProjectNothing = new() { ["_id"] = false };

        private static readonly BsonDocument ProjectNothingButDate = new()
            {
                ["_id"] = false, ["date"] = true,
            };

        private static readonly BsonDocument ProjectDetail = new()
            {
                ["user"] = "$detail.user",
                ["currency"] = "$detail.currency",
                ["title"] = "$detail.title",
                ["subtitle"] = "$detail.subtitle",
                ["content"] = "$detail.content",
                ["fund"] = "$detail.fund",
                ["remark"] = "$detail.remark",
            };

        private static readonly BsonDocument ProjectYear;
        private static readonly BsonDocument ProjectMonth;
        private static readonly BsonDocument ProjectWeek;

        private static readonly BsonDocument ProjectNothingButYear;
        private static readonly BsonDocument ProjectNothingButMonth;
        private static readonly BsonDocument ProjectNothingButWeek;

        private static readonly BsonDocument FilterNonZero = new()
            {
                ["total"] = new BsonDocument
                    {
                        ["$not"] = new BsonDocument
                            {
                                ["$gt"] = -VoucherDetail.Tolerance, ["$lt"] = +VoucherDetail.Tolerance,
                            },
                    },
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
            BsonSerializer.RegisterSerializer(new ExchangeSerializer());

            static BsonDocument MakeProject(BsonValue days, bool detail)
            {
                var proj = new BsonDocument
                    {
                        ["_id"] = false,
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
                                                                                ["$type"] = "$date",
                                                                            },
                                                                        "missing",
                                                                    },
                                                            },
                                                        ["then"] = BsonNull.Value,
                                                    },
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
                                                                        new BsonArray { days, 24 * 60 * 60 * 1000 },
                                                                },
                                                        },
                                            },
                                    },
                            },
                    };
                if (detail)
                    proj["detail"] = true;
                return proj;
            }

            var year = new BsonDocument
                {
                    ["$subtract"] = new BsonArray { new BsonDocument { ["$dayOfYear"] = "$date" }, 1 },
                };
            var month = new BsonDocument
                {
                    ["$subtract"] = new BsonArray { new BsonDocument { ["$dayOfMonth"] = "$date" }, 1 },
                };
            var week = new BsonDocument
                {
                    ["$mod"] = new BsonArray
                        {
                            new BsonDocument
                                {
                                    ["$add"] = new BsonArray
                                        {
                                            new BsonDocument { ["$dayOfWeek"] = "$date" }, 5,
                                        },
                                },
                            7,
                        },
                };

            ProjectYear = MakeProject(year, true);
            ProjectMonth = MakeProject(month, true);
            ProjectWeek = MakeProject(week, true);

            ProjectNothingButYear = MakeProject(year, false);
            ProjectNothingButMonth = MakeProject(month, false);
            ProjectNothingButWeek = MakeProject(week, false);
        }

        public MongoDbAdapter(string uri, string db = null)
        {
            var url = new MongoUrl(uri);
            m_Client = new(MongoClientSettings.FromUrl(url));

            m_Db = m_Client.GetDatabase(db ?? url.DatabaseName ?? "accounting");

            m_Vouchers = m_Db.GetCollection<Voucher>("voucher");
            m_Assets = m_Db.GetCollection<Asset>("asset");
            m_Amortizations = m_Db.GetCollection<Amortization>("amortization");
            m_Records = m_Db.GetCollection<ExchangeRecord>("exchangeRecord");
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
                new ReplaceOptions { IsUpsert = true });
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

        /// <summary>
        ///     汇率集合
        /// </summary>
        private readonly IMongoCollection<ExchangeRecord> m_Records;

        #endregion

        #region Voucher

        /// <inheritdoc />
        public Voucher SelectVoucher(string id) =>
            m_Vouchers.FindSync(GetNQuery<Voucher>(id)).FirstOrDefault();

        /// <inheritdoc />
        public IEnumerable<Voucher> SelectVouchers(IQueryCompounded<IVoucherQueryAtom> query) =>
            m_Vouchers.Find(query.Accept(new MongoDbNativeVoucher())).Sort(Builders<Voucher>.Sort.Ascending("date"))
                .ToEnumerable();

        private static FilterDefinition<BsonDocument> GetChk(IVoucherDetailQuery query)
        {
            var visitor = new MongoDbNativeDetailUnwinded();
            return query.DetailEmitFilter != null
                ? query.DetailEmitFilter.DetailFilter.Accept(visitor)
                : query.VoucherQuery is IVoucherQueryAtom dQuery
                    ? dQuery.DetailFilter.Accept(visitor)
                    : throw new ArgumentException("不指定细目映射检索式时记账凭证检索式为复合检索式", nameof(query));
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
        public IEnumerable<Balance> SelectVouchersGrouped(IVoucherGroupedQuery query)
        {
            if (query.Subtotal.GatherType != GatheringType.VoucherCount)
                throw new InvalidOperationException("记账凭证分类汇总只能计数");
            if (query.Subtotal.EquivalentDate.HasValue)
                throw new InvalidOperationException("记账凭证分类汇总不能等值");

            var level = query.Subtotal.Levels.Aggregate(SubtotalLevel.None, (total, l) => total | l);
            if (query.Subtotal.AggrType != AggregationType.None)
                level |= query.Subtotal.AggrInterval;

            if (level.HasFlag(SubtotalLevel.User))
                throw new InvalidOperationException("记账凭证不能按用户分类汇总");
            if (level.HasFlag(SubtotalLevel.Currency))
                throw new InvalidOperationException("记账凭证不能按币种分类汇总");
            if (level.HasFlag(SubtotalLevel.Title))
                throw new InvalidOperationException("记账凭证不能按一级科目分类汇总");
            if (level.HasFlag(SubtotalLevel.SubTitle))
                throw new InvalidOperationException("记账凭证不能按二级科目分类汇总");
            if (level.HasFlag(SubtotalLevel.Content))
                throw new InvalidOperationException("记账凭证不能按内容分类汇总");
            if (level.HasFlag(SubtotalLevel.Remark))
                throw new InvalidOperationException("记账凭证不能按备注分类汇总");

            var preF = query.VoucherQuery.Accept(new MongoDbNativeVoucher());

            BsonDocument pprj;
            if (!level.HasFlag(SubtotalLevel.Day))
                pprj = ProjectNothing;
            else if (!level.HasFlag(SubtotalLevel.Week))
                pprj = ProjectNothingButDate;
            else if (level.HasFlag(SubtotalLevel.Year))
                pprj = ProjectNothingButYear;
            else if (level.HasFlag(SubtotalLevel.Month))
                pprj = ProjectNothingButMonth;
            else // if (level.HasFlag(SubtotalLevel.Week))
                pprj = ProjectNothingButWeek;

            var prj = new BsonDocument();
            if (level.HasFlag(SubtotalLevel.Day))
                prj["date"] = "$date";

            var grp = new BsonDocument { ["_id"] = prj, ["count"] = new BsonDocument { ["$sum"] = 1 } };

            var fluent = m_Vouchers.Aggregate().Match(preF).Project(pprj).Group(grp);
            return fluent.ToEnumerable().Select(b => BsonSerializer.Deserialize<Balance>(b));
        }

        /// <inheritdoc />
        public IEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query)
        {
            var level = query.Subtotal.Levels.Aggregate(SubtotalLevel.None, (total, l) => total | l);
            if (query.Subtotal.AggrType != AggregationType.None)
                level |= query.Subtotal.AggrInterval;
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
            if (level.HasFlag(SubtotalLevel.User))
                prj["user"] = "$detail.user";
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
                grp = new BsonDocument { ["_id"] = prj, ["count"] = new BsonDocument { ["$sum"] = 1 } };
            else
                grp = new BsonDocument { ["_id"] = prj, ["total"] = new BsonDocument { ["$sum"] = "$detail.fund" } };

            var fluent = m_Vouchers.Aggregate().Match(preF).Project(pprj).Unwind("detail").Match(chk).Group(grp);
            if (query.Subtotal.AggrType != AggregationType.ChangedDay &&
                query.Subtotal.Levels.LastOrDefault().HasFlag(SubtotalLevel.NonZero))
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
        public long DeleteVouchers(IQueryCompounded<IVoucherQueryAtom> query)
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
        public IEnumerable<Asset> SelectAssets(IQueryCompounded<IDistributedQueryAtom> filter) =>
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
                new ReplaceOptions { IsUpsert = true });
            return res.ModifiedCount <= 1;
        }

        /// <inheritdoc />
        public long DeleteAssets(IQueryCompounded<IDistributedQueryAtom> filter)
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
        public IEnumerable<Amortization> SelectAmortizations(IQueryCompounded<IDistributedQueryAtom> filter) =>
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
                new ReplaceOptions { IsUpsert = true });
            return res.ModifiedCount <= 1;
        }

        /// <inheritdoc />
        public long DeleteAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
        {
            var res = m_Amortizations.DeleteMany(filter.Accept(new MongoDbNativeDistributed<Amortization>()));
            return res.DeletedCount;
        }

        #endregion

        #region ExchangeRecord

        /// <inheritdoc />
        public ExchangeRecord SelectExchangeRecord(ExchangeRecord record) =>
            m_Records.FindSync(
                Builders<ExchangeRecord>.Filter.Eq("_id.date", record.Date) &
                Builders<ExchangeRecord>.Filter.Eq("_id.from", record.From) &
                Builders<ExchangeRecord>.Filter.Eq("_id.to", record.To)
            ).SingleOrDefault();

        /// <inheritdoc />
        public bool Upsert(ExchangeRecord record)
        {
            try
            {
                m_Records.InsertOne(record);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        #endregion
    }
}
