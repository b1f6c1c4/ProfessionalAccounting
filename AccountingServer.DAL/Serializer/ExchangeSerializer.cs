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
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace AccountingServer.DAL.Serializer;

/// <summary>
///     汇率序列化器
/// </summary>
internal class ExchangeSerializer : BaseSerializer<ExchangeRecord, ExchangeRecord>
{
    public override ExchangeRecord Deserialize(IBsonReader bsonReader)
    {
        string read = null;
        bsonReader.ReadStartDocument();
        var record = bsonReader.ReadDocument("_id", ref read, br =>
            {
                // ReSharper disable AccessToModifiedClosure
                br.ReadStartDocument();
                var rec = new ExchangeRecord
                    {
                        Time = br.ReadDateTime("date", ref read)!.Value,
                        From = br.ReadString("from", ref read),
                        To = br.ReadString("to", ref read),
                    };
                br.ReadEndDocument();
                return rec;
                // ReSharper restore AccessToModifiedClosure
            });
        record.Value = bsonReader.ReadDouble("value", ref read)!.Value;
        bsonReader.ReadEndDocument();
        return record;
    }

    public override void Serialize(IBsonWriter bsonWriter, ExchangeRecord record)
    {
        bsonWriter.WriteStartDocument();
        bsonWriter.WriteName("_id");
        bsonWriter.WriteStartDocument();
        bsonWriter.Write("date", record.Time);
        bsonWriter.Write("from", record.From);
        bsonWriter.Write("to", record.To);
        bsonWriter.WriteEndDocument();
        bsonWriter.Write("value", record.Value);
        bsonWriter.WriteEndDocument();
    }

    public override ExchangeRecord GetId(ExchangeRecord entity) => entity;
    protected override void SetId(ExchangeRecord entity, ExchangeRecord id) => throw new NotImplementedException();
    protected override bool IsNull(ExchangeRecord id) => id == null;

    protected override ExchangeRecord MakeId(IMongoCollection<ExchangeRecord> container, ExchangeRecord entity)
        => throw new NotImplementedException();
}
