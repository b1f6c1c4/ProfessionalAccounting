using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using AccountingServer.Entities;

namespace AccountingServer.Plugins.Reimburse
{
    [Serializable]
    [XmlRoot("Templates")]
    public class ReimTemplates
    {
        /// <summary>
        ///     财月分隔
        /// </summary>
        [XmlAttribute("day")]
        public int Day { get; set; }

        [XmlElement("Template")] public List<ReimTemplate> Templates;
    }

    [Serializable]
    public class ReimTemplate : Voucher
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlText]
        public string Query { get; set; }

        [XmlAttribute("ratio")]
        public double Ratio { get; set; }

        [XmlAttribute("ext")]
        [DefaultValue(false)]
        public bool IsLeftExtended { get; set; }

        [XmlAttribute("byTitle")]
        [DefaultValue(false)]
        public bool ByTitle { get; set; }

        [XmlAttribute("bySubTitle")]
        [DefaultValue(false)]
        public bool BySubTitle { get; set; }

        [XmlAttribute("byContent")]
        [DefaultValue(false)]
        public bool ByCountent { get; set; }

        [XmlAttribute("hideContent")]
        [DefaultValue(false)]
        public bool HideCountent { get; set; }

        [XmlAttribute("byRemark")]
        [DefaultValue(false)]
        public bool ByRemark { get; set; }
    }
}
