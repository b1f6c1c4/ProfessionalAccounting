using System;
using AccountingServer.Entities;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     摊销计算表条目序列化器
    /// </summary>
    internal class AmortItemSerializer : IBsonSerializer<AmortItem>
    {
        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            Deserialize(context, args);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) =>
            Serialize(context, args, (AmortItem)value);

        public Type ValueType => typeof(AmortItem);

        public AmortItem Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            Deserialize(context.Reader);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, AmortItem value) =>
            Serialize(context.Writer, value);

        public static AmortItem Deserialize(IBsonReader bsonReader)
        {
            string read = null;

            bsonReader.ReadStartDocument();
            var item = new AmortItem
                           {
                               VoucherID = bsonReader.ReadObjectId("voucher", ref read),
                               Date = bsonReader.ReadDateTime("date", ref read),
                               Amount = bsonReader.ReadDouble("amount", ref read) ?? 0D,
                               Remark = bsonReader.ReadString("remark", ref read)
                           };
            bsonReader.ReadEndDocument();
            return item;
        }

        public static void Serialize(IBsonWriter bsonWriter, AmortItem item)
        {
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteObjectId("voucher", item.VoucherID);
            bsonWriter.Write("date", item.Date);
            bsonWriter.Write("amount", item.Amount);
            bsonWriter.Write("remark", item.Remark);
            bsonWriter.WriteEndDocument();
        }
    }
}
