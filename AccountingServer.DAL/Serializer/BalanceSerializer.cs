/* Copyright (C) 2020-2022 b1f6c1c4
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
using MongoDB.Bson.IO;

namespace AccountingServer.DAL.Serializer;

/// <summary>
///     余额表条目反序列化器（从MapReduce的结果中反序列化）
/// </summary>
internal class BalanceSerializer : BaseSerializer<Balance>
{
    public override void Serialize(IBsonWriter bsonWriter, Balance voucher)
        => throw new InvalidOperationException();

    public override Balance Deserialize(IBsonReader bsonReader)
    {
        string read = null;

        bsonReader.ReadStartDocument();
        var balance =
            bsonReader
                .ReadDocument(
                    "_id",
                    ref read,
                    bR =>
                        {
                            // ReSharper disable AccessToModifiedClosure
                            bR.ReadStartDocument();
                            var bal =
                                new Balance
                                    {
                                        Date = bR.ReadDateTime("date", ref read),
                                        User = bR.ReadString("user", ref read),
                                        Currency = bR.ReadString("currency", ref read),
                                        Title = bR.ReadInt32("title", ref read),
                                        SubTitle = bR.ReadInt32("subtitle", ref read),
                                        Content = bR.ReadString("content", ref read),
                                        Remark = bR.ReadString("remark", ref read),
                                    };
                            bR.ReadEndDocument();
                            return bal;
                            // ReSharper restore AccessToModifiedClosure
                        });
        balance.Fund = bsonReader.ReadDouble("total", ref read) ?? bsonReader.ReadInt32("count", ref read)!.Value;
        bsonReader.ReadEndDocument();

        return balance;
    }
}
