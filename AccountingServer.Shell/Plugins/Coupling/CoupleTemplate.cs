/* Copyright (C) 2020-2022 b1f6c1c4
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

public abstract class Cash
{
    [XmlAttribute("user")]
    public string User { get; set; }

    [XmlElement("Cash")] public List<CashAccount> CashAccounts;

    [XmlIgnore]
    private List<IQueryCompounded<IDetailQueryAtom>> m_CashQuery;

    public bool IsCash(VoucherDetail detail)
    {
        m_CashQuery ??= CashAccounts.Select(sa
            => ParsingF.PureDetailQuery(sa.Query, new() { User = User, Today = DateTime.UtcNow.Date })).ToList();
        return detail.User == User && m_CashQuery.Any(detail.IsMatch);
    }

    public bool IsNonCash(VoucherDetail detail)
    {
        m_CashQuery ??= CashAccounts.Select(sa
            => ParsingF.PureDetailQuery(sa.Query, new() { User = User, Today = DateTime.UtcNow.Date })).ToList();
        return detail.User == User && !m_CashQuery.Any(detail.IsMatch);
    }

    public string NameOf(VoucherDetail detail)
    {
        m_CashQuery ??= CashAccounts.Select(sa
            => ParsingF.PureDetailQuery(sa.Query, new() { User = User, Today = DateTime.UtcNow.Date })).ToList();
        var id = m_CashQuery.FindIndex(detail.IsMatch);
        return CashAccount.NameOf(detail, id < 0 ? null : CashAccounts[id]);
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
