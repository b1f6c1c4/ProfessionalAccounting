/* Copyright (C) 2020-2024 b1f6c1c4
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
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AccountingServer.DAL.Serializer;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using static AccountingServer.DAL.MongoDbNative;

namespace AccountingServer.DAL;

/// <summary>
///     MongoDb数据访问类
/// </summary>
[SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
internal class MongoDbAdapter : IDbAdapter
{
    private static readonly BsonDocument ProjectDetails = new() { ["_id"] = false, ["detail"] = true };

    private static readonly BsonDocument ProjectDate = new() { ["_id"] = false, ["detail"] = true, ["date"] = true };

    private static readonly BsonDocument ProjectNothing = new() { ["_id"] = false };

    private static readonly BsonDocument ProjectNothingButDate = new() { ["_id"] = false, ["date"] = true };

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
    private static readonly BsonDocument ProjectQuarter;
    private static readonly BsonDocument ProjectMonth;
    private static readonly BsonDocument ProjectWeek;

    private static readonly BsonDocument ProjectNothingButYear;
    private static readonly BsonDocument ProjectNothingButQuarter;
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

        static BsonDocument MakeProject(BsonValue subs, bool detail)
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
                                            ["$dateSubtract"] = subs,
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
                ["startDate"] = "$date",
                ["unit"] = "day",
                ["amount"] = new BsonDocument
                    {
                        ["$subtract"] = new BsonArray { new BsonDocument { ["$dayOfYear"] = "$date" }, 1 },
                    },
            };
        var monthOfYear = new BsonDocument
            {
                ["$getField"] = new BsonDocument
                    {
                        ["field"] = "month",
                        ["input"] = new BsonDocument
                            {
                                ["$dateToParts"] = new BsonDocument { ["date"] = "$date" },
                            }
                    }
            };
        var quarter = new BsonDocument
            {
                ["startDate"] = new BsonDocument
                    {
                        ["$dateSubtract"] = new BsonDocument
                            {
                                ["startDate"] = "$date",
                                ["unit"] = "day",
                                ["amount"] = new BsonDocument
                                {
                                    ["$subtract"] = new BsonArray { new BsonDocument { ["$dayOfMonth"] = "$date" }, 1 },
                                },
                            },
                    },
                ["unit"] = "month",
                ["amount"] = new BsonDocument
                    {
                        ["$mod"] = new BsonArray
                            {
                                new BsonDocument
                                    {
                                        ["$subtract"] = new BsonArray { monthOfYear, 1 },
                                    },
                                3,
                            },
                    },
            };
        var month = new BsonDocument
            {
                ["startDate"] = "$date",
                ["unit"] = "day",
                ["amount"] = new BsonDocument
                    {
                        ["$subtract"] = new BsonArray { new BsonDocument { ["$dayOfMonth"] = "$date" }, 1 },
                    },
            };
        var week = new BsonDocument
            {
                ["startDate"] = "$date",
                ["unit"] = "day",
                ["amount"] = new BsonDocument
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
                    },
            };

        ProjectYear = MakeProject(year, true);
        ProjectQuarter = MakeProject(quarter, true);
        ProjectMonth = MakeProject(month, true);
        ProjectWeek = MakeProject(week, true);

        ProjectNothingButYear = MakeProject(year, false);
        ProjectNothingButQuarter = MakeProject(quarter, false);
        ProjectNothingButMonth = MakeProject(month, false);
        ProjectNothingButWeek = MakeProject(week, false);
    }

    public MongoDbAdapter(string uri, string db = null, string x509 = null)
    {
        var url = new MongoUrl(uri);
        var settings = MongoClientSettings.FromUrl(url);
        if (!string.IsNullOrEmpty(x509))
        {
            var cert = X509Certificate2.CreateFromPemFile(x509);
            settings.SslSettings.ClientCertificates = new[] { cert };
        }

        m_Client = new(settings);

        m_Db = m_Client.GetDatabase(db ?? url.DatabaseName ?? "accounting");

        m_Vouchers = m_Db.GetCollection<Voucher>("voucher");
        m_Assets = m_Db.GetCollection<Asset>("asset");
        m_Amortizations = m_Db.GetCollection<Amortization>("amortization");
        m_Records = m_Db.GetCollection<ExchangeRecord>("exchangeRecord");
    }

    private static async ValueTask<bool> Upsert<T, TId>(IMongoCollection<T> collection, T entity,
        BaseSerializer<T, TId> idProvider)
    {
        if (idProvider.FillId(collection, entity))
        {
            await collection.InsertOneAsync(entity);
            return true;
        }

        var res = await collection.ReplaceOneAsync(
            Builders<T>.Filter.Eq("_id", idProvider.GetId(entity)),
            entity,
            new ReplaceOptions { IsUpsert = true });
        return res.ModifiedCount <= 1;
    }

    private static async ValueTask<long> Upsert<T, TId>(IMongoCollection<T> collection, IEnumerable<T> entities,
        BaseSerializer<T, TId> idProvider)
    {
        var ops = new List<WriteModel<T>>();
        foreach (var entity in entities)
            if (idProvider.FillId(collection, entity))
                ops.Add(new InsertOneModel<T>(entity));
            else
                ops.Add(new ReplaceOneModel<T>(Builders<T>.Filter.Eq("_id", idProvider.GetId(entity)), entity)
                    {
                        IsUpsert = true,
                    });
        if (ops.Count == 0)
            return 0L;

        var res = await collection.BulkWriteAsync(ops, new() { IsOrdered = false });
        return res.ProcessedRequests.Count;
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
    public async ValueTask<Voucher> SelectVoucher(string id) =>
        (await m_Vouchers.FindAsync(GetNQuery<Voucher>(id))).FirstOrDefault();

    /// <inheritdoc />
    public IAsyncEnumerable<Voucher> SelectVouchers(IQueryCompounded<IVoucherQueryAtom> query, IEnumerable<string> exclude = null) =>
        m_Vouchers.Find(query.Accept(new MongoDbNativeVoucher()) & GetXQuery<Voucher>(exclude))
            .Sort(Builders<Voucher>.Sort.Ascending("date")).ToAsyncEnumerable();

    private static FilterDefinition<BsonDocument> GetChk(IVoucherDetailQuery query)
        => query.ActualDetailFilter().Accept(new MongoDbNativeDetailUnwinded());

    /// <inheritdoc />
    public IAsyncEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query, IEnumerable<string> exclude = null)
    {
        var preF = query.VoucherQuery.Accept(new MongoDbNativeVoucher()) & GetXQuery<Voucher>(exclude);
        var chk = GetChk(query);
        var srt = Builders<Voucher>.Sort.Ascending("date");
        return m_Vouchers.Aggregate().Match(preF).Sort(srt).Project(ProjectDetails).Unwind("detail").Match(chk)
            .Project(ProjectDetail).ToAsyncEnumerable()
            .Select(static b => BsonSerializer.Deserialize<VoucherDetail>(b));
    }

    /// <inheritdoc />
    public IAsyncEnumerable<Balance> SelectVouchersGrouped(IVoucherGroupedQuery query, int limit = 0, IEnumerable<string> exclude = null)
    {
        var level = query.Subtotal.PreprocessVoucher();
        var preF = query.VoucherQuery.Accept(new MongoDbNativeVoucher()) & GetXQuery<Voucher>(exclude);

        BsonDocument pprj;
        if (!level.HasFlag(SubtotalLevel.Day))
            pprj = ProjectNothing;
        else if (!level.HasFlag(SubtotalLevel.Week))
            pprj = ProjectNothingButDate;
        else if (level.HasFlag(SubtotalLevel.Year))
            pprj = ProjectNothingButYear;
        else if (level.HasFlag(SubtotalLevel.Quarter))
            pprj = ProjectNothingButQuarter;
        else if (level.HasFlag(SubtotalLevel.Month))
            pprj = ProjectNothingButMonth;
        else // if (level.HasFlag(SubtotalLevel.Week))
            pprj = ProjectNothingButWeek;

        var prj = new BsonDocument();
        if (level.HasFlag(SubtotalLevel.Day))
            prj["date"] = "$date";

        var grp = new BsonDocument { ["_id"] = prj, ["count"] = new BsonDocument { ["$sum"] = 1 } };

        var fluent = m_Vouchers.Aggregate().Match(preF).Project(pprj).Group(grp);
        if (limit > 0)
            fluent = fluent.Sort(new SortDefinitionBuilder<BsonDocument>().Descending("count")).Limit(limit);
        return fluent.ToAsyncEnumerable().Select(static b => BsonSerializer.Deserialize<Balance>(b));
    }

    /// <inheritdoc />
    public IAsyncEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query, int limit = 0, IEnumerable<string> exclude = null)
    {
        var level = query.Subtotal.PreprocessDetail();
        var preF = query.VoucherEmitQuery.VoucherQuery.Accept(new MongoDbNativeVoucher()) & GetXQuery<Voucher>(exclude);
        var chk = GetChk(query.VoucherEmitQuery);

        BsonDocument pprj;
        if (!level.HasFlag(SubtotalLevel.Day))
            pprj = ProjectDetails;
        else if (!level.HasFlag(SubtotalLevel.Week))
            pprj = ProjectDate;
        else if (level.HasFlag(SubtotalLevel.Year))
            pprj = ProjectYear;
        else if (level.HasFlag(SubtotalLevel.Quarter))
            pprj = ProjectQuarter;
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
        if (level.HasFlag(SubtotalLevel.Value))
            prj["value"] = new BsonDocument
                {
                    ["$round"] = new BsonArray { "$detail.fund", -(int)Math.Log10(VoucherDetail.Tolerance) },
                };

        BsonDocument grp;
        if (query.Subtotal.GatherType == GatheringType.Count)
            grp = new() { ["_id"] = prj, ["count"] = new BsonDocument { ["$sum"] = 1 } };
        else
            grp = new() { ["_id"] = prj, ["total"] = new BsonDocument { ["$sum"] = "$detail.fund" } };

        var fluent = m_Vouchers.Aggregate().Match(preF).Project(pprj).Unwind("detail").Match(chk).Group(grp);
        if (query.Subtotal.ShouldAvoidZero())
            fluent = fluent.Match(FilterNonZero);
        if (limit > 0)
            fluent = fluent.Sort(new SortDefinitionBuilder<BsonDocument>().Descending("count")).Limit(limit);
        return fluent.ToAsyncEnumerable().Select(static b => BsonSerializer.Deserialize<Balance>(b));
    }

    public IAsyncEnumerable<(Voucher, string, string, double)> SelectUnbalancedVouchers(
        IQueryCompounded<IVoucherQueryAtom> query)
        => m_Vouchers.Aggregate().Match(query.Accept(new MongoDbNativeVoucher()))
            .Unwind("detail").Group(new BsonDocument
                {
                    ["_id"] = new BsonDocument
                        {
                            ["id"] = "$_id", ["user"] = "$detail.user", ["currency"] = "$detail.currency",
                        },
                    ["value"] = new BsonDocument { ["$sum"] = "$detail.fund" },
                })
            .Match(new BsonDocument
                {
                    ["$expr"] = new BsonDocument
                        {
                            ["$gt"] = new BsonArray
                                {
                                    new BsonDocument { ["$abs"] = "$value" }, VoucherDetail.Tolerance,
                                },
                        },
                })
            .Lookup("voucher", "_id.id", "_id", "voucher")
            .Sort(new SortDefinitionBuilder<BsonDocument>().Ascending("voucher.date"))
            .ToAsyncEnumerable().Select(static b =>
                {
                    var bsonReader = new BsonDocumentReader(b);
                    string read = null;
                    bsonReader.ReadStartDocument();
                    var (user, curr) = bsonReader.ReadDocument("_id", ref read, bR =>
                        {
                            // ReSharper disable AccessToModifiedClosure
                            bR.ReadStartDocument();
                            bR.ReadObjectId("id", ref read);
                            var u = bR.ReadString("user", ref read);
                            var c = bR.ReadString("currency", ref read);
                            bR.ReadEndDocument();
                            return (u, c);
                            // ReSharper restore AccessToModifiedClosure
                        });
                    var v = bsonReader.ReadDouble("value", ref read)!.Value;
                    var voucher = bsonReader.ReadArray("voucher", ref read, new VoucherSerializer().Deserialize)
                        .Single();
                    bsonReader.ReadEndDocument();
                    return (voucher, user, curr, v);
                });

    public IAsyncEnumerable<(Voucher, List<string>)> SelectDuplicatedVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => m_Vouchers.Aggregate().Match(query.Accept(new MongoDbNativeVoucher()))
            .Group(new BsonDocument
                {
                    ["_id"] = new BsonDocument { ["date"] = "$date", ["detail"] = "$detail" },
                    ["count"] = new BsonDocument { ["$sum"] = 1 },
                    ["ids"] = new BsonDocument { ["$push"] = "$_id" },
                })
            .Match(new FilterDefinitionBuilder<BsonDocument>().Gt("count", 1))
            .Sort(new SortDefinitionBuilder<BsonDocument>().Ascending("_id.date"))
            .ToAsyncEnumerable().Select(static b =>
                {
                    var bsonReader = new BsonDocumentReader(b);
                    string read = null;
                    bsonReader.ReadStartDocument();
                    var v = bsonReader.ReadDocument("_id", ref read, new VoucherSerializer().Deserialize);
                    bsonReader.ReadInt32("count", ref read);
                    var ids = bsonReader.ReadArray("ids", ref read, static bR => bR.ReadObjectId().ToString());
                    bsonReader.ReadEndDocument();
                    return (v, ids);
                });

    /// <inheritdoc />
    public async ValueTask<bool> DeleteVoucher(string id)
    {
        var res = await m_Vouchers.DeleteOneAsync(GetNQuery<Voucher>(id));
        return res.DeletedCount == 1;
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteVouchers(IEnumerable<string> ids)
        => (await m_Vouchers.DeleteManyAsync(GetNQuery<Voucher>(ids))).DeletedCount;

    /// <inheritdoc />
    public async ValueTask<long> DeleteVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => (await m_Vouchers.DeleteManyAsync(query.Accept(new MongoDbNativeVoucher()))).DeletedCount;

    /// <inheritdoc />
    public ValueTask<bool> Upsert(Voucher entity) => Upsert(m_Vouchers, entity, new VoucherSerializer());

    /// <inheritdoc />
    public ValueTask<long> Upsert(IEnumerable<Voucher> entities)
        => Upsert(m_Vouchers, entities, new VoucherSerializer());

    #endregion

    #region Asset

    /// <inheritdoc />
    public async ValueTask<Asset> SelectAsset(Guid id)
        => (await m_Assets.FindAsync(GetNQuery<Asset>(id))).FirstOrDefault();

    /// <inheritdoc />
    public IAsyncEnumerable<Asset> SelectAssets(IQueryCompounded<IDistributedQueryAtom> filter) =>
        m_Assets.Find(filter.Accept(new MongoDbNativeDistributed<Asset>()))
            .ToAsyncEnumerable();

    /// <inheritdoc />
    public async ValueTask<bool> DeleteAsset(Guid id)
    {
        var res = await m_Assets.DeleteOneAsync(GetNQuery<Asset>(id));
        return res.DeletedCount == 1;
    }

    /// <inheritdoc />
    public async ValueTask<bool> Upsert(Asset entity)
    {
        var res = await m_Assets.ReplaceOneAsync(
            Builders<Asset>.Filter.Eq("_id", entity.ID),
            entity,
            new ReplaceOptions { IsUpsert = true });
        return res.ModifiedCount <= 1;
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteAssets(IQueryCompounded<IDistributedQueryAtom> filter)
    {
        var res = await m_Assets.DeleteManyAsync(filter.Accept(new MongoDbNativeDistributed<Asset>()));
        return res.DeletedCount;
    }

    #endregion

    #region Amortization

    /// <inheritdoc />
    public async ValueTask<Amortization> SelectAmortization(Guid id) =>
        (await m_Amortizations.FindAsync(GetNQuery<Amortization>(id))).FirstOrDefault();

    /// <inheritdoc />
    public IAsyncEnumerable<Amortization> SelectAmortizations(IQueryCompounded<IDistributedQueryAtom> filter) =>
        m_Amortizations.Find(filter.Accept(new MongoDbNativeDistributed<Amortization>()))
            .ToAsyncEnumerable();

    /// <inheritdoc />
    public async ValueTask<bool> DeleteAmortization(Guid id)
    {
        var res = await m_Amortizations.DeleteOneAsync(GetNQuery<Amortization>(id));
        return res.DeletedCount == 1;
    }

    /// <inheritdoc />
    public async ValueTask<bool> Upsert(Amortization entity)
    {
        var res = await m_Amortizations.ReplaceOneAsync(
            Builders<Amortization>.Filter.Eq("_id", entity.ID),
            entity,
            new ReplaceOptions { IsUpsert = true });
        return res.ModifiedCount <= 1;
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
    {
        var res = await m_Amortizations.DeleteManyAsync(filter.Accept(new MongoDbNativeDistributed<Amortization>()));
        return res.DeletedCount;
    }

    #endregion

    #region ExchangeRecord

    /// <inheritdoc />
    public async ValueTask<ExchangeRecord> SelectExchangeRecord(ExchangeRecord record) =>
        (await m_Records.Find(
                Builders<ExchangeRecord>.Filter.Gte("_id.date", record.Time) &
                Builders<ExchangeRecord>.Filter.Eq("_id.from", record.From) &
                Builders<ExchangeRecord>.Filter.Eq("_id.to", record.To)
            ).Sort(Builders<ExchangeRecord>.Sort.Ascending("_id.date"))
            .Limit(1)
            .ToCursorAsync()
        ).SingleOrDefault();

    /// <inheritdoc />
    public async ValueTask<bool> Upsert(ExchangeRecord record)
    {
        try
        {
            await m_Records.InsertOneAsync(record);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    #endregion
}
