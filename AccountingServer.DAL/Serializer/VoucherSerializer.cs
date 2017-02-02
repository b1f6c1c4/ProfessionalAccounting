using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace AccountingServer.DAL.Serializer
{
    /// <summary>
    ///     记账凭证序列化器
    /// </summary>
    internal class VoucherSerializer : BaseSerializer<Voucher, ObjectId>
    {
        public override Voucher Deserialize(IBsonReader bsonReader)
        {
            string read = null;
            bsonReader.ReadStartDocument();

            var voucher = new Voucher
                {
                    ID = bsonReader.ReadObjectId("_id", ref read),
                    Date = bsonReader.ReadDateTime("date", ref read),
                    Type = VoucherType.Ordinary
                };
            switch (bsonReader.ReadString("special", ref read))
            {
                case "amorz":
                    voucher.Type = VoucherType.Amortization;
                    break;
                case "acarry":
                    voucher.Type = VoucherType.AnnualCarry;
                    break;
                case "carry":
                    voucher.Type = VoucherType.Carry;
                    break;
                case "dep":
                    voucher.Type = VoucherType.Depreciation;
                    break;
                case "dev":
                    voucher.Type = VoucherType.Devalue;
                    break;
                case "unc":
                    voucher.Type = VoucherType.Uncertain;
                    break;
                default:
                    voucher.Type = VoucherType.Ordinary;
                    break;
            }

            voucher.Details = bsonReader.ReadArray("detail", ref read, new VoucherDetailSerializer().Deserialize);
            voucher.Remark = bsonReader.ReadString("remark", ref read);
            voucher.Currency = bsonReader.ReadString("currency", ref read) ?? Voucher.BaseCurrency;
            bsonReader.ReadEndDocument();

            return voucher;
        }

        public override void Serialize(IBsonWriter bsonWriter, Voucher voucher)
        {
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteObjectId("_id", voucher.ID);
            bsonWriter.Write("date", voucher.Date);
            if (voucher.Type != VoucherType.Ordinary)
                switch (voucher.Type)
                {
                    case VoucherType.Amortization:
                        bsonWriter.Write("special", "amorz");
                        break;
                    case VoucherType.AnnualCarry:
                        bsonWriter.Write("special", "acarry");
                        break;
                    case VoucherType.Carry:
                        bsonWriter.Write("special", "carry");
                        break;
                    case VoucherType.Depreciation:
                        bsonWriter.Write("special", "dep");
                        break;
                    case VoucherType.Devalue:
                        bsonWriter.Write("special", "dev");
                        break;
                    case VoucherType.Uncertain:
                        bsonWriter.Write("special", "unc");
                        break;
                }

            if (voucher.Details != null)
            {
                bsonWriter.WriteStartArray("detail");
                var serializer = new VoucherDetailSerializer();
                foreach (var detail in voucher.Details)
                    serializer.Serialize(bsonWriter, detail);

                bsonWriter.WriteEndArray();
            }

            if (voucher.Remark != null)
                bsonWriter.WriteString("remark", voucher.Remark);
            if (voucher.Currency != Voucher.BaseCurrency)
                bsonWriter.WriteString("currency", voucher.Currency);
            bsonWriter.WriteEndDocument();
        }

        public override ObjectId GetId(Voucher entity) => entity.ID != null ? new ObjectId(entity.ID) : ObjectId.Empty;
        protected override void SetId(Voucher entity, ObjectId id) => entity.ID = id.ToString();
        protected override bool IsNull(ObjectId id) => id == ObjectId.Empty;

        protected override ObjectId MakeId(IMongoCollection<Voucher> container, Voucher entity) =>
            (ObjectId)new ObjectIdGenerator().GenerateId(container, entity);
    }
}
