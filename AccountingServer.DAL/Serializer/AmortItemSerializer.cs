/* Copyright (C) 2020 b1f6c1c4
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

namespace AccountingServer.DAL.Serializer
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
                    Remark = bsonReader.ReadString("remark", ref read),
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
