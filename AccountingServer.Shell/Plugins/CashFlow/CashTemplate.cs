/* Copyright (C) 2020-2021 b1f6c1c4
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
using System.Xml.Serialization;

namespace AccountingServer.Shell.Plugins.CashFlow;

[Serializable]
[XmlRoot("Templates")]
public class CashTemplates
{
    [XmlElement("Account")] public List<CashAccount> Accounts;
}

[Serializable]
public class CashAccount
{
    [XmlArray("Items")]
    [XmlArrayItem("OnceItem", typeof(OnceItem))]
    [XmlArrayItem("OnceQueryItem", typeof(OnceQueryItem))]
    [XmlArrayItem("MonthlyItem", typeof(MonthlyItem))]
    [XmlArrayItem("SimpleCreditCard", typeof(SimpleCreditCard))]
    [XmlArrayItem("ComplexCreditCard", typeof(ComplexCreditCard))]
    public List<CashFlowItem> Items;

    [XmlAttribute("user")]
    public string User { get; set; }

    [XmlAttribute("currency")]
    public string Currency { get; set; }

    [XmlElement]
    public string QuickAsset { get; set; }

    [XmlElement]
    public string Reimburse { get; set; }
}

[Serializable]
public abstract class CashFlowItem { }

[Serializable]
public class OnceItem : CashFlowItem
{
    [XmlAttribute("date")]
    public DateTime Date { get; set; }

    [XmlAttribute("fund")]
    public double Fund { get; set; }
}

[Serializable]
public class OnceQueryItem : CashFlowItem
{
    [XmlAttribute("date")]
    public DateTime Date { get; set; }

    [XmlText]
    public string Query { get; set; }
}

[Serializable]
public class MonthlyItem : CashFlowItem
{
    [XmlAttribute("day")]
    public int Day { get; set; }

    [XmlAttribute("since")]
    public DateTime Since { get; set; }

    [XmlAttribute("till")]
    public DateTime Till { get; set; }

    [XmlAttribute("fund")]
    public double Fund { get; set; }
}

[Serializable]
public class SimpleCreditCard : CashFlowItem
{
    [XmlAttribute("bill")]
    public int BillDay { get; set; }

    [XmlAttribute("repay")]
    public int RepaymentDay { get; set; }

    [XmlText]
    public string Query { get; set; }

    [XmlAttribute("monthly")]
    public double MonthlyFee { get; set; }
}

[Serializable]
public class ComplexCreditCard : CashFlowItem
{
    [XmlAttribute("bill")]
    public int BillDay { get; set; }

    [XmlAttribute("repay")]
    public int RepaymentDay { get; set; }

    [XmlAttribute("utility")]
    public int MaximumUtility { get; set; } = -1;

    [XmlText]
    public string Query { get; set; }

    [XmlAttribute("monthly")]
    public double MonthlyFee { get; set; }
}