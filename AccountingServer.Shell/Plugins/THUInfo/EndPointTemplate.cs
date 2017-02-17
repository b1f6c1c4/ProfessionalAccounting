using System;
using System.Collections.Generic;
using System.Xml.Serialization;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace AccountingServer.Shell.Plugins.THUInfo
{
    [Serializable]
    public class EndPointTemplates
    {
        [XmlElement("Template")] public List<EndPointTemplate> Templates;
    }

    [Serializable]
    public class EndPointTemplate
    {
        [XmlElement("Detail")]
        public List<string> DetailString { get; set; }

        [XmlElement("Type")]
        public string Type { get; set; }

        [XmlArrayItem("Range")] public List<EndPointRange> Ranges;
    }

    [Serializable]
    public class EndPointRange
    {
        public int Start { get; set; }

        public int End { get; set; }
    }
}
