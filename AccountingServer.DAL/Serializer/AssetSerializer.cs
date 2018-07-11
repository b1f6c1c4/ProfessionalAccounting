using System;
using AccountingServer.Entities;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace AccountingServer.DAL.Serializer
{
    /// <summary>
    ///     资产序列化器
    /// </summary>
    internal class AssetSerializer : BaseSerializer<Asset, Guid?>
    {
        private static readonly AssetItemSerializer ItemSerializer = new AssetItemSerializer();

        public override Asset Deserialize(IBsonReader bsonReader)
        {
            string read = null;

            bsonReader.ReadStartDocument();

            var asset = new Asset
                {
                    ID = bsonReader.ReadGuid("_id", ref read),
                    Name = bsonReader.ReadString("name", ref read),
                    Date = bsonReader.ReadDateTime("date", ref read),
                    Currency = bsonReader.ReadString("currency", ref read),
                    Value = bsonReader.ReadDouble("value", ref read),
                    Salvge = bsonReader.ReadDouble("salvge", ref read),
                    Life = bsonReader.ReadInt32("life", ref read),
                    Title = bsonReader.ReadInt32("title", ref read),
                    DepreciationTitle = bsonReader.ReadInt32("deptitle", ref read),
                    DevaluationTitle = bsonReader.ReadInt32("devtitle", ref read),
                    DepreciationExpenseTitle = bsonReader.ReadInt32("exptitle", ref read),
                    DevaluationExpenseTitle = bsonReader.ReadInt32("exvtitle", ref read)
                };
            switch (bsonReader.ReadString("method", ref read))
            {
                case "sl":
                    asset.Method = DepreciationMethod.StraightLine;
                    break;
                case "sy":
                    asset.Method = DepreciationMethod.SumOfTheYear;
                    break;
                case "dd":
                    asset.Method = DepreciationMethod.DoubleDeclineMethod;
                    break;
                default:
                    asset.Method = DepreciationMethod.None;
                    break;
            }

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
            bsonWriter.Write("name", asset.Name);
            bsonWriter.Write("date", asset.Date);
            bsonWriter.Write("currency", asset.Currency);
            bsonWriter.Write("value", asset.Value);
            bsonWriter.Write("salvge", asset.Salvge);
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
            switch (asset.Method)
            {
                case DepreciationMethod.StraightLine:
                    bsonWriter.Write("method", "sl");
                    break;
                case DepreciationMethod.SumOfTheYear:
                    bsonWriter.Write("method", "sy");
                    break;
                case DepreciationMethod.DoubleDeclineMethod:
                    bsonWriter.Write("method", "dd");
                    break;
            }

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
}
