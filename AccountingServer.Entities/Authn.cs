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
using System.Text;

namespace AccountingServer.Entities;

public abstract class Authn
{
    public byte[] ID { get; set; }

    public string StringID
    {
        get => Convert.ToBase64String(ID).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        set
        {
            var padded = value.Replace('-', '+').Replace('_', '/');

            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }

            ID = Convert.FromBase64String(padded);
        }
    }

    public string IdentityName { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }
}

public class WebAuthn : Authn
{
    public string AttestationOptions { get; set; }
    public byte[] CredentialId { get; set; }
    public byte[] PublicKey { get; set; }
    public uint? SignCount { get; set; }
    public DateTime? InvitedAt { get; set; }
}

public class CertAuthn : Authn
{
    public string Fingerprint { get; set; }
    public string IssuerDN { get; set; }
    public string SubjectDN { get; set; }
    public string Serial { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
}
