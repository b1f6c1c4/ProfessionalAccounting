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
using System.Text;
using System.Xml.Serialization;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell;

[Serializable]
[XmlRoot("ACL")]
public class ACL
{
    [XmlElement("Identity")]
    public List<Identity> Identities { get; set; }

    [XmlElement("Role")]
    public List<Role> Roles { get; set; }

    [XmlIgnore]
    internal Dictionary<string, Identity> IdentityMatrix { get; set; }

    [XmlIgnore]
    internal Dictionary<string, Role> RoleMatrix { get; set; }
}

[Serializable]
public class Role
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlElement("Role")]
    public List<string> Inherits { get; set; }

    [XmlElement("Entity")]
    public List<string> Users { get; set; }

    [XmlElement("Assume")]
    public List<string> Assumes { get; set; }

    [XmlElement("Known")]
    public List<string> Knowns { get; set; }

    [XmlElement("Grant")]
    public List<Permission> Grants { get; set; }

    [XmlElement("Deny")]
    public List<Permission> Denies { get; set; }

    [XmlElement("Reject")]
    public List<Permission> Rejects { get; set; }

    [XmlIgnore]
    public Permissions P { get; internal set; }
}

public class Permissions
{
    public bool Imba { get; internal set; }
    public bool Reflect { get; internal set; }
    public HashSet<Role> AllRoles { get; internal set; }
    public HashSet<string> AllUsers { get; internal set; }
    public HashSet<string> AllAssumes { get; internal set; }
    public HashSet<string> AllKnowns { get; internal set; }
    public List<string> GrantInvokes { get; internal set; }
    public ACLQuery<IDetailQueryAtom> View { get; internal set; }
    public ACLQuery<IDetailQueryAtom> Edit { get; internal set; }
    public ACLQuery<IVoucherQueryAtom> Voucher { get; internal set; }
    public ACLQuery<IDistributedQueryAtom> Asset { get; internal set; }
    public ACLQuery<IDistributedQueryAtom> Amort { get; internal set; }
}

public struct ACLQuery<TAtom> where TAtom : class
{
    internal IQueryCompounded<TAtom> Reject { get; set; }
    public IQueryCompounded<TAtom> Query { get; internal set; } // used for actual filteration
    public IQueryCompounded<TAtom> Grant { get; internal set; } // visible to non-reflect users
}

[Serializable]
public class Identity : Role
{
    [XmlElement("IdP")]
    public IdentityProvider IdP { get; set; }
}

[Serializable]
public class IdPEntry
{
    [XmlElement("Subject")]
    public string Subject;

    [XmlElement("CN")]
    public string CN;

    [XmlElement("Issuer")]
    public string Issuer;

    [XmlElement("Serial")]
    public string Serial;

    [XmlElement("Fingerprint")]
    public string Fingerprint;
}

[Serializable]
public class IdentityProvider : IdPEntry
{
    [XmlElement("OR")]
    public List<IdPEntry> ORs { get; set; }
}

[Flags]
[Serializable]
public enum Verb
{
    None = 0b0000_0000,

    [XmlEnum("view")]
    View = 0b0000_0001,

    [XmlEnum("edit")]
    Edit = 0b0000_0011,

    [XmlEnum("imba")]
    Imbalance = 0b0000_0100,

    [XmlEnum("voucher")]
    Voucher = 0b0000_1000,

    [XmlEnum("invoke")]
    Invoke = 0b0001_0000,

    [XmlEnum("asset")]
    Asset = 0b0010_0000,

    [XmlEnum("amort")]
    Amort = 0b0100_0000,

    [XmlEnum("reflect")]
    Reflect = 0b1000_0000,
}

[Serializable]
public class Permission
{
    [XmlAttribute("action")]
    public Verb Action { get; set; }

    [XmlText]
    public string Query { get; set; }
}

public static class ACLManager
{
    static ACLManager()
        => Cfg.RegisterType<ACL>("ACL");

    private static Permissions Stub = new();

    public static Role GetRole(string name)
    {
        var acl = Cfg.Get<ACL>();
        if (acl.RoleMatrix == null)
        {
            var dict = new Dictionary<string, Role>();
            foreach (var role in acl.Roles)
                dict.Add(role.Name, role);

            acl.RoleMatrix = dict;
        }
        var id = acl.RoleMatrix[name];
        PrepareRole(id);
        return id;
    }

    private static bool Flags(this Verb a, Verb b)
        => (a & b) != Verb.None;

    public static void PrepareRole(Role id)
    {
        if (id.P == Stub)
            throw new ApplicationException($"Loop detected processing {id.Name.AsId()} in ACL.xml");

        if (id.P != null)
            return;

        id.P = Stub;

        var p = new Permissions();
        p.AllRoles = new();
        p.AllUsers = new(id.Users);
        p.AllAssumes = new(id.Assumes);
        p.AllKnowns = new(id.Knowns);
        p.GrantInvokes = id.Grants.Where(static (p) => p.Action.HasFlag(Verb.Invoke))
            .Select(static (p) => p.Query).ToList();

        void Merge(HashSet<string> a, HashSet<string> b)
        {
            if (b == null)
                a.Add("*");
            else
                foreach (var v in b)
                    a.Add(v);
        }

        foreach (var nm in id.Inherits)
        {
            var role = GetRole(nm);
            p.AllRoles.Add(role);
            p.GrantInvokes.AddRange(role.P.GrantInvokes);
            foreach (var v in role.P.AllRoles)
                p.AllRoles.Add(v);
            Merge(p.AllUsers, role.P.AllUsers);
            Merge(p.AllAssumes, role.P.AllAssumes);
            Merge(p.AllKnowns, role.P.AllKnowns);
        }
        if (p.AllUsers.Contains("*"))
            p.AllUsers = null;
        if (p.AllAssumes.Contains("*"))
            p.AllAssumes = null;
        if (p.AllKnowns.Contains("*"))
            p.AllKnowns = null;

        bool ComputeB(Verb f)
            => Recursive((id) =>
                id.Grants.Any((p) => p.Action.HasFlag(f))
                && !id.Denies.Any((p) => p.Action.Flags(f)), id)
            && !Recursive((id) => id.Rejects.Any((p) => p.Action.Flags(f)), id);
        p.Imba = ComputeB(Verb.Imbalance);
        p.Reflect = ComputeB(Verb.Reflect);

        ACLQuery<T> Compute<T>(Verb f, Func<Permission, IQueryCompounded<T>> proj, Func<Role, ACLQuery<T>> proj2)
            where T : class
        {
            if (id.Inherits.Count == 0)
            {
                var g = id.Grants.Where((p) => p.Action.HasFlag(f)).Select(proj).QueryAny();
                return new()
                    {
                        Grant = g,
                        Reject = id.Rejects.Where((p) => p.Action.Flags(f)).Select(proj).QueryAny(),
                        Query = g
                            .QueryBut(id.Denies.Where((p) => p.Action.Flags(f)).Select(proj))
                            .QueryBut(id.Rejects.Where((p) => p.Action.Flags(f)).Select(proj)),
                    };
            }
            else
            {
                var lst = id.Inherits.Select((nm) => proj2(GetRole(nm)).Reject)
                    .Concat(id.Rejects.Where((p) => p.Action.Flags(f)).Select(proj)).ToList();
                return new()
                    {
                        Reject = lst.QueryAny(),
                        Grant = id.Inherits.Select((nm) => proj2(GetRole(nm)).Grant).QueryAny()
                            .QueryAny(id.Grants.Where((p) => p.Action.HasFlag(f)).Select(proj)),
                        Query = id.Inherits.Select((nm) => proj2(GetRole(nm)).Query).QueryAny()
                            .QueryAny(id.Grants.Where((p) => p.Action.HasFlag(f)).Select(proj))
                            .QueryBut(id.Denies.Where((p) => p.Action.Flags(f)).Select(proj))
                            .QueryBut(lst),
                    };
            }
        }

        var dq = static (Permission p) => ParsingF.PureDetailQuery(p.Query, new());
        var vq = static (Permission p) => ParsingF.VoucherQuery(p.Query, new());
        var aq = static (Permission p) => ParsingF.DistributedQuery(p.Query, new());
        p.View = Compute(Verb.View, dq, (r) => r.P.View);
        p.Edit = Compute(Verb.Edit, dq, (r) => r.P.Edit);
        p.Voucher = Compute(Verb.Voucher, vq, (r) => r.P.Voucher);
        p.Asset = Compute(Verb.Asset, aq, (r) => r.P.Asset);
        p.Amort = Compute(Verb.Amort, aq, (r) => r.P.Amort);

        id.P = p;
        if (id is Identity)
            Console.WriteLine($"Prepared identity {id.Name}");
        else
            Console.WriteLine($"Prepared role {id.Name}");
    }

    private static bool Recursive(Predicate<Role> pred, Role id0)
        => id0 != null && (pred(id0) || id0.Inherits.Any((nm) => Recursive(pred,
                        Cfg.Get<ACL>().Roles.SingleOrDefault((id) => id.Name == nm))));

    public static bool IsMatch(IdentityProvider login, IdPEntry idp)
    {
        if (login == null)
            return false;

        bool Compare(string a, string b)
        {
            if (a == null)
                return true;

            if (a == "")
                return string.IsNullOrWhiteSpace(b);

            return a.Equals(b, StringComparison.Ordinal);
        }

        bool Compares(IdPEntry tmpl)
        {
            if (!Compare(tmpl.Subject, idp.Subject)) return false;
            if (!Compare(tmpl.CN, idp.CN)) return false;
            if (!Compare(tmpl.Issuer, idp.Issuer)) return false;
            if (!Compare(tmpl.Serial, idp.Serial)) return false;
            if (!Compare(tmpl.Fingerprint, idp.Fingerprint)) return false;
            return true;
        }

        if (!Compares(login))
            return false;

        if (login.ORs != null && login.ORs.Count > 0)
            return login.ORs.Any(Compares);

        return true;
    }

    public static Identity Authenticate(IdPEntry idp, string req = null)
    {
        var acl = Cfg.Get<ACL>();
        if (acl.IdentityMatrix == null)
        {
            var dict = new Dictionary<string, Identity>();
            foreach (var id in acl.Identities)
                dict.Add(id.Name, id);

            acl.IdentityMatrix = dict;
        }

        var lst = acl.Identities.Where((id) => IsMatch(id.IdP, idp)).ToList();
        if (lst.Count == 0)
            throw new ApplicationException("Authentication failure: no identity matches your credential");

        if (lst.Count > 1)
        {
            var tmp = lst.Where((id) => id.Name == req).ToList();
            if (tmp.Count != 1)
            {
                var nms = string.Join(", ", lst.Select(static (id) => id.Name));
                throw new ApplicationException($"Authentication failure: multiple identities: {nms}");
            }
            else
                lst = tmp;
        }

        PrepareRole(lst[0]);
        return lst[0];
    }

    public static Identity Assume(this Identity id0, string req)
    {
        if (string.IsNullOrEmpty(req))
            return id0;

        if (id0 == null)
            throw new ApplicationException($"Assume identity of {req.AsId()} denied for unknown identity");

        if (id0.P.AllAssumes != null && !id0.P.AllAssumes.Contains(req))
            throw new ApplicationException($"Assume identity of {req.AsId()} denied for identity {id0.Name.AsId()}");

        var res = Cfg.Get<ACL>().Identities.SingleOrDefault((id) => id.Name == req);
        if (res == null)
            throw new ApplicationException($"Assume identity of {req.AsId()} granted, but does not exist");

        PrepareRole(res);
        return res;
    }

    public static bool CanLogin(this Identity id0, string user)
        => id0.P.AllUsers == null || id0.P.AllUsers.Contains(user);

    public static void WillLogin(this Identity id0, string user)
    {
        if (!CanLogin(id0, user))
            throw new ApplicationException($"Login of {user} denied for identity {id0.Name.AsId()}");
    }

    public static bool CanInvoke(this Identity id0, string term)
        => id0 != null && Recursive((id) =>
                id.Grants.Any((p) => p.Action.HasFlag(Verb.Invoke) && term.StartsWith(p.Query ?? "", StringComparison.Ordinal))
                && !id.Denies.Any((p) => p.Action.Flags(Verb.Invoke) && term.StartsWith(p.Query ?? "", StringComparison.Ordinal)), id0)
            && !Recursive((id) => id.Rejects.Any((p) => p.Action.Flags(Verb.Invoke) && term.StartsWith(p.Query ?? "", StringComparison.Ordinal)), id0);

    public static void WillInvoke(this Identity id0, string term)
    {
        if (!CanInvoke(id0, term))
            throw new ApplicationException($"Access to \"{term}\" denied for identity {id0.Name.AsId()}");
    }

    public static bool CanRead(this Identity id, Voucher v)
        => v.IsMatch(id.P.Voucher.Query);

    public static bool CanWrite(this Identity id, Voucher v)
        => v == null || v.IsMatch(id.P.Voucher.Query) && v.Details.All((d) => d.IsMatch(id.P.Edit.Query));

    public static bool CanRead(this Identity id, VoucherDetail d)
        => d.IsMatch(id.P.View.Query);

    public static bool CanWrite(this Identity id, VoucherDetail d)
        => d.IsMatch(id.P.Edit.Query);

    public static void WillWrite(this Identity id, Voucher v, bool userInput = false)
    {
        if (v == null)
            return;

        if (v.Details.GroupBy(static (d) => d.Currency).Any(static (g) =>
                    Math.Abs(g.Sum(static (d) => d.Fund!.Value)) >= VoucherDetail.Tolerance) && !id.P.Imba)
            throw new ApplicationException($"Write to {v.ID.Quotation('^')} denied for identity {id.Name.AsId()}\n" +
                    "Reason: Debit/Credit imbalance");

        if (Math.Abs(v.Details.Where(static (d) => d.Title == 3998)
                    .Sum(static (d) => d.Fund!.Value)) >= VoucherDetail.Tolerance && !id.P.Imba)
            throw new ApplicationException($"Write to {v.ID.Quotation('^')} denied for identity {id.Name.AsId()}\n" +
                    "Reason: T3998 imbalance");

        if (!v.IsMatch(id.P.Voucher.Query))
            throw new ApplicationException($"Write to {v.ID.Quotation('^')} denied for identity {id.Name.AsId()}\n" +
                    (userInput ? "Reason: You are not authorized to make such a voucher"
                     : "Reason: You are not authorized to edit that voucher"));

        var noEdit = false;
        var noView = false;
        var lst = new List<VoucherDetail>();
        foreach (var d in v.Details)
        {
            if (d.IsMatch(id.P.Edit.Query))
                continue;

            noEdit = true;
            if (d.IsMatch(id.P.View.Query) || userInput)
                lst.Add(d);
            else
                noView = true;
        }
        if (!noEdit)
            return;

        var sb = new StringBuilder();
        sb.Append($"Write to {v.ID.Quotation('^')} denied for identity {id.Name.AsId()}\n");
        if (userInput)
            sb.Append("Reason: attempting to write to unauthorized accounts\n");
        else
            sb.Append("Reason: attempting to modify unauthorized accounts\n");
        if (lst.Count > 0)
        {
            sb.Append("Note: affected details are:\n");
            var ser = new ExprSerializer();
            foreach (var d in lst)
                sb.Append(ser.PresentVoucherDetail(d));
            if (noView)
                sb.Append("Note: this is not a complete list -- you are not authorized to view the rest.\n");
        } else if (noView)
            sb.Append("... and you are not even authorized to view these details!\n");

        throw new ApplicationException(sb.ToString());
    }

    public static bool CanAccess(this Identity id, Asset a)
        => a.IsMatch(id.P.Asset.Query);

    public static void WillAccess(this Identity id, Asset a)
    {
        if (a == null)
            return;

        if (!id.CanAccess(a))
            throw new ApplicationException($"Access to {a.ID} denied for identity {id.Name.AsId()}");
    }

    public static bool CanAccess(this Identity id, Amortization a)
        => a.IsMatch(id.P.Amort.Query);

    public static void WillAccess(this Identity id, Amortization a)
    {
        if (a == null)
            return;

        if (!id.CanAccess(a))
            throw new ApplicationException($"Access to {a.ID} denied for identity {id.Name.AsId()}");
    }

    private static IQueryCompounded<TAtom> I<TAtom>(bool redact, IQueryCompounded<TAtom> a, ACLQuery<TAtom> q)
        where TAtom : class
    {
        var b = redact ? q.Grant : q.Query;
        return b == null ? null : new IntersectQueries<TAtom>(a, b);
    }

    public static IQueryCompounded<IVoucherQueryAtom> Refine(this Identity id, IQueryCompounded<IVoucherQueryAtom> vq, bool synth = false)
        => I(!id.P.Reflect && synth, vq, id.P.Voucher);

    public static IQueryCompounded<IDistributedQueryAtom> RefineAsset(this Identity id, IQueryCompounded<IDistributedQueryAtom> aq, bool synth = false)
        => I(!id.P.Reflect && synth, aq, id.P.Asset);

    public static IQueryCompounded<IDistributedQueryAtom> RefineAmort(this Identity id, IQueryCompounded<IDistributedQueryAtom> aq, bool synth = false)
        => I(!id.P.Reflect && synth, aq, id.P.Amort);

    public static IVoucherDetailQuery Refine(this Identity id, IVoucherDetailQuery vdq, bool synth = false)
        => new VoucherDetailQuery(
            I(!id.P.Reflect && synth, vdq.VoucherQuery, id.P.Voucher),
            I(!id.P.Reflect && synth, vdq.ActualDetailFilter(), id.P.View));

    public static IGroupedQuery Refine(this Identity id, IGroupedQuery gq, bool synth = false)
        => new GroupedQuery(id.Refine(gq.VoucherEmitQuery, synth), gq.Subtotal);

    public static IVoucherGroupedQuery Refine(this Identity id, IVoucherGroupedQuery vgq, bool synth = false)
        => new VoucherGroupedQuery(id.Refine(vgq.VoucherQuery, synth), vgq.Subtotal);

    public static Voucher RedactDetails(this Identity id, Voucher v)
    {
        var lst = v.Details.Where((d) => d.IsMatch(id.P.View.Query)).ToList();
        if (lst.Count() == v.Details.Count())
            return v;

        return new()
            {
                ID = v.ID,
                Date = v.Date,
                Type = v.Type,
                Redacted = true,
                Details = lst,
            };
    }

    private static IEnumerable<Identity> GetKnowns(this Identity id)
    {
        var mat = Cfg.Get<ACL>().IdentityMatrix;
        if (id.P.AllKnowns == null)
        {
            foreach (var (k, ix) in mat)
            {
                PrepareRole(ix);
                yield return ix;
            }
        }
        else
        {
            foreach (var k in id.P.AllKnowns)
                if (mat.TryGetValue(k, out var ix))
                {
                    PrepareRole(ix);
                    yield return ix;
                }
        }
    }

    /// <summary>
    ///     If <c>v == null</c>, list all identities known to <c>id</c> that can R/W <c>d</c>;
    ///     If <c>v != null</c>, list all identities known to <c>id</c> that cannot R <c>d</c>;
    /// </summary>
    public static IEnumerable<(Identity, bool)> Audit(this Identity id, VoucherDetail d, Voucher v)
    {
        var mat = Cfg.Get<ACL>().IdentityMatrix;
        foreach (var ix in GetKnowns(id))
        {
            if (ix == id)
                continue;

            if (v == null)
            {
                if (ix.CanRead(d))
                    yield return (ix, ix.CanWrite(d) && (v == null || ix.CanWrite(v)));

                continue;
            }

            if (!ix.CanRead(v))
                continue;

            if (!ix.CanRead(d))
                yield return (ix, false);
        }
    }

    public static IEnumerable<(Identity, bool)> Audit(this Identity id, Voucher v)
    {
        var mat = Cfg.Get<ACL>().IdentityMatrix;
        foreach (var ix in GetKnowns(id))
            if (ix != id && ix.CanRead(v))
                yield return (ix, ix.CanWrite(v));
    }

    public static IEnumerable<Identity> Audit(this Identity id, Asset a)
    {
        var mat = Cfg.Get<ACL>().IdentityMatrix;
        foreach (var ix in GetKnowns(id))
            if (ix != id && ix.CanAccess(a))
                yield return ix;
    }

    public static IEnumerable<Identity> Audit(this Identity id, Amortization a)
    {
        var mat = Cfg.Get<ACL>().IdentityMatrix;
        foreach (var ix in GetKnowns(id))
            if (ix != id && ix.CanAccess(a))
                yield return ix;
    }
}
