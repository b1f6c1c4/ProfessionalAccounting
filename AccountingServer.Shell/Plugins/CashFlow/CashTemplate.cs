using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AccountingServer.Shell.Plugins.CashFlow
{
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
        [XmlArrayItem("CreditCard", typeof(CreditCard))]
        public List<CashFlowItem> Items;

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

        [XmlAttribute("fund")]
        public double Fund { get; set; }
    }

    [Serializable]
    public class CreditCard : CashFlowItem
    {
        [XmlAttribute("repay")]
        public int RepaymentDay { get; set; }

        [XmlText]
        public string Query { get; set; }

        [XmlAttribute("bill")]
        public int BillDay { get; set; }
    }
}
