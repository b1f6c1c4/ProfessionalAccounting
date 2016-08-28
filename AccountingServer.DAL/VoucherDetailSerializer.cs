using System;
using AccountingServer.Entities;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     Ï¸Ä¿ÐòÁÐ»¯Æ÷
    /// </summary>
    internal class VoucherDetailSerializer : IBsonSerializer<VoucherDetail>
    {
        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            Deserialize(context, args);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) =>
            Serialize(context, args, (VoucherDetail)value);

        public Type ValueType => typeof(VoucherDetail);

        public VoucherDetail Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            Deserialize(context.Reader);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, VoucherDetail value) =>
            Serialize(context.Writer, value);

        public static VoucherDetail Deserialize(IBsonReader bsonReader)
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

        internal static void Serialize(IBsonWriter bsonWriter, VoucherDetail detail)
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
