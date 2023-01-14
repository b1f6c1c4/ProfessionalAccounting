/* Copyright (C) 2020-2023 b1f6c1c4
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

using AccountingServer.Entities;
using MongoDB.Bson.IO;

namespace AccountingServer.DAL.Serializer;

/// <summary>
///     细目序列化器
/// </summary>
internal class VoucherDetailSerializer : BaseSerializer<VoucherDetail>
{
    public override VoucherDetail Deserialize(IBsonReader bsonReader)
    {
        string read = null;

        bsonReader.ReadStartDocument();
        var detail = new VoucherDetail
            {
                User = bsonReader.ReadString("user", ref read),
                Currency = bsonReader.ReadString("currency", ref read),
                Title = bsonReader.ReadInt32("title", ref read),
                SubTitle = bsonReader.ReadInt32("subtitle", ref read),
                Content = bsonReader.ReadString("content", ref read),
                Fund = bsonReader.ReadDouble("fund", ref read),
                Remark = bsonReader.ReadString("remark", ref read),
            };
        bsonReader.ReadEndDocument();

        return detail;
    }

    public override void Serialize(IBsonWriter bsonWriter, VoucherDetail detail)
    {
        bsonWriter.WriteStartDocument();
        bsonWriter.WriteString("user", detail.User);
        bsonWriter.WriteString("currency", detail.Currency);
        bsonWriter.Write("title", detail.Title);
        bsonWriter.Write("subtitle", detail.SubTitle);
        bsonWriter.Write("content", detail.Content);
        bsonWriter.Write("fund", detail.Fund);
        bsonWriter.Write("remark", detail.Remark);
        bsonWriter.WriteEndDocument();
    }
}
