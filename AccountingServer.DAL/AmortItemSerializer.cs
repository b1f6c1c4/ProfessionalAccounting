using AccountingServer.Entities;
using MongoDB.Bson.IO;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     摊销计算表条目序列化器
    /// </summary>
    internal class AmortItemSerializer : BaseSerializer<AmortItem>
    {
        public override AmortItem Deserialize(IBsonReader bsonReader)
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

        public override void Serialize(IBsonWriter bsonWriter, AmortItem item)
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
