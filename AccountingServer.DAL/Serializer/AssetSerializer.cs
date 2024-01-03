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
using AccountingServer.Entities;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace AccountingServer.DAL.Serializer;

/// <summary>
///     资产序列化器
/// </summary>
internal class AssetSerializer : BaseSerializer<Asset, Guid?>
{
    private static readonly AssetItemSerializer ItemSerializer = new();

    public override Asset Deserialize(IBsonReader bsonReader)
    {
        string read = null;

        bsonReader.ReadStartDocument();

        var asset = new Asset
            {
                ID = bsonReader.ReadGuid("_id", ref read),
                User = bsonReader.ReadString("user", ref read),
                Name = bsonReader.ReadString("name", ref read),
                Date = bsonReader.ReadDateTime("date", ref read),
                Currency = bsonReader.ReadString("currency", ref read),
                Value = bsonReader.ReadDouble("value", ref read),
                Salvage = bsonReader.ReadDouble("salvage", ref read),
                Life = bsonReader.ReadInt32("life", ref read),
                Title = bsonReader.ReadInt32("title", ref read),
                DepreciationTitle = bsonReader.ReadInt32("deptitle", ref read),
                DevaluationTitle = bsonReader.ReadInt32("devtitle", ref read),
                DepreciationExpenseTitle = bsonReader.ReadInt32("exptitle", ref read),
                DevaluationExpenseTitle = bsonReader.ReadInt32("exvtitle", ref read),
                Method = bsonReader.ReadString("method", ref read) switch
                    {
                        "sl" => DepreciationMethod.StraightLine,
                        "sy" => DepreciationMethod.SumOfTheYear,
                        "dd" => DepreciationMethod.DoubleDeclineMethod,
                        _ => DepreciationMethod.None,
                    },
            };

        if (asset.DepreciationExpenseTitle > 100)
        {
            asset.DepreciationExpenseSubTitle = asset.DepreciationExpenseTitle % 100;
            asset.DepreciationExpenseTitle /= 100;
        }

        if (asset.DevaluationExpenseTitle > 100)
        {
            asset.DevaluationExpenseSubTitle = asset.DevaluationExpenseTitle % 100;
            asset.DevaluationExpenseTitle /= 100;
        }

        asset.Schedule = bsonReader.ReadArray("schedule", ref read, ItemSerializer.Deserialize);
        asset.Remark = bsonReader.ReadString("remark", ref read);
        bsonReader.ReadEndDocument();
        return asset;
    }

    public override void Serialize(IBsonWriter bsonWriter, Asset asset)
    {
        bsonWriter.WriteStartDocument();
        bsonWriter.Write("_id", asset.ID);
        bsonWriter.Write("user", asset.User);
        bsonWriter.Write("name", asset.Name);
        bsonWriter.Write("date", asset.Date);
        bsonWriter.Write("currency", asset.Currency);
        bsonWriter.Write("value", asset.Value);
        bsonWriter.Write("salvage", asset.Salvage);
        bsonWriter.Write("life", asset.Life);
        bsonWriter.Write("title", asset.Title);
        bsonWriter.Write("deptitle", asset.DepreciationTitle);
        bsonWriter.Write("devtitle", asset.DevaluationTitle);
        bsonWriter.Write(
            "exptitle",
            asset.DepreciationExpenseSubTitle.HasValue
                ? asset.DepreciationExpenseTitle * 100 + asset.DepreciationExpenseSubTitle
                : asset.DepreciationExpenseTitle);
        bsonWriter.Write(
            "exvtitle",
            asset.DevaluationExpenseSubTitle.HasValue
                ? asset.DevaluationExpenseTitle * 100 + asset.DevaluationExpenseSubTitle
                : asset.DevaluationExpenseTitle);
        if (asset.Method != DepreciationMethod.None)
            bsonWriter.Write("method", asset.Method switch
                {
                    DepreciationMethod.StraightLine => "sl",
                    DepreciationMethod.SumOfTheYear => "sy",
                    DepreciationMethod.DoubleDeclineMethod => "dd",
                    _ => throw new InvalidOperationException(),
                });

        if (asset.Schedule != null)
        {
            bsonWriter.WriteStartArray("schedule");
            foreach (var item in asset.Schedule)
                ItemSerializer.Serialize(bsonWriter, item);

            bsonWriter.WriteEndArray();
        }

        bsonWriter.Write("remark", asset.Remark);
        bsonWriter.WriteEndDocument();
    }

    public override Guid? GetId(Asset entity) => entity.ID;
    protected override void SetId(Asset entity, Guid? id) => entity.ID = id;
    protected override bool IsNull(Guid? id) => !id.HasValue;

    protected override Guid? MakeId(IMongoCollection<Asset> container, Asset entity) =>
        Guid.NewGuid();
}
