/* Copyright (C) 2025 b1f6c1c4
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

internal class AuthIdentitySerializer : BaseSerializer<AuthIdentity>
{
    public override AuthIdentity Deserialize(IBsonReader bsonReader)
    {
        string read = null;

        bsonReader.ReadStartDocument();
        var aid = new AuthIdentity
        {
            ID = bsonReader.ReadBinaryData("_id", ref read),
            CredentialId = bsonReader.ReadBinaryData("credentialId", ref read),
            PublicKey = bsonReader.ReadBinaryData("publicKey", ref read),
            SignCount = bsonReader.ReadUInt32("signCount", ref read),
            AttestationOptions = bsonReader.ReadString("attestationOptions", ref read),
        };
        bsonReader.ReadEndDocument();

        return aid;
    }

    public override void Serialize(IBsonWriter bsonWriter, AuthIdentity aid)
    {
        bsonWriter.WriteStartDocument();
        bsonWriter.Write("_id", aid.ID);
        bsonWriter.Write("credentialId", aid.CredentialId);
        bsonWriter.Write("publicKey", aid.PublicKey);
        bsonWriter.Write("signCount", aid.SignCount);
        bsonWriter.Write("attestationOptions", aid.AttestationOptions);
        bsonWriter.WriteEndDocument();
    }
}
