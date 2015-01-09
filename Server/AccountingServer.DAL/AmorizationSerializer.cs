using System;
using AccountingServer.Entities;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace AccountingServer.DAL
{
    internal class AmortizationSerializer : BsonBaseSerializer, IBsonIdProvider
    {
        public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType,
                                           IBsonSerializationOptions options)
        {
            string read = null;

            bsonReader.ReadStartDocument();

            var amort = new Amortization
                            {
                                ID = bsonReader.ReadGuid("_id", ref read),
                                Name = bsonReader.ReadString("name", ref read),
                                Value = bsonReader.ReadDouble("value", ref read),
                                Date = bsonReader.ReadDateTime("date", ref read),
                                TotalDays = bsonReader.ReadInt32("tday", ref read),
                            };
            switch (bsonReader.ReadString("interval", ref read))
            {
                case "d":
                    amort.Interval = AmortizeInterval.EveryDay;
                    break;
                case "w":
                    amort.Interval = AmortizeInterval.SameDayOfWeek;
                    break;
                case "W":
                    amort.Interval = AmortizeInterval.LastDayOfWeek;
                    break;
                case "m":
                    amort.Interval = AmortizeInterval.SameDayOfMonth;
                    break;
                case "M":
                    amort.Interval = AmortizeInterval.LastDayOfMonth;
                    break;
                case "y":
                    amort.Interval = AmortizeInterval.SameDayOfYear;
                    break;
                case "Y":
                    amort.Interval = AmortizeInterval.LastDayOfYear;
                    break;
            }
            amort.Template = bsonReader.ReadDocument("template", ref read, VoucherSerializer.Deserialize);
            amort.Schedule = bsonReader.ReadArray("schedule", ref read, AmortItemSerializer.Deserialize).ToArray();
            amort.Remark = bsonReader.ReadString("remark", ref read);
            bsonReader.ReadEndDocument();
            return amort;
        }

        public override void Serialize(BsonWriter bsonWriter, Type nominalType, object value,
                                       IBsonSerializationOptions options)
        {
            var asset = (Amortization)value;

            bsonWriter.WriteStartDocument();
            bsonWriter.Write("_id", asset.ID);
            bsonWriter.Write("name", asset.Name);
            bsonWriter.Write("value", asset.Value);
            bsonWriter.Write("date", asset.Date);
            bsonWriter.Write("tday", asset.TotalDays);
            switch (asset.Interval)
            {
                case AmortizeInterval.EveryDay:
                    bsonWriter.Write("interval", "d");
                    break;
                case AmortizeInterval.SameDayOfWeek:
                    bsonWriter.Write("interval", "w");
                    break;
                case AmortizeInterval.LastDayOfWeek:
                    bsonWriter.Write("interval", "W");
                    break;
                case AmortizeInterval.SameDayOfMonth:
                    bsonWriter.Write("interval", "m");
                    break;
                case AmortizeInterval.LastDayOfMonth:
                    bsonWriter.Write("interval", "M");
                    break;
                case AmortizeInterval.SameDayOfYear:
                    bsonWriter.Write("interval", "y");
                    break;
                case AmortizeInterval.LastDayOfYear:
                    bsonWriter.Write("interval", "Y");
                    break;
            }
            if (asset.Template != null)
                VoucherSerializer.Serialize(bsonWriter, asset.Template);
            if (asset.Schedule != null)
            {
                bsonWriter.WriteStartArray("schedule");
                foreach (var item in asset.Schedule)
                    AmortItemSerializer.Serialize(bsonWriter, item);
                bsonWriter.WriteEndArray();
            }
            bsonWriter.Write("remark", asset.Remark);
            bsonWriter.WriteEndDocument();
        }

        public bool GetDocumentId(object document, out object id, out Type idNominalType, out IIdGenerator idGenerator)
        {
            id = null;
            idNominalType = typeof(Amortization);
            idGenerator = new GuidGenerator();
            if (((Amortization)document).ID == null)
                return false;

            id = ((Amortization)document).ID;
            return true;
        }

        public void SetDocumentId(object document, object id) { ((Amortization)document).ID = (Guid)id; }
    }
}
