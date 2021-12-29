/* Copyright (C) 2020-2021 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace AccountingServer.DAL.Serializer;

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
                Type = VoucherType.Ordinary,
            };
        voucher.Type = bsonReader.ReadString("special", ref read) switch
            {
                "amorz" => VoucherType.Amortization,
                "acarry" => VoucherType.AnnualCarry,
                "carry" => VoucherType.Carry,
                "dep" => VoucherType.Depreciation,
                "dev" => VoucherType.Devalue,
                "unc" => VoucherType.Uncertain,
                _ => VoucherType.Ordinary,
            };

        voucher.Details = bsonReader.ReadArray("detail", ref read, new VoucherDetailSerializer().Deserialize);
        voucher.Remark = bsonReader.ReadString("remark", ref read);
        bsonReader.ReadEndDocument();

        return voucher;
    }

    public override void Serialize(IBsonWriter bsonWriter, Voucher voucher)
    {
        bsonWriter.WriteStartDocument();
        bsonWriter.WriteObjectId("_id", voucher.ID);
        bsonWriter.Write("date", voucher.Date);
        if (voucher.Type != null && voucher.Type != VoucherType.Ordinary)
            bsonWriter.Write("special", voucher.Type switch
                {
                    VoucherType.Amortization => "amorz",
                    VoucherType.AnnualCarry => "acarry",
                    VoucherType.Carry => "carry",
                    VoucherType.Depreciation => "dep",
                    VoucherType.Devalue => "dev",
                    VoucherType.Uncertain => "unc",
                    _ => throw new InvalidOperationException(),
                });

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
        bsonWriter.WriteEndDocument();
    }

    public override ObjectId GetId(Voucher entity) => entity.ID != null ? new(entity.ID) : ObjectId.Empty;
    protected override void SetId(Voucher entity, ObjectId id) => entity.ID = id.ToString();
    protected override bool IsNull(ObjectId id) => id == ObjectId.Empty;

    protected override ObjectId MakeId(IMongoCollection<Voucher> container, Voucher entity) =>
        (ObjectId)new ObjectIdGenerator().GenerateId(container, entity);
}
