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
using System.Threading.Tasks;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Synthesizer;

namespace AccountingServer.Shell;

internal class AuthnShell : IShellComponent
{
    private readonly DbSession m_Db;

    public readonly AuthnManager Mgr;

    public AuthnShell(DbSession db)
    {
        m_Db = db;
        Mgr = new(m_Db);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<string> Execute(string expr, Context ctx, string term)
    {
        expr = expr.Rest();
        return expr.Initial() switch
            {
                "" => ListIdentity(ctx, false),
                "invite" => Invite(ctx, expr.Rest().Trim()),
                "list" => ListAuthn(ctx, expr.Rest().Trim()),
                "cert" => SaveCert(ctx, expr.Rest().Trim()),
                "rm" => null,
                _ => throw new InvalidOperationException("表达式无效"),
            };
    }

    /// <inheritdoc />
    public bool IsExecutable(string expr) => expr.Initial() == "an";

    public static async IAsyncEnumerable<string> ListIdentity(Context ctx, bool brief)
    {
        yield return $"Authenticated Identity: {ctx.TrueIdentity.Name.AsId()}\n";
        if (ctx.Identity != ctx.TrueIdentity)
            yield return $"Assume Identity: {ctx.Identity.Name.AsId()}\n";

        if (brief && !ctx.Identity.CanLogin(ctx.Client.User))
            throw new AccessDeniedException($"Selected entity {ctx.Client.User.AsUser()} denied\n");

        yield return $"Selected Entity: {ctx.Client.User.AsUser()}\n\n";

        if (brief)
            yield break;

        yield return "---- Authn details ----\n";
        if (ctx.Session != null)
        {
            yield return "WebAuthn accepted\n";
            yield return $"\tAuthn ID: {ctx.Session.Authn.StringID}\n";
            yield return $"\tInvitedAt: {ctx.Session.Authn.InvitedAt:s}\n";
            yield return $"\tCreatedAt: {ctx.Session.Authn.CreatedAt:s}\n";
            yield return $"\tLastUsedAt: {ctx.Session.Authn.LastUsedAt:s}\n";
            yield return $"\tSession CreatedAt: {ctx.Session.CreatedAt:s}\n";
            yield return $"\tSession ExpiresAt: {ctx.Session.ExpiresAt:s}\n";
            yield return $"\tSession MaxExpiresAt: {ctx.Session.MaxExpiresAt:s}\n";
        }
        else
            yield return "No WebAuthn credential present\n";

        if (ctx.Certificate != null)
        {
            if (ctx.Certificate.ID != null)
            {
                yield return "Client certificate accepted\n";
                yield return $"\tAuthn ID: {ctx.Certificate.StringID}\n";
                yield return $"\tCreatedAt: {ctx.Certificate.CreatedAt:s}\n";
                yield return $"\tLastUsedAt: {ctx.Certificate.LastUsedAt:s}\n";
            }
            else
                yield return "Client certificate acknowledged but not accepted\n";

            yield return $"\tFingerprint: {ctx.Certificate.Fingerprint}\n";
            yield return $"\tSubjectDN: {ctx.Certificate.SubjectDN}\n";
            yield return $"\tIssuerDN: {ctx.Certificate.IssuerDN}\n";
            yield return $"\tSerial: {ctx.Certificate.Serial}\n";
            yield return $"\tValid not Before: {ctx.Certificate.Start}\n";
            yield return $"\tValid not After: {ctx.Certificate.End}\n";
        }
        else
            yield return "No client certificate present\n";

        yield return "---- RBAC details ----\n";
        if (ctx.TrueIdentity.P.AllAssumes == null)
            yield return "Assumable Identities: *\n";
        else
        {
            var assumes = ctx.TrueIdentity.P.AllAssumes.Select(static (s) => s.AsId());
            yield return $"Assumable Identities: {string.Join(", ", assumes)}\n";
        }

        if (ctx.Identity.P.AllKnowns == null)
            yield return "Known Identities: *\n";
        else
        {
            var knowns = ctx.Identity.P.AllKnowns.Select(static (s) => s.AsId());
            yield return $"Known Identities: {string.Join(", ", knowns)}\n";
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

        yield return "Allowable Permissions:\n";
        yield return $"\tView: {Synth(ctx.Identity.P.View.Grant)}\n";
        yield return $"\tEdit: {Synth(ctx.Identity.P.Edit.Grant)}\n";
        yield return $"\tVoucher: {Synth(ctx.Identity.P.Voucher.Grant)}\n";
        yield return $"\tAsset: {Synth(ctx.Identity.P.Asset.Grant)}\n";
        yield return $"\tAmort: {Synth(ctx.Identity.P.Amort.Grant)}\n";

        var invokes = ctx.Identity.P.GrantInvokes.Select(static (r) => r.Quotation('"'));
        yield return $"\tInvokes: {string.Join(", ", invokes)}\n";

        yield return $"Debit/Credit Imbalance: {(ctx.Identity.P.Imba ? "Granted" : "Denied")}\n";
        yield return $"Reflect on Denies: {(ctx.Identity.P.Reflect ? "Granted" : "Denied")}\n";

        if (ctx.Identity.P.Reflect)
        {
            yield return "Full Permissions:\n";
            yield return $"\tView: {Synth(ctx.Identity.P.View.Query)}\n";
            yield return $"\tEdit: {Synth(ctx.Identity.P.Edit.Query)}\n";
            yield return $"\tVoucher: {Synth(ctx.Identity.P.Voucher.Query)}\n";
            yield return $"\tAsset: {Synth(ctx.Identity.P.Asset.Query)}\n";
            yield return $"\tAmort: {Synth(ctx.Identity.P.Amort.Query)}\n";
        }

        yield return $"Client Date: {ctx.Client.Today:s}\n";
        yield return $"Server Date: {DateTime.UtcNow:s}\n";
    }

    public async IAsyncEnumerable<string> Invite(Context ctx, string id)
    {
        ctx.Identity.WillInvoke("invite");
        var aid = await Mgr.InviteWebAuthn(id);
        yield return $"Invitation created, please visit the following link:\n";
        yield return $"https://{AuthnManager.Server}/invite?q={aid.StringID}\n";
        yield return $"This link will exire at {aid.ExpiresAt:s} UTC time.\n";
    }

    public async IAsyncEnumerable<string> ListAuthn(Context ctx, string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            ctx.TrueIdentity.WillInvoke("an-list-other");
        }
        else if (ctx.Identity == ctx.TrueIdentity)
        {
            ctx.TrueIdentity.WillInvoke("an-list-self");
            id = ctx.TrueIdentity.Name;
        }
        else
        {
            ctx.TrueIdentity.WillInvoke("an-list-other");
            id = ctx.Identity.Name;
        }

        yield return $"Authentication methods for {id.AsId()}:\n";
        await foreach (var aid in m_Db.SelectAuths(id))
        {
            if (aid is WebAuthn wa)
            {
                yield return $"{aid.StringID} WebAuthn (created at {aid.CreatedAt:s}; last used at {aid.LastUsedAt:s})\n";
                if (wa.AttestationOptions != null)
                {
                    yield return $"\tPending invitation since {wa.InvitedAt:s}";
                    continue;
                }

                yield return $"\tCredential Id = {wa.CredentialId}\n";
                yield return $"\tBackup {(wa.IsBackupEligible ? "" : "NOT")} eligible;";
                yield return $"Is {(wa.IsBackedUp ? "" : "NOT")} backed up\n";
                yield return $"\tAttestation format: {wa.AttestationFormat}\n";
                yield return $"\tTransports: {string.Join(" ", wa.Transports)}\n\n";
                continue;
            }
            if (aid is CertAuthn ca)
            {
                yield return $"{aid.StringID} CertAuthn (created at {aid.CreatedAt:s}; last used at {aid.LastUsedAt:s})\n";
                yield return $"\tFingerprint: {ctx.Certificate.Fingerprint}\n";
                yield return $"\tSubjectDN: {ctx.Certificate.SubjectDN}\n";
                yield return $"\tIssuerDN: {ctx.Certificate.IssuerDN}\n";
                yield return $"\tSerial: {ctx.Certificate.Serial}\n";
                yield return $"\tValid not Before: {ctx.Certificate.Start}\n";
                yield return $"\tValid not After: {ctx.Certificate.End}\n\n";
                continue;
            }
            yield return $"{aid.StringID} Unknown (created at {aid.CreatedAt:s}; last used at {aid.LastUsedAt:s})\n\n";
        }
    }

    private async IAsyncEnumerable<string> SaveCert(Context ctx, string id)
    {
        if (ctx.Certificate == null || string.IsNullOrEmpty(ctx.Certificate.Fingerprint))
            throw new ApplicationException("No certifiacte present");

        if (!string.IsNullOrEmpty(id))
        {
            ctx.TrueIdentity.WillInvoke("an-cert-other");
            ctx.Certificate.IdentityName = id;
        }
        else if (ctx.Identity == ctx.TrueIdentity)
        {
            ctx.TrueIdentity.WillInvoke("an-cert-self");
            ctx.Certificate.IdentityName = ctx.TrueIdentity.Name;
        }
        else
        {
            ctx.TrueIdentity.WillInvoke("an-cert-other");
            ctx.Certificate.IdentityName = ctx.Identity.Name;
        }

        await Mgr.RegisterCert(ctx.Certificate);

        yield return $"Certificate saved for identity {ctx.Identity.Name.AsId()}, you can now authenticate\n";
        yield return $"using certificate {ctx.Certificate.Fingerprint}\n";
        yield return $"Auth: {ctx.Certificate.ID}\n";
    }
}
