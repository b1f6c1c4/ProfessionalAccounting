using System;
using AccountingServer.Entities;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace AccountingServer.DAL.Serializer
{
    /// <summary>
    ///     Ì¯ÏúÐòÁÐ»¯Æ÷
    /// </summary>
    internal class AmortizationSerializer : BaseSerializer<Amortization, Guid?>
    {
        public override Amortization Deserialize(IBsonReader bsonReader)
        {
            string read = null;

            bsonReader.ReadStartDocument();

            var amort = new Amortization
                {
                    ID = bsonReader.ReadGuid("_id", ref read),
                    Name = bsonReader.ReadString("name", ref read),
                    Value = bsonReader.ReadDouble("value", ref read),
                    Date = bsonReader.ReadDateTime("date", ref read),
                    TotalDays = bsonReader.ReadInt32("tday", ref read)
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

            amort.Template = bsonReader.ReadDocument("template", ref read, new VoucherSerializer().Deserialize);
            amort.Schedule = bsonReader.ReadArray("schedule", ref read, new AmortItemSerializer().Deserialize);
            amort.Remark = bsonReader.ReadString("remark", ref read);
            bsonReader.ReadEndDocument();
            return amort;
        }

        public override void Serialize(IBsonWriter bsonWriter, Amortization amort)
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
                new VoucherSerializer().Serialize(bsonWriter, amort.Template);
            }
            if (amort.Schedule != null)
            {
                bsonWriter.WriteStartArray("schedule");
                var serializer = new AmortItemSerializer();
                foreach (var item in amort.Schedule)
                    serializer.Serialize(bsonWriter, item);

                bsonWriter.WriteEndArray();
            }

            bsonWriter.Write("remark", amort.Remark);
            bsonWriter.WriteEndDocument();
        }

        public override Guid? GetId(Amortization entity) => entity.ID;
        public override void SetId(Amortization entity, Guid? id) => entity.ID = id;
        public override bool IsNull(Guid? id) => !id.HasValue;

        public override Guid? MakeId(IMongoCollection<Amortization> container, Amortization entity) =>
            Guid.NewGuid();
    }
}
