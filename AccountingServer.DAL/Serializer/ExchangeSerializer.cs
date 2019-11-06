using System;
using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace AccountingServer.DAL.Serializer
{
    /// <summary>
    ///     汇率序列化器
    /// </summary>
    internal class ExchangeSerializer : BaseSerializer<ExchangeRecord, ExchangeRecord>
    {
        public override ExchangeRecord Deserialize(IBsonReader bsonReader)
        {
            string read = null;
            bsonReader.ReadStartDocument();
            var record = bsonReader.ReadDocument("_id", ref read, (IBsonReader br) =>
                {
                    br.ReadStartDocument();
                    var rec = new ExchangeRecord
                        {
                            Date = br.ReadDateTime("date", ref read).Value,
                            From = br.ReadString("from", ref read),
                            To = br.ReadString("to", ref read)
                        };
                    br.ReadEndDocument();
                    return rec;
                });
            record.Value = bsonReader.ReadDouble("value", ref read).Value;
            bsonReader.ReadEndDocument();
            return record;
        }

        public override void Serialize(IBsonWriter bsonWriter, ExchangeRecord record)
        {
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteName("_id");
            bsonWriter.WriteStartDocument();
            bsonWriter.Write("date", record.Date);
            bsonWriter.Write("from", record.From);
            bsonWriter.Write("to", record.To);
            bsonWriter.WriteEndDocument();
            bsonWriter.Write("value", record.Value);
            bsonWriter.WriteEndDocument();
        }

        public override ExchangeRecord GetId(ExchangeRecord entity) => entity;
        protected override void SetId(ExchangeRecord entity, ExchangeRecord id) => throw new NotImplementedException();
        protected override bool IsNull(ExchangeRecord id) => id == null;
        protected override ExchangeRecord MakeId(IMongoCollection<ExchangeRecord> container, ExchangeRecord entity) => throw new NotImplementedException();
    }
}
