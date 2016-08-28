using System;
using AccountingServer.Entities;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     折旧计算表条目序列化器
    /// </summary>
    internal class AssetItemSerializer : IBsonSerializer<AssetItem>
    {
        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            Deserialize(context, args);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) =>
            Serialize(context, args, (AssetItem)value);

        public Type ValueType => typeof(AssetItem);

        public AssetItem Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            Deserialize(context.Reader);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, AssetItem value) =>
            Serialize(context.Writer, value);

        public static AssetItem Deserialize(IBsonReader bsonReader)
        {
            string read = null;

            bsonReader.ReadStartDocument();
            var vid = bsonReader.ReadObjectId("voucher", ref read);
            var dt = bsonReader.ReadDateTime("date", ref read);
            var rmk = bsonReader.ReadString("remark", ref read);

            AssetItem item;
            double? val;
            if ((val = bsonReader.ReadDouble("acq", ref read)).HasValue)
                item = new AcquisationItem { VoucherID = vid, Date = dt, Remark = rmk, OrigValue = val.Value };
            else if ((val = bsonReader.ReadDouble("dep", ref read)).HasValue)
                item = new DepreciateItem { VoucherID = vid, Date = dt, Remark = rmk, Amount = val.Value };
            else if ((val = bsonReader.ReadDouble("devto", ref read)).HasValue)
                item = new DevalueItem { VoucherID = vid, Date = dt, Remark = rmk, FairValue = val.Value };
            else if (bsonReader.ReadNull("dispo", ref read))
                item = new DispositionItem { VoucherID = vid, Date = dt, Remark = rmk };
            else
                item = null;
            bsonReader.ReadEndDocument();
            return item;
        }

        public static void Serialize(IBsonWriter bsonWriter, AssetItem item)
        {
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteObjectId("voucher", item.VoucherID);
            bsonWriter.Write("date", item.Date);
            bsonWriter.Write("remark", item.Remark);

            if (item is AcquisationItem)
                bsonWriter.Write("acq", (item as AcquisationItem).OrigValue);
            else if (item is DepreciateItem)
                bsonWriter.Write("dep", (item as DepreciateItem).Amount);
            else if (item is DevalueItem)
                bsonWriter.Write("devto", (item as DevalueItem).FairValue);
            else if (item is DispositionItem)
                bsonWriter.WriteNull("dispo");
            bsonWriter.WriteEndDocument();
        }
    }
}
