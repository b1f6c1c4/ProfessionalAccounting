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
using System.Collections.Generic;
using System.Text;

namespace AccountingServer.Entities;

public class Authn
{
    public byte[] ID { get; set; }

    public static byte[] FromBytes(string str)
    {
        var padded = str.Replace('-', '+').Replace('_', '/');

        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }


    public string StringID
    {
        get => Convert.ToBase64String(ID).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        set => ID = FromBytes(value);
    }

    public string IdentityName;

    public DateTime? CreatedAt;

    public DateTime? LastUsedAt;
}

public class WebAuthn : Authn
{
    public string AttestationOptions;

    public byte[] CredentialId;
    public byte[] PublicKey;
    public uint? SignCount;
    public DateTime? InvitedAt;

    // some additional information
    public bool IsBackupEligible, IsBackedUp;
    public string AttestationFormat;
    public List<string> Transports;

    public DateTime? ExpiresAt; // not stored in db
}

public class CertAuthn : Authn
{
    public string Fingerprint;
    public string IssuerDN;
    public string SubjectDN;
    public string Serial;
    public string Start;
    public string End;
}
