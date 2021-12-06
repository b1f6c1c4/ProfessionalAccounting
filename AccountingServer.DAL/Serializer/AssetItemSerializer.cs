using AccountingServer.Entities;
using MongoDB.Bson.IO;

namespace AccountingServer.DAL.Serializer
{
    /// <summary>
    ///     折旧计算表条目序列化器
    /// </summary>
    internal class AssetItemSerializer : BaseSerializer<AssetItem>
    {
        public override AssetItem Deserialize(IBsonReader bsonReader)
        {
            string read = null;

            bsonReader.ReadStartDocument();
            var vid = bsonReader.ReadObjectId("voucher", ref read);
            var dt = bsonReader.ReadDateTime("date", ref read);
            var rmk = bsonReader.ReadString("remark", ref read);

            AssetItem item;
            double? val;
            if ((val = bsonReader.ReadDouble("acq", ref read)).HasValue)
                item = new AcquisitionItem { VoucherID = vid, Date = dt, Remark = rmk, OrigValue = val.Value };
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

        public override void Serialize(IBsonWriter bsonWriter, AssetItem item)
        {
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteObjectId("voucher", item.VoucherID);
            bsonWriter.Write("date", item.Date);
            bsonWriter.Write("remark", item.Remark);

            switch (item)
            {
                case AcquisitionItem acq:
                    bsonWriter.Write("acq", acq.OrigValue);
                    break;
                case DepreciateItem dep:
                    bsonWriter.Write("dep", dep.Amount);
                    break;
                case DevalueItem dev:
                    bsonWriter.Write("devto", dev.FairValue);
                    break;
                case DispositionItem _:
                    bsonWriter.WriteNull("dispo");
                    break;
            }

            bsonWriter.WriteEndDocument();
        }
    }
}
