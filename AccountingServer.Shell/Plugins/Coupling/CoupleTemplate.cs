/* Copyright (C) 2020-2023 b1f6c1c4
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

namespace AccountingServer.Shell.Plugins.Coupling;

[Flags]
public enum CashCategory
{
    Cash = 0x1,
    ExtraCash = 0x2,
    NonCash = 0x3,
    IsCouple = 0x40000000,
}

public abstract class Cash
{
    [XmlElement("Cash")] public List<CashAccount> CashAccounts;

    [XmlElement("ExtraCash")] public List<CashAccount> ExtraCashAccounts;

    [XmlIgnore]
    private List<IQueryCompounded<IDetailQueryAtom>> m_CashQuery;

    private List<IQueryCompounded<IDetailQueryAtom>> CashQuery
    {
        get
        {
            m_CashQuery ??= CashAccounts.Select(sa
                => ParsingF.PureDetailQuery(sa.Query, new() { User = User, Today = DateTime.UtcNow.Date })).ToList();
            return m_CashQuery;
        }
    }

    [XmlIgnore]
    private List<IQueryCompounded<IDetailQueryAtom>> m_ExtraCashQuery;

    private List<IQueryCompounded<IDetailQueryAtom>> ExtraCashQuery
    {
        get
        {
            m_ExtraCashQuery ??= ExtraCashAccounts.Select(sa
                => ParsingF.PureDetailQuery(sa.Query, new() { User = User, Today = DateTime.UtcNow.Date })).ToList();
            return m_ExtraCashQuery;
        }
    }

    [XmlAttribute("user")]
    public string User { get; set; }

    public (CashCategory, string) Parse(VoucherDetail detail)
    {
        if (detail.User != User)
            throw new ApplicationException("Inapplicable user");

        var id = CashQuery.FindIndex(detail.IsMatch);
        if (id >= 0)
            return (CashCategory.Cash, CashAccount.NameOf(detail, CashAccounts[id]));

        id = ExtraCashQuery.FindIndex(detail.IsMatch);
        if (id >= 0)
            return (CashCategory.ExtraCash, CashAccount.NameOf(detail, ExtraCashAccounts[id]));

        return (CashCategory.NonCash, null);
    }
}

[Serializable]
[XmlRoot("Coupling")]
public class CoupleTemplate
{
    [XmlElement("Couple")]
    public List<ConfigCouple> Couples;
}

[Serializable]
public class ConfigCouple : Cash
{
    [XmlElement("Of")]
    public List<Liability> Liabilities;

    public new (CashCategory, string) Parse(VoucherDetail detail)
    {
        if (User != detail.User)
            return Liabilities.Single(l => l.User == detail.User).Parse(detail);

        var (type, name) = base.Parse(detail);
        return (type | CashCategory.IsCouple, name);
    }
}

[Serializable]
public class Liability : Cash { }

[Serializable]
public class CashAccount
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlAttribute("byContent")]
    public bool ByContent { get; set; }

    [XmlAttribute("byRemark")]
    public bool ByRemark { get; set; }

    [XmlText]
    public string Query { get; set; }

    public static string NameOf(VoucherDetail detail, CashAccount sa)
    {
        var a = detail.User.AsUser();
        if (sa == null)
            return a;

        if (sa.Name != null)
            a += $"-{sa.Name}";
        if (sa.ByContent && detail.Content != null)
            a += $"-{detail.Content}";
        if (sa.ByRemark && detail.Remark != null)
            a += $"-{detail.Remark}";
        return a;
    }
}
