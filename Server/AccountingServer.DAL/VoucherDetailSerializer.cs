using System;
using AccountingServer.Entities;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace AccountingServer.DAL
{
    internal class VoucherDetailSerializer : BsonBaseSerializer
    {
        public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType,
                                           IBsonSerializationOptions options)
        {
            return Deserialize(bsonReader);
        }

        public static VoucherDetail Deserialize(BsonReader bsonReader)
        {
            string read = null;

            bsonReader.ReadStartDocument();
            var detail = new VoucherDetail
                             {
                                 Title = bsonReader.ReadInt32("title", ref read),
                                 SubTitle = bsonReader.ReadInt32("subtitle", ref read),
                                 Content = bsonReader.ReadString("content", ref read),
                                 Fund = bsonReader.ReadDouble("fund", ref read),
                                 Remark = bsonReader.ReadString("remark", ref read)
                             };
            bsonReader.ReadEndDocument();

            return detail;
        }

        public override void Serialize(BsonWriter bsonWriter, Type nominalType, object value,
                                       IBsonSerializationOptions options)
        {
            Serialize(bsonWriter, (VoucherDetail)value);
        }

        internal static void Serialize(BsonWriter bsonWriter, VoucherDetail detail)
        {
            bsonWriter.WriteStartDocument();
            bsonWriter.Write("title", detail.Title);
            bsonWriter.Write("subtitle", detail.SubTitle);
            bsonWriter.Write("content", detail.Content);
            bsonWriter.Write("fund", detail.Fund);
            bsonWriter.Write("remark", detail.Remark);
            bsonWriter.WriteEndDocument();
        }
    }
}
