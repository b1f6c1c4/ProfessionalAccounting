using System;
using AccountingServer.Entities;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace AccountingServer.DAL
{
    internal class BalanceSerializer : BsonBaseSerializer
    {
        public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType,
                                           IBsonSerializationOptions options) { return Deserialize(bsonReader); }

        public static Balance Deserialize(BsonReader bsonReader)
        {
            string read = null;

            bsonReader.ReadStartDocument();
            var detail = new Balance
                             {
                                 Date = bsonReader.ReadDateTime("date", ref read),
                                 Title = bsonReader.ReadInt32("title", ref read),
                                 SubTitle = bsonReader.ReadInt32("subtitle", ref read),
                                 Content = bsonReader.ReadString("content", ref read),
                                 Fund = bsonReader.ReadDouble("fund", ref read).Value,
                                 Remark = bsonReader.ReadString("remark", ref read)
                             };
            bsonReader.ReadEndDocument();

            return detail;
        }

        public override void Serialize(BsonWriter bsonWriter, Type nominalType, object value,
                                       IBsonSerializationOptions options) { Serialize(bsonWriter, (Balance)value); }

        internal static void Serialize(BsonWriter bsonWriter, Balance detail)
        {
            bsonWriter.WriteStartDocument();
            bsonWriter.Write("date", detail.Date);
            bsonWriter.Write("title", detail.Title);
            bsonWriter.Write("subtitle", detail.SubTitle);
            bsonWriter.Write("content", detail.Content);
            bsonWriter.Write("fund", detail.Fund);
            bsonWriter.Write("remark", detail.Remark);
            bsonWriter.WriteEndDocument();
        }
    }
}
