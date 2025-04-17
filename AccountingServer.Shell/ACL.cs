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
using System.Xml.Serialization;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
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

    [XmlElement("Login")]
    public List<string> Users { get; set; }

    [XmlElement("Grant")]
    public List<Permission> Grants { get; set; }

    [XmlElement("Deny")]
    public List<Permission> Denies { get; set; }

    [XmlElement("Reject")]
    public List<Permission> Rejects { get; set; }

    [XmlIgnore]
    public bool Prepared { get; internal set; }

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
    public string Subject { get; set; }

    [XmlElement("CN")]
    public string CN { get; set; }

    [XmlElement("Issuer")]
    public string Issuer { get; set; }

    [XmlElement("Serial")]
    public string Serial { get; set; }

    [XmlElement("Fingerprint")]
    public string Fingerprint { get; set; }
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

    private static bool Recursive(Predicate<Identity> pred, Identity id0)
        => id0 != null && (pred(id0) || id0.Inherits.Any((nm) => Recursive(pred,
                        Cfg.Get<ACL>().Identities.SingleOrDefault((id) => id.Name == nm))));

    public static bool IsMatch(IdentityProvider login, Predicate<IdPEntry> pred)
    {
        if (login == null)
            return false;

        if (!pred(login))
            return false;

        if (login.ORs != null && login.ORs.Count > 0)
            return login.ORs.Any((v) => pred(v));

        return true;
    }

    public static Identity Parse(Predicate<IdPEntry> pred)
    {
        var lst = Cfg.Get<ACL>().Identities.Where((id) => IsMatch(id.IdP, pred)).ToList();
        if (lst.Count == 0)
            lst = new() { Cfg.Get<ACL>().Identities.Single((id) => id.Name == null) };

        if (lst.Count > 1)
            throw new ApplicationException($"Multiple identities matched: {string.Join(", ", lst)}");

        PrepareRole(lst[0]);
        return lst[0];
    }

    public static bool CanLogin(this Identity id0, string user)
        => id0 != null && Recursive((id) => id.Users.Contains("*") || id.Users.Contains(user), id0);

    public static void WillLogin(this Identity id0, string user)
    {
        if (!CanLogin(id0, user))
            throw new ApplicationException($"Login of {user} denied for identity \"{id0.Name}\"");
    }

    public static bool CanInvoke(this Identity id0, string term)
        => id0 != null && Recursive((id) =>
                id.Grants.Any((p) => p.Action.HasFlag(Verb.Invoke) && term.StartsWith(p.Query, StringComparison.Ordinal))
                && !id.Denies.Any((p) => p.Action.Flags(Verb.Invoke) && term.StartsWith(p.Query, StringComparison.Ordinal)), id0)
            && !Recursive((id) => id.Rejects.Any((p) => p.Action.Flags(Verb.Invoke) && term.StartsWith(p.Query, StringComparison.Ordinal)), id0);

    public static void WillInvoke(this Identity id0, string term)
    {
        if (!CanInvoke(id0, term))
            throw new ApplicationException($"Access to \"{term}\" denied for identity \"{id0.Name}\"");
    }

    public static bool CanRead(this Identity id, Voucher v)
        => v.IsMatch(id.Voucher.Item2);

    public static bool CanWrite(this Identity id, Voucher v)
        => v.IsMatch(id.Voucher.Item2) && v.Details.All((d) => d.IsMatch(id.Edit.Item2));

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
