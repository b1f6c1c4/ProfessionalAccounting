/* Copyright (C) 2020-2025 b1f6c1c4
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
using System.Threading.Tasks;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using static AccountingServer.BLL.Parsing.Synthesizer;

namespace AccountingServer.Shell;

public partial class Facade
{
    public string ServerName => AuthnManager.Config.ServerName;

    public string Server => AuthnManager.Config.ServerPort == 443
        ? AuthnManager.Config.ServerDomain
        : $"{AuthnManager.Config.ServerDomain}:{AuthnManager.Config.ServerPort}";

    public async ValueTask<string> GetATChallenge(string userHandle)
        => (await m_Db.SelectAuth(Authn.FromBytes(userHandle)) as WebAuthn)?.AttestationOptions;

    public ValueTask<bool> RegisterWebAuthn(string userHandle, string body)
        => m_Auth.RegisterWebAuthn(Authn.FromBytes(userHandle), body);

    public async ValueTask<string> CreateLogin()
        => m_Auth.CreateLogin();

    public async ValueTask<Session> VerifyLogin(string body)
    {
        var aid = await m_Auth.VerifyResponse(body);
        if (aid == null)
            return null;

        return m_SessionManager.CreateSession(aid);
    }

    public async ValueTask<Context> AuthnCtx(string user, DateTime dt,
            string sessionKey = null, CertAuthn cert = null,
            string assume = null, string spec = null, int limit = 0, IEnumerable<Authn> aux = null)
    {
        var session = m_SessionManager.AccessSession(sessionKey);
        var ca = await m_Db.SelectCertAuthn(cert?.Fingerprint);
        if (ca != null)
        {
            ca.LastUsedAt = DateTime.UtcNow;
            await m_Db.Update(ca);
        }

        var creds = new List<Authn> { session?.Authn, ca };
        if (aux != null)
            creds.AddRange(aux);
        var id = ACLManager.Authenticate(creds, assume);
        return new(m_Db, user, dt, id.Assume(assume), id, session, ca ?? cert, spec, limit);
    }

    private async IAsyncEnumerable<string> Invite(Context ctx, string name)
    {
        ctx.Identity.WillInvoke("invite");
        var aid = await m_Auth.InviteWebAuthn(name);
        yield return $"Invitation created, please visit the following link:\n";
        yield return $"https://{Server}/invite?q={aid.StringID}\n";
    }

    private async IAsyncEnumerable<string> Logout(Context ctx)
    {
        if (ctx.Session == null)
            yield return "No active session -- you've never login\n";

        m_SessionManager.DiscardSession(ctx.Session);
        yield return "Session discarded";
    }

    private async IAsyncEnumerable<string> SaveCert(Context ctx)
    {
        if (ctx.Certificate == null || string.IsNullOrEmpty(ctx.Certificate.Fingerprint))
            throw new ApplicationException("No certifiacte present");

        ctx.Identity.WillInvoke("save-cert");
        if (ctx.Identity != ctx.TrueIdentity)
            ctx.TrueIdentity.WillInvoke("save-cert-assume");

        ctx.Certificate.IdentityName = ctx.Identity.Name;
        await m_Auth.RegisterCert(ctx.Certificate);

        yield return $"Certificate saved for identity {ctx.Identity.Name.AsId()}, it can now authenticate\n";
        yield return $"using certificate {ctx.Certificate.Fingerprint}\n";
    }

    private static async IAsyncEnumerable<string> ListIdentity(Context ctx)
    {
        if (ctx.Session != null)
        {
            yield return "WebAuthn accepted\n";
            yield return $"WebAuthn Authn ID: {ctx.Session.Authn.StringID}\n";
            yield return $"WebAuthn InvitedAt: {ctx.Session.Authn.InvitedAt:s}\n";
            yield return $"WebAuthn CreatedAt: {ctx.Session.Authn.CreatedAt:s}\n";
            yield return $"WebAuthn LastUsedAt: {ctx.Session.Authn.LastUsedAt:s}\n";
            yield return $"Session CreatedAt: {ctx.Session.CreatedAt:s}\n";
            yield return $"Session ExpiresAt: {ctx.Session.ExpiresAt:s}\n";
            yield return $"Session MaxExpiresAt: {ctx.Session.MaxExpiresAt:s}\n";
        }

        if (ctx.Certificate != null)
        {
            if (ctx.Certificate.ID != null)
            {
                yield return "Client certificate accepted\n";
                yield return $"Certificate Authn ID: {ctx.Certificate.StringID}\n";
                yield return $"Certificate CreatedAt: {ctx.Certificate.CreatedAt:s}\n";
                yield return $"Certificate LastUsedAt: {ctx.Certificate.LastUsedAt:s}\n";
            }
            else
                yield return "Client certificate acknowledged but not accepted\n";

            yield return $"Certificate Fingerprint: {ctx.Certificate.Fingerprint}\n";
            yield return $"Certificate SubjectDN: {ctx.Certificate.SubjectDN}\n";
            yield return $"Certificate IssuerND: {ctx.Certificate.IssuerDN}\n";
            yield return $"Certificate Serial: {ctx.Certificate.Serial}\n";
            yield return $"Certificate Valid not Before: {ctx.Certificate.Start}\n";
            yield return $"Certificate Valid not After: {ctx.Certificate.End}\n";
        }

        yield return $"Authenticated Identity: {ctx.TrueIdentity.Name.AsId()}\n";

        if (ctx.TrueIdentity.P.AllAssumes == null)
            yield return "Assumable Identies: *\n";
        else
        {
            var assumes = ctx.TrueIdentity.P.AllAssumes.Select(static (s) => s.AsId());
            yield return $"Assumable Identies: {string.Join(", ", assumes)}\n";
        }

        yield return $"Assume Identity: {ctx.Identity.Name.AsId()}\n";

        if (ctx.Identity.P.AllKnowns == null)
            yield return "Known Identies: *\n";
        else
        {
            var knowns = ctx.Identity.P.AllKnowns.Select(static (s) => s.AsId());
            yield return $"Known Identies: {string.Join(", ", knowns)}\n";
        }

        if (ctx.Identity.P.AllRoles == null)
            yield return "Associated Roles: *\n";
        else
        {
            var roles = ctx.Identity.P.AllRoles.Select(static (r) => r.Name.AsId());
            yield return $"Associated Roles: {string.Join(", ", roles)}\n";
        }

        if (ctx.Identity.P.AllUsers == null)
            yield return "Associated Entities: *\n";
        else
        {
            var users = ctx.Identity.P.AllUsers.Select(static (s) => s.AsUser());
            yield return $"Associated Entities: {string.Join(", ", users)}\n";
        }

        if (ctx.Identity.CanLogin(ctx.Client.User))
            yield return $"Selected Entity: {ctx.Client.User.AsUser()}\n";
        else
            yield return $"!!Access to {ctx.Client.User.AsUser()} denied, try `entity` + one of the above!!\n";

        yield return $"Current Date: {ctx.Client.Today.AsDate()}\n";

        yield return $"Allowable View: {Synth(ctx.Identity.P.View.Grant)}\n";
        yield return $"Allowable Edit: {Synth(ctx.Identity.P.Edit.Grant)}\n";
        yield return $"Allowable Voucher: {Synth(ctx.Identity.P.Voucher.Grant)}\n";
        yield return $"Allowable Asset: {Synth(ctx.Identity.P.Asset.Grant)}\n";
        yield return $"Allowable Amort: {Synth(ctx.Identity.P.Amort.Grant)}\n";

        var invokes = ctx.Identity.P.GrantInvokes.Select(static (r) => r.Quotation('"'));
        yield return $"Allowable Invokes: {string.Join(", ", invokes)}\n";

        yield return $"Debit/Credit Imbalance: {(ctx.Identity.P.Imba ? "Granted" : "Denied")}\n";
        yield return $"Reflect on Denies: {(ctx.Identity.P.Reflect ? "Granted" : "Denied")}\n";

        if (ctx.Identity.P.Reflect)
        {
            yield return $"Full View: {Synth(ctx.Identity.P.View.Query)}\n";
            yield return $"Full Edit: {Synth(ctx.Identity.P.Edit.Query)}\n";
            yield return $"Full Voucher: {Synth(ctx.Identity.P.Voucher.Query)}\n";
            yield return $"Full Asset: {Synth(ctx.Identity.P.Asset.Query)}\n";
            yield return $"Full Amort: {Synth(ctx.Identity.P.Amort.Query)}\n";
        }
    }
}
