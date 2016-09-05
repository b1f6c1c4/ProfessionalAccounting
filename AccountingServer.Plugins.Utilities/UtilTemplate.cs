using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using AccountingServer.Entities;

// ReSharper disable once CheckNamespace

namespace AccountingServer.Plugins
{
    [Serializable]
    [XmlRoot("Templates")]
    public class UtilTemplates
    {
        [XmlElement("Template")] public List<UtilTemplate> Templates;
    }

    [Serializable]
    public enum UtilTemplateType
    {
        [XmlEnum(Name = "fixed")] Fixed,
        [XmlEnum(Name = "value")] Value,
        [XmlEnum(Name = "fill")] Fill
    }

    [Serializable]
    public class UtilTemplate : Voucher
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("desc")]
        public string Description { get; set; }

        [XmlAttribute("type")]
        public UtilTemplateType TemplateType { get; set; }

        [XmlAttribute("query")]
        public string Query { get; set; }

        [XmlAttribute("default")]
        public string DefaultString
        {
            get { return Default?.ToString("R"); }
            set { Default = Convert.ToDouble(value); }
        }

        [XmlIgnore]
        public double? Default { get; set; }
    }
}
