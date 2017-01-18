using AccountingServer.Entities;
using MongoDB.Bson.IO;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     Ï¸Ä¿ÐòÁÐ»¯Æ÷
    /// </summary>
    internal class VoucherDetailSerializer : BaseSerializer<VoucherDetail>
    {
        public override VoucherDetail Deserialize(IBsonReader bsonReader)
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

        public override void Serialize(IBsonWriter bsonWriter, VoucherDetail detail)
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
