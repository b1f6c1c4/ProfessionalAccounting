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
                                           IBsonSerializationOptions options) { return Deserialize(bsonReader); }

        // ReSharper disable once MemberCanBePrivate.Global
        public static object Deserialize(BsonReader bsonReader)
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
            amort.Schedule = bsonReader.ReadArray("schedule", ref read, AmortItemSerializer.Deserialize);
            amort.Remark = bsonReader.ReadString("remark", ref read);
            bsonReader.ReadEndDocument();
            return amort;
        }

        public override void Serialize(BsonWriter bsonWriter, Type nominalType, object value,
                                       IBsonSerializationOptions options) { Serialize(bsonWriter, (Amortization)value); }

        // ReSharper disable once MemberCanBePrivate.Global
        public static void Serialize(BsonWriter bsonWriter, Amortization amort)
        {
            bsonWriter.WriteStartDocument();
            bsonWriter.Write("_id", amort.ID);
            bsonWriter.Write("name", amort.Name);
            bsonWriter.Write("value", amort.Value);
            bsonWriter.Write("date", amort.Date);
            bsonWriter.Write("tday", amort.TotalDays);
            switch (amort.Interval)
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
            if (amort.Template != null)
            {
                bsonWriter.WriteName("template");
                VoucherSerializer.Serialize(bsonWriter, amort.Template);
            }
            if (amort.Schedule != null)
            {
                bsonWriter.WriteStartArray("schedule");
                foreach (var item in amort.Schedule)
                    AmortItemSerializer.Serialize(bsonWriter, item);
                bsonWriter.WriteEndArray();
            }
            bsonWriter.Write("remark", amort.Remark);
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
