using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace AccountingServer
{
    [Serializable]
    [XmlRoot("Abbrs")]
    public class Abbreviations
    {
        [XmlElement("Abbr")] public List<Abbreviation> Abbrs;
    }

    [Serializable]
    public class Abbreviation
    {
        [XmlAttribute("string")]
        public string Abbr { get; set; }

        [XmlAttribute("edit")]
        [DefaultValue(false)]
        public bool Editable { get; set; }

        public int Title { get; set; }

        [DefaultValue(null)]
        public int? SubTitle { get; set; }

        [DefaultValue(null)]
        public string Content { get; set; }

        [DefaultValue(null)]
        public string Remark { get; set; }
    }
}
