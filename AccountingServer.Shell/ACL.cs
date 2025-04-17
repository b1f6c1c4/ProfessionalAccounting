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

    [XmlElement("Grant")]
    public List<Permission> Grants { get; set; }

    [XmlElement("Deny")]
    public List<Permission> Denies { get; set; }

    [XmlElement("Reject")]
    public List<Permission> Rejects { get; set; }

    [XmlIgnore]
    public bool Prepared { get; internal set; }

    [XmlIgnore]
    public bool Imba { get; internal set; }

    [XmlIgnore]
    public HashSet<Role> AllRoles { get; internal set; }

    [XmlIgnore]
    public HashSet<string> AllUsers { get; internal set; }

    [XmlIgnore]
    public HashSet<string> AllAssumes { get; internal set; }

    [XmlIgnore]
    public (IQueryCompounded<IDetailQueryAtom>,IQueryCompounded<IDetailQueryAtom>) View { get; internal set; }

    [XmlIgnore]
    public (IQueryCompounded<IDetailQueryAtom>,IQueryCompounded<IDetailQueryAtom>) Edit { get; internal set; }

    [XmlIgnore]
    public (IQueryCompounded<IVoucherQueryAtom>,IQueryCompounded<IVoucherQueryAtom>) Voucher { get; internal set; }
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

    public static Role GetRole(string name)
    {
        var id = Cfg.Get<ACL>().Roles.Single((id) => id.Name == name);
        PrepareRole(id);
        return id;
    }

    private static bool Flags(this Verb a, Verb b)
        => (a & b) != Verb.None;

    public static void PrepareRole(Role id)
    {
        if (id.Prepared)
            return;

        id.AllRoles = new();
        id.AllUsers = new();
        id.AllAssumes = new();

        void Process(Role r)
        {
            if (!id.AllRoles.Add(r))
                return;

            foreach (var u in r.Users)
                id.AllUsers.Add(u);
            foreach (var a in r.Assumes)
                id.AllAssumes.Add(a);
            foreach (var nm in r.Inherits)
                Process(GetRole(nm));
        }
        Process(id);
        id.AllRoles.Remove(id);
        if (id.AllUsers.Contains("*"))
            id.AllUsers = null;
        if (id.AllAssumes.Contains("*"))
            id.AllAssumes = null;

        id.Imba = Recursive((id) =>
                id.Grants.Any((p) => p.Action.HasFlag(Verb.Imbalance))
                && !id.Denies.Any((p) => p.Action.Flags(Verb.Imbalance)), id)
            && !Recursive((id) => id.Rejects.Any((p) => p.Action.Flags(Verb.Imbalance)), id);

        (IQueryCompounded<T>,IQueryCompounded<T>) Compute<T>(Verb f,
                Func<Permission, IQueryCompounded<T>> proj,
                Func<Role, (IQueryCompounded<T>,IQueryCompounded<T>)> proj2)
            where T : class
        {
            if (id.Inherits.Count == 0)
            {
                return (id.Rejects.Where((p) => p.Action.Flags(f)).Select(proj).QueryAny(),
                        id.Grants.Where((p) => p.Action.HasFlag(f)).Select(proj).QueryAny()
                        .QueryBut(id.Denies.Where((p) => p.Action.Flags(f)).Select(proj))
                        .QueryBut(id.Rejects.Where((p) => p.Action.Flags(f)).Select(proj)));
            }
            else
            {
                var lst = id.Inherits.Select((nm) => proj2(GetRole(nm)).Item1)
                    .Concat(id.Rejects.Where((p) => p.Action.Flags(f)).Select(proj)).ToList();
                return (lst.QueryAny(),
                        id.Inherits.Select((nm) => proj2(GetRole(nm)).Item2).QueryAny()
                        .QueryAny(id.Grants.Where((p) => p.Action.HasFlag(f)).Select(proj))
                        .QueryBut(id.Denies.Where((p) => p.Action.Flags(f)).Select(proj))
                        .QueryBut(lst));
            }
        }

        var dq = static (Permission p) => ParsingF.PureDetailQuery(p.Query, new());
        var vq = static (Permission p) => ParsingF.VoucherQuery(p.Query, new());
        id.View = Compute(Verb.View, dq, (r) => r.View);
        id.Edit = Compute(Verb.Edit, dq, (r) => r.Edit);
        id.Voucher = Compute(Verb.Voucher, vq, (r) => r.Voucher);
        id.Prepared = true;
        Console.WriteLine($"Prepared identity {id.Name}");
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
        var lst = Cfg.Get<ACL>().Identities.Where((id) => IsMatch(id.IdP, idp)).ToList();
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
        if (req == null)
            return id0;

        if (id0 == null)
            throw new ApplicationException($"Assume identity of {req.Quotation('"')} denied for unknown identity");

        var nm = id0.Name.Quotation('"');
        if (id0.AllAssumes != null && !id0.AllAssumes.Contains(req))
            throw new ApplicationException($"Assume identity of {req.Quotation('"')} denied for identity {nm}");

        var res = Cfg.Get<ACL>().Identities.SingleOrDefault((id) => id.Name == req);
        if (res == null)
            throw new ApplicationException($"Assume identity of {req.Quotation('"')} granted, but does not exist");

        PrepareRole(res);
        return res;
    }

    public static bool CanLogin(this Identity id0, string user)
        => id0.AllUsers == null || id0.AllUsers.Contains(user);

    public static void WillLogin(this Identity id0, string user)
    {
        if (!CanLogin(id0, user))
            throw new ApplicationException($"Login of {user} denied for identity {id0.Name.Quotation('"')}");
    }

    public static bool CanInvoke(this Identity id0, string term)
        => id0 != null && Recursive((id) =>
                id.Grants.Any((p) => p.Action.HasFlag(Verb.Invoke) && term.StartsWith(p.Query ?? "", StringComparison.Ordinal))
                && !id.Denies.Any((p) => p.Action.Flags(Verb.Invoke) && term.StartsWith(p.Query ?? "", StringComparison.Ordinal)), id0)
            && !Recursive((id) => id.Rejects.Any((p) => p.Action.Flags(Verb.Invoke) && term.StartsWith(p.Query ?? "", StringComparison.Ordinal)), id0);

    public static void WillInvoke(this Identity id0, string term)
    {
        if (!CanInvoke(id0, term))
            throw new ApplicationException($"Access to \"{term}\" denied for identity {id0.Name.Quotation('"')}");
    }

    public static bool CanRead(this Identity id, Voucher v)
        => v.IsMatch(id.Voucher.Item2);

    public static bool CanWrite(this Identity id, Voucher v)
        => v == null || v.IsMatch(id.Voucher.Item2) && v.Details.All((d) => d.IsMatch(id.Edit.Item2));

    public static void WillWrite(this Identity id, Voucher v, bool userInput = false)
    {
        if (v == null)
            return;

        var nm = id.Name.Quotation('"');

        if (v.Details.GroupBy(static (d) => d.Currency).Any(static (g) =>
                    Math.Abs(g.Sum(static (d) => d.Fund!.Value)) >= VoucherDetail.Tolerance) && !id.Imba)
            throw new ApplicationException($"Write to {v.ID.Quotation('^')} denied for identity {nm}\n" +
                    "Reason: Debit/Credit imbalance");

        if (Math.Abs(v.Details.Where(static (d) => d.Title == 3998)
                    .Sum(static (d) => d.Fund!.Value)) >= VoucherDetail.Tolerance && !id.Imba)
            throw new ApplicationException($"Write to {v.ID.Quotation('^')} denied for identity {nm}\n" +
                    "Reason: T3998 imbalance");

        if (!v.IsMatch(id.Voucher.Item2))
            throw new ApplicationException($"Write to {v.ID.Quotation('^')} denied for identity {nm}\n" +
                    "Reason: You are not authorized to do anything on such a voucher");

        var noEdit = false;
        var noView = false;
        var lst = new List<VoucherDetail>();
        foreach (var d in v.Details)
        {
            if (d.IsMatch(id.Edit.Item2))
                continue;

            noEdit = true;
            if (d.IsMatch(id.View.Item2) || userInput)
                lst.Add(d);
            else
                noView = true;
        }
        if (!noEdit)
            return;

        var sb = new StringBuilder();
        sb.Append($"Write to {v.ID.Quotation('^')} denied for identity {nm}\n");
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

    public static IQueryCompounded<IVoucherQueryAtom> Refine(this Identity id, IQueryCompounded<IVoucherQueryAtom> vq)
        => id.Voucher.Item2 == null ? null : new IntersectQueries<IVoucherQueryAtom>(vq, id.Voucher.Item2);

    public static IVoucherDetailQuery Refine(this Identity id, IVoucherDetailQuery vdq)
        => id.Voucher.Item2 == null || id.View.Item2 == null ? null :new VoucherDetailQuery(
                new IntersectQueries<IVoucherQueryAtom>(vdq.VoucherQuery, id.Voucher.Item2),
                new IntersectQueries<IDetailQueryAtom>(vdq.ActualDetailFilter(), id.View.Item2));

    public static IGroupedQuery Refine(this Identity id, IGroupedQuery gq)
        => new GroupedQuery(id.Refine(gq.VoucherEmitQuery), gq.Subtotal);

    public static IVoucherGroupedQuery Refine(this Identity id, IVoucherGroupedQuery vgq)
        => new VoucherGroupedQuery(id.Refine(vgq.VoucherQuery), vgq.Subtotal);

    public static Voucher RedactDetails(this Identity id, Voucher v)
    {
        var lst = v.Details.Where((d) => d.IsMatch(id.View.Item2)).ToList();
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
}
