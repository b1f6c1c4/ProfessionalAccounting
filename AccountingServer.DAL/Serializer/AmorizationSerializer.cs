/* Copyright (C) 2020-2024 b1f6c1c4
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
///     摊销序列化器
/// </summary>
internal class AmortizationSerializer : BaseSerializer<Amortization, Guid?>
{
    private static readonly AmortItemSerializer ItemSerializer = new();
    private static readonly VoucherSerializer VoucherSerializer = new();

    public override Amortization Deserialize(IBsonReader bsonReader)
    {
        string read = null;

        bsonReader.ReadStartDocument();

        var amort = new Amortization
            {
                ID = bsonReader.ReadGuid("_id", ref read),
                User = bsonReader.ReadString("user", ref read),
                Name = bsonReader.ReadString("name", ref read),
                Value = bsonReader.ReadDouble("value", ref read),
                Date = bsonReader.ReadDateTime("date", ref read),
                TotalDays = bsonReader.ReadInt32("tday", ref read),
            };
        amort.Interval = bsonReader.ReadString("interval", ref read) switch
            {
                "d" => AmortizeInterval.EveryDay,
                "w" => AmortizeInterval.SameDayOfWeek,
                "W" => AmortizeInterval.LastDayOfWeek,
                "m" => AmortizeInterval.SameDayOfMonth,
                "M" => AmortizeInterval.LastDayOfMonth,
                "y" => AmortizeInterval.SameDayOfYear,
                "Y" => AmortizeInterval.LastDayOfYear,
                _ => amort.Interval,
            };

        amort.Template = bsonReader.ReadDocument("template", ref read, VoucherSerializer.Deserialize);
        amort.Schedule = bsonReader.ReadArray("schedule", ref read, ItemSerializer.Deserialize);
        amort.Remark = bsonReader.ReadString("remark", ref read);
        bsonReader.ReadEndDocument();
        return amort;
    }

    public override void Serialize(IBsonWriter bsonWriter, Amortization amort)
    {
        bsonWriter.WriteStartDocument();
        bsonWriter.Write("_id", amort.ID);
        bsonWriter.Write("user", amort.User);
        bsonWriter.Write("name", amort.Name);
        bsonWriter.Write("value", amort.Value);
        bsonWriter.Write("date", amort.Date);
        bsonWriter.Write("tday", amort.TotalDays);
        bsonWriter.Write("interval", amort.Interval switch
            {
                AmortizeInterval.EveryDay => "d",
                AmortizeInterval.SameDayOfWeek => "w",
                AmortizeInterval.LastDayOfWeek => "W",
                AmortizeInterval.SameDayOfMonth => "m",
                AmortizeInterval.LastDayOfMonth => "M",
                AmortizeInterval.SameDayOfYear => "y",
                AmortizeInterval.LastDayOfYear => "Y",
                _ => throw new InvalidOperationException(),
            });

        if (amort.Template != null)
        {
            bsonWriter.WriteName("template");
            VoucherSerializer.Serialize(bsonWriter, amort.Template);
        }

        if (amort.Schedule != null)
        {
            bsonWriter.WriteStartArray("schedule");
            foreach (var item in amort.Schedule)
                ItemSerializer.Serialize(bsonWriter, item);

            bsonWriter.WriteEndArray();
        }

        bsonWriter.Write("remark", amort.Remark);
        bsonWriter.WriteEndDocument();
    }

    public override Guid? GetId(Amortization entity) => entity.ID;
    protected override void SetId(Amortization entity, Guid? id) => entity.ID = id;
    protected override bool IsNull(Guid? id) => !id.HasValue;

    protected override Guid? MakeId(IMongoCollection<Amortization> container, Amortization entity) =>
        Guid.NewGuid();
}
