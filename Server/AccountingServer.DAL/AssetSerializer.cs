using System;
using AccountingServer.Entities;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     资产序列化器
    /// </summary>
    internal class AssetSerializer : BsonBaseSerializer, IBsonIdProvider
    {
        public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType,
                                           IBsonSerializationOptions options) => Deserialize(bsonReader);

        // ReSharper disable once MemberCanBePrivate.Global
        public static object Deserialize(BsonReader bsonReader)
        {
            string read = null;

            bsonReader.ReadStartDocument();

            var asset = new Asset
                            {
                                ID = bsonReader.ReadGuid("_id", ref read),
                                Name = bsonReader.ReadString("name", ref read),
                                Date = bsonReader.ReadDateTime("date", ref read),
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
            asset.Schedule = bsonReader.ReadArray("schedule", ref read, AssetItemSerializer.Deserialize);
            asset.Remark = bsonReader.ReadString("remark", ref read);
            bsonReader.ReadEndDocument();
            return asset;
        }

        public override void Serialize(BsonWriter bsonWriter, Type nominalType, object value,
                                       IBsonSerializationOptions options) => Serialize(bsonWriter, (Asset)value);

        // ReSharper disable once MemberCanBePrivate.Global
        public static void Serialize(BsonWriter bsonWriter, Asset asset)
        {
            bsonWriter.WriteStartDocument();
            bsonWriter.Write("_id", asset.ID);
            bsonWriter.Write("name", asset.Name);
            bsonWriter.Write("date", asset.Date);
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
                    AssetItemSerializer.Serialize(bsonWriter, item);
                bsonWriter.WriteEndArray();
            }
            bsonWriter.Write("remark", asset.Remark);
            bsonWriter.WriteEndDocument();
        }

        public bool GetDocumentId(object document, out object id, out Type idNominalType, out IIdGenerator idGenerator)
        {
            id = null;
            idNominalType = typeof(Asset);
            idGenerator = new GuidGenerator();
            if (((Asset)document).ID == null)
                return false;

            id = ((Asset)document).ID;
            return true;
        }

        public void SetDocumentId(object document, object id) { ((Asset)document).ID = (Guid)id; }
    }
}
