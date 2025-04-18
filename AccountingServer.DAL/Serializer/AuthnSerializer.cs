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

internal class AuthnSerializer : BaseSerializer<Authn>
{
    public override Authn Deserialize(IBsonReader bsonReader)
    {
        string read = null;

        bsonReader.ReadStartDocument();
        var id = bsonReader.ReadBinaryData("_id", ref read);
        var name = bsonReader.ReadString("identityName", ref read);
        var created = bsonReader.ReadDateTime("createdAt", ref read);
        var used = bsonReader.ReadDateTime("lastUsedAt", ref read);
        var type = bsonReader.ReadString("type", ref read);
        Authn aid;
        switch (type)
        {
            case "webauthn":
                aid = new WebAuthn
                    {
                        ID = id,
                        IdentityName = name,
                        CreatedAt = created,
                        LastUsedAt = used,
                        AttestationOptions = bsonReader.ReadString("attestationOptions", ref read),
                        CredentialId = bsonReader.ReadBinaryData("credentialId", ref read),
                        PublicKey = bsonReader.ReadBinaryData("publicKey", ref read),
                        SignCount = bsonReader.ReadUInt32("signCount", ref read),
                        InvitedAt = bsonReader.ReadDateTime("invitedAt", ref read),
                    };
                break;
            case "cert":
                aid = new CertAuthn
                    {
                        ID = id,
                        IdentityName = name,
                        CreatedAt = created,
                        LastUsedAt = used,
                        IssuerDN = bsonReader.ReadString("issuer", ref read),
                        SubjectDN = bsonReader.ReadString("subject", ref read),
                        Serial = bsonReader.ReadString("serial", ref read),
                        Fingerprint = bsonReader.ReadString("fingerprint", ref read),
                        Start = bsonReader.ReadString("start", ref read),
                        End = bsonReader.ReadString("end", ref read),
                    };
                break;
            default:
                throw new ArgumentOutOfRangeException();
        };
        bsonReader.ReadEndDocument();

        return aid;
    }

    public override void Serialize(IBsonWriter bsonWriter, Authn aid)
    {
        bsonWriter.WriteStartDocument();
        bsonWriter.Write("_id", aid.ID);
        bsonWriter.Write("identityName", aid.IdentityName);
        bsonWriter.Write("createdAt", aid.CreatedAt);
        bsonWriter.Write("lastUsedAt", aid.LastUsedAt);
        if (aid is WebAuthn wa)
        {
            bsonWriter.Write("attestationOptions", wa.AttestationOptions);
            bsonWriter.Write("type", "webauthn");
            bsonWriter.Write("credentialId", wa.CredentialId);
            bsonWriter.Write("publicKey", wa.PublicKey);
            bsonWriter.Write("signCount", wa.SignCount);
            bsonWriter.Write("invitedAt", wa.InvitedAt);
        }
        else if (aid is CertAuthn ca)
        {
            bsonWriter.Write("type", "cert");
            bsonWriter.Write("issuer", ca.IssuerDN);
            bsonWriter.Write("subject", ca.SubjectDN);
            bsonWriter.Write("serial", ca.Serial);
            bsonWriter.Write("fingerprint", ca.Fingerprint);
            bsonWriter.Write("start", ca.Start);
            bsonWriter.Write("end", ca.End);
        }
        bsonWriter.WriteEndDocument();
    }
}
