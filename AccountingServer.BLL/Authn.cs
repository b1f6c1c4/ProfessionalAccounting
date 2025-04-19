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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Fido2NetLib;
using Fido2NetLib.Objects;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL;

[Serializable]
[XmlRoot("WebAuthn")]
public class WebAuthnConfig
{
    public string ServerDomain;

    public string ServerPort;

    public string ServerName;

    [XmlElement("Origin")]
    public List<string> Origins;

    public TimeSpan SessionExpire;

    public TimeSpan SessionMax;
}

public class AuthnManager
{
    private static Dictionary<string, CredentialCreateOptions> m_PendingCredentials = new();
    private static Dictionary<string, AssertionOptions> m_PendingAssertions = new();

    static AuthnManager()
        => Cfg.RegisterType<WebAuthnConfig>("Authn");

    public static WebAuthnConfig Config => Cfg.Get<WebAuthnConfig>();

    private DbSession m_Db;

    public AuthnManager(DbSession db) => m_Db = db;

    private Fido2 F()
    {
        var cfg = Cfg.Get<WebAuthnConfig>();
        return new Fido2(new Fido2Configuration
            {
                ServerDomain = cfg.ServerDomain,
                ServerName = cfg.ServerName,
                Origins = new HashSet<string>(cfg.Origins),
            });
    }

    public async ValueTask<WebAuthn> InviteWebAuthn(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ApplicationException("Identity name must not be empty");

        var aid = new WebAuthn
            {
                ID = RandomNumberGenerator.GetBytes(24),
                IdentityName = name,
            };

        var options = F().RequestNewCredential(new()
            {
                User = new()
                    {
                        DisplayName = name,
                        Name = name,
                        Id = aid.ID,
                    },
                ExcludeCredentials = new List<PublicKeyCredentialDescriptor>(),
                AuthenticatorSelection = new()
                    {
                        ResidentKey = ResidentKeyRequirement.Required,
                        UserVerification = UserVerificationRequirement.Required,
                    },
                AttestationPreference = AttestationConveyancePreference.None,
                Extensions = new()
                    {
                        Extensions = true,
                        UserVerificationMethod = true,
                        CredProps = true
                    },
            });

        m_PendingCredentials[aid.StringID] = options;

        aid.AttestationOptions = options.ToJson();
        aid.InvitedAt = DateTime.UtcNow;

        await m_Db.Insert(aid);

        return aid;
    }

    public async ValueTask RegisterCert(CertAuthn ca)
    {
        ca.ID = RandomNumberGenerator.GetBytes(24);
        ca.CreatedAt = DateTime.UtcNow;
        await m_Db.Insert(ca);
    }

    public async ValueTask<bool> RegisterWebAuthn(byte[] name, string attestation)
    {
        var aid = (await m_Db.SelectAuth(name)) as WebAuthn;
        if (aid == null)
            throw new ApplicationException("No AuthIdentity found");

        var ar = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(attestation);
        if (ar == null)
            throw new ApplicationException("Invalid json AuthenticatorAttestationRawResponse");

        if (!m_PendingCredentials.TryGetValue(aid.StringID, out var options))
            throw new ApplicationException("No pending credential found");

        var credential = await F().MakeNewCredentialAsync(new()
                {
                    AttestationResponse = ar,
                    OriginalOptions = options,
                    IsCredentialIdUniqueToUserCallback = async (args, _)
                        => (await m_Db.SelectWebAuthn(args.CredentialId)) == null,
                });

        aid.CreatedAt = DateTime.UtcNow;
        aid.AttestationOptions = null;
        aid.CredentialId = credential.Id;
        aid.PublicKey = credential.PublicKey;
        aid.SignCount = credential.SignCount;

        if (!await m_Db.Update(aid))
            throw new ApplicationException("Update failed");

        m_PendingCredentials.Remove(aid.StringID);

        return true;
    }

    public string CreateChallenge()
    {
        var options = F().GetAssertionOptions(new()
            {
                AllowedCredentials = new List<PublicKeyCredentialDescriptor>(),
                UserVerification = UserVerificationRequirement.Discouraged,
                Extensions = new()
                    {
                        Extensions = true,
                        UserVerificationMethod = true,
                    },
            });

        m_PendingAssertions[new string(options.Challenge.Select(b => (char)b).ToArray())] = options;

        return options.ToJson();
    }

    public async ValueTask<WebAuthn> VerifyResponse(string assertion)
    {
        var ar = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(assertion);
        if (ar == null)
            throw new ApplicationException("Invalid json AuthenticatorAssertionRawResponse");

        var response = JsonSerializer.Deserialize<AuthenticatorResponse>(ar.Response.ClientDataJson);
        if (response == null)
            throw new ApplicationException("Invalid json AuthenticatorResponse");

        var key = new string(response.Challenge.Select(b => (char)b).ToArray());
        if (!m_PendingAssertions.TryGetValue(key, out var options))
            throw new ApplicationException("No pending assertion found");

        m_PendingAssertions.Remove(key);

        var aid = await m_Db.SelectWebAuthn(ar.Id);
        if (aid == null)
            throw new ApplicationException("No AuthIdentity found");

        var res = await F().MakeAssertionAsync(new()
                {
                    AssertionResponse = ar,
                    OriginalOptions = options,
                    StoredPublicKey = aid.PublicKey,
                    StoredSignatureCounter = aid.SignCount!.Value,
                    IsUserHandleOwnerOfCredentialIdCallback = (args, _)
                        => Task.FromResult(aid.CredentialId.SequenceEqual(args.CredentialId)),
                });

        aid.LastUsedAt = DateTime.UtcNow;
        aid.SignCount = res.SignCount;
        if (!await m_Db.Update(aid))
            throw new ApplicationException("Update failed");

        return aid;
    }
}
