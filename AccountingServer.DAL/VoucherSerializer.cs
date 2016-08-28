using System;
using AccountingServer.Entities;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     记账凭证序列化器
    /// </summary>
    internal class VoucherSerializer : IBsonSerializer<Voucher>, IBsonIdProvider
    {
        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)=>
            Deserialize(context, args);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) =>
            Serialize(context, args, (Voucher)value);

        public Type ValueType => typeof(Voucher);

        public Voucher Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            Deserialize(context.Reader);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Voucher value) =>
            Serialize(context.Writer, value);

        public static Voucher Deserialize(IBsonReader bsonReader)
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
            voucher.Details = bsonReader.ReadArray("detail", ref read, VoucherDetailSerializer.Deserialize);
            voucher.Remark = bsonReader.ReadString("remark", ref read);
            bsonReader.ReadEndDocument();

            return voucher;
        }

        public static void Serialize(IBsonWriter bsonWriter, Voucher voucher)
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
                foreach (var detail in voucher.Details)
                    VoucherDetailSerializer.Serialize(bsonWriter, detail);
                bsonWriter.WriteEndArray();
            }
            if (voucher.Remark != null)
                bsonWriter.WriteString("remark", voucher.Remark);
            bsonWriter.WriteEndDocument();
        }

        public bool GetDocumentId(object document, out object id, out Type idNominalType, out IIdGenerator idGenerator)
        {
            id = null;
            idNominalType = typeof(Voucher);
            idGenerator = new StringObjectIdGenerator();
            if (((Voucher)document).ID == null)
                return false;

            id = ((Voucher)document).ID;
            return true;
        }

        public void SetDocumentId(object document, object id) => ((Voucher)document).ID = (string)id;
    }
}
