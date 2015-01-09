using System;
using AccountingServer.Entities;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace AccountingServer.DAL
{
    internal class AssetItemSerializer : BsonBaseSerializer
    {
        public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType,
                                           IBsonSerializationOptions options) { return Deserialize(bsonReader); }

        public static AssetItem Deserialize(BsonReader bsonReader)
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

        public override void Serialize(BsonWriter bsonWriter, Type nominalType, object value,
                                       IBsonSerializationOptions options) { Serialize(bsonWriter, (AssetItem)value); }

        public static void Serialize(BsonWriter bsonWriter, AssetItem item)
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
