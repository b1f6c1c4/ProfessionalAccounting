using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AccountingServer.Shell.Plugins.CashFlow
{
    [Serializable]
    [XmlRoot("Templates")]
    public class CashTemplates
    {
        [XmlElement]
        public string QuickAsset { get; set; }

        [XmlElement]
        public bool Reimburse { get; set; }

        [XmlArray("Debts")] [XmlArrayItem("SimpleDebt", typeof(SimpleDebt))]
        [XmlArrayItem("CreditCard", typeof(CreditCard))] public List<Debt> Debts;
    }

    [Serializable]
    public abstract class Debt { }

    [Serializable]
    public class SimpleDebt : Debt
    {
        [XmlAttribute("day")]
        public DateTime Day { get; set; }

        [XmlText]
        public string Query { get; set; }
    }

    [Serializable]
    public class CreditCard : Debt
    {
        [XmlAttribute("repay")]
        public int RepaymentDay { get; set; }

        [XmlText]
        public string Query { get; set; }

        [XmlAttribute("bill")]
        public int BillDay { get; set; }
    }
}
