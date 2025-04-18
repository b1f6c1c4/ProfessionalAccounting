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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Fido2NetLib;
using Fido2NetLib.Objects;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.Shell;

[Serializable]
[XmlRoot("Authentication")]
public class AuthConfig
{
    public string ServerDomain;

    public string ServerName;

    [XmlElement("Origin")]
    public List<string> Origins;

    public string JwtSecret;
}

public class Authentication
{
    private static Dictionary<string, CredentialCreateOptions> m_PendingCredentials = new();

    static Authentication()
        => Cfg.RegisterType<AuthConfig>("Authn");

    private DbSession m_Db;

    public Authentication(DbSession db) => m_Db = db;

    private Fido2 Make()
    {
        var cfg = Cfg.Get<AuthConfig>();
        return new Fido2(new Fido2Configuration
            {
                ServerDomain = cfg.ServerDomain,
                ServerName = cfg.ServerName,
                Origins = new HashSet<string>(cfg.Origins),
            });
    }

    public async IAsyncEnumerable<string> CreateAttestationOptions(string display)
    {
        if (string.IsNullOrWhiteSpace(display))
            throw new ApplicationException("The displayName must not be empty");

        var aid = new AuthIdentity
            {
                ID = RandomNumberGenerator.GetBytes(24),
                DisplayName = display,
            };
        var user = new Fido2User
            {
                DisplayName = display,
                Name = display,
                Id = aid.ID,
            };

        var options = Make().RequestNewCredential(new()
            {
                User = user,
                ExcludeCredentials = new List<PublicKeyCredentialDescriptor>(),
                AuthenticatorSelection = new()
                    {
                        ResidentKey = ResidentKeyRequirement.Required,
                        UserVerification = UserVerificationRequirement.Required,
                    },
                AttestationPreference = AttestationConveyancePreference.None,
                Extensions = new AuthenticationExtensionsClientInputs
                    {
                        Extensions = true,
                        UserVerificationMethod = true,
                        CredProps = true
                    },
            });

        m_PendingCredentials[aid.StringID] = options;

        aid.AttestationOptions = options.ToJson();

        await m_Db.Upsert(aid);

        yield return $"AuthIdentity prepared for registration; use the following link:\n";
        yield return $"https://<host>:<port>/invite/{aid.StringID}\n";
    }

    public async ValueTask<bool> RegisterCredentials(byte[] name, string attestation)
    {
        var aid = await m_Db.SelectAuth(name);
        if (aid == null)
            return false;

        var ar = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(attestation)!;
        var credential = await Make().MakeNewCredentialAsync(new()
                {
                    AttestationResponse = ar,
                    OriginalOptions = m_PendingCredentials[aid.StringID],
                    IsCredentialIdUniqueToUserCallback = async (args, _)
                        => (await m_Db.SelectAuthCredential(args.CredentialId)) == null,
                });

        aid.AttestationOptions = null;
        aid.CredentialId = credential.Id;
        aid.PublicKey = credential.PublicKey;
        aid.SignCount = credential.SignCount;

        await m_Db.Upsert(aid);

        m_PendingCredentials.Remove(aid.StringID);

        return true;
    }
}
