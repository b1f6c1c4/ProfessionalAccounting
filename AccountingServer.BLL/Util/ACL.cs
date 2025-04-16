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
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.BLL.Util;

[Serializable]
[XmlRoot("ACL")]
public class ACL
{
    [XmlElement("Identity")]
    public List<Identity> Identities { get; set; }
}

[Serializable]
public class Identity
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlElement("Inherit")]
    public List<string> Inherits { get; set; }

    [XmlElement("Login")]
    public Login Login { get; set; }

    [XmlElement("User")]
    public List<string> Users { get; set; }

    [XmlElement("Grant")]
    public List<Permission> Grants { get; set; }

    [XmlElement("Deny")]
    public List<Permission> Denies { get; set; }

    [XmlElement("Reject")]
    public List<Permission> Rejects { get; set; }

    public IQueryCompounded<IDetailQueryAtom> DetailQueryView;

    public IQueryCompounded<IDetailQueryAtom> DetailQueryViewRejects;

    public IQueryCompounded<IDetailQueryAtom> DetailQueryEdit;

    public IQueryCompounded<IDetailQueryAtom> DetailQueryEditRejects;
}

[Serializable]
public class Login : LoginEntry
{
    [XmlElement("OR")]
    public List<LoginEntry> ORs { get; set; }
}

[Serializable]
public enum Action
{
    [XmlEnum("view")]
    View,

    [XmlEnum("edit")]
    Edit,

    [XmlEnum("invoke")]
    Invoke,
}

[Serializable]
public class Permission
{
    [XmlAttribute("action")]
    public Action Action { get; set; }

    [XmlText]
    public string Query { get; set; }

    private IQueryCompounded<IDetailQueryAtom> m_DetailQuery;

    public IQueryCompounded<IDetailQueryAtom> DetailQuery
        => m_DetailQuery ?? (m_DetailQuery = ParsingF.PureDetailQuery(Query, new()));
}

public static class ACLManager
{
    static ACLManager()
        => Cfg.RegisterType<ACL>("ACL");

    public static Identity GetIdentity(string name)
    {
        var id = Cfg.Get<ACL>().Identities.SingleOrDefault((id) => id.Name == name)
            ?? Cfg.Get<ACL>().Identities.Single((id) => id.Name == null);

        if (id.DetailQueryView != null)
            return id;

        if (id.Inherits.Count == 0)
        {
            id.DetailQueryView = DetailQueryUnconstrained.Instance
                .QueryBut(id.Denies.Where(static (p) => p.Action == Action.View).Select(static (p) => p.DetailQuery))
                .QueryBut(id.Rejects.Where(static (p) => p.Action == Action.View).Select(static (p) => p.DetailQuery));
            id.DetailQueryViewRejects = id.Rejects.Where(static (p) => p.Action == Action.View)
                .Select(static (p) => p.DetailQuery).ToList().QueryAny();
            id.DetailQueryEdit = DetailQueryUnconstrained.Instance
                .QueryBut(id.Denies.Where(static (p) => p.Action == Action.Edit).Select(static (p) => p.DetailQuery))
                .QueryBut(id.Rejects.Where(static (p) => p.Action == Action.Edit).Select(static (p) => p.DetailQuery));
            id.DetailQueryEditRejects = id.Rejects.Where(static (p) => p.Action == Action.Edit)
                .Select(static (p) => p.DetailQuery).ToList().QueryAny();
        }
        else
        {
            var lst = id.Inherits.Select((nm) => GetIdentity(nm).DetailQueryViewRejects)
                .Where(static (q) => q != null).ToList();
            lst.AddRange(id.Rejects.Select(static (p) => p.DetailQuery));
            id.DetailQueryViewRejects = lst.QueryAny();
            id.DetailQueryView = id.Inherits.Select((nm) => GetIdentity(nm).DetailQueryView).ToList().QueryAny()
                .QueryAny(id.Grants.Where(static (p) => p.Action == Action.View).Select(static (g) => g.DetailQuery))
                .QueryBut(id.Denies.Where(static (p) => p.Action == Action.View).Select(static (d) => d.DetailQuery))
                .QueryBut(lst);
            lst = id.Inherits.Select((nm) => GetIdentity(nm).DetailQueryEditRejects)
                .Where(static (q) => q != null).ToList();
            lst.AddRange(id.Rejects.Select(static (p) => p.DetailQuery));
            id.DetailQueryEditRejects = lst.QueryAny();
            id.DetailQueryEdit = id.Inherits.Select((nm) => GetIdentity(nm).DetailQueryEdit).ToList().QueryAny()
                .QueryAny(id.Grants.Where(static (p) => p.Action == Action.Edit).Select(static (g) => g.DetailQuery))
                .QueryBut(id.Denies.Where(static (p) => p.Action == Action.Edit).Select(static (d) => d.DetailQuery))
                .QueryBut(lst);
        }

        return id;
    }

    private static bool Recursive(Predicate<Identity> pred, Identity id0)
        => id0 != null && (pred(id0) || id0.Inherits.Any((nm) => Recursive(pred,
                        Cfg.Get<ACL>().Identities.SingleOrDefault((id) => id.Name == nm))));

    public static bool IsMatch(Login login, Predicate<LoginEntry> pred)
    {
        if (!pred(login))
            return false;

        if (login.ORs != null && login.ORs.Count > 0)
            return login.ORs.Any((v) => pred(v));

        return true;
    }

    public static Identity Parse(Predicate<LoginEntry> pred)
    {
        var lst = Cfg.Get<ACL>().Identities.Where((id) => IsMatch(id.Login, pred)).ToList();
        if (lst.Count == 0)
            return GetIdentity("anonymous");

        if (lst.Count == 1)
            return lst[0];

        throw new ApplicationException($"Multiple identities matched: {string.Join(", ", lst)}");
    }

    public static bool CanLogin(this Identity id0, string user)
        => id0 != null && Recursive((id) => id.Users.Contains(user), id0);

    public static void Redact(this Identity id, Voucher v)
    {
        var lst = v.Details.Where((d) => d.IsMatch(id.DetailQueryView)).ToList();
        if (lst.Count == v.Details.Count)
            return;

        v.Redacted = true;
        v.Details = lst;
    }

    public static IVoucherDetailQuery Redact(this Identity id, IVoucherDetailQuery vdq)
        => new VoucherDetailQuery(vdq.VoucherQuery,
                new IntersectQueries<IDetailQueryAtom>(vdq.ActualDetailFilter(), id.DetailQueryView));
}
