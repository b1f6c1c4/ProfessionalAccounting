using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AccountingServer.Plugins.THUInfo
{
    [Serializable]
    internal class EndPointTemplates
    {
        [XmlElement("Template")] public List<EndPointTemplate> Templates;
    }

    [Serializable]
    internal class EndPointTemplate
    {
        public string Name { get; set; }

        [XmlArrayItem("Range")] public List<EndPointRange> Ranges;
    }

    [Serializable]
    internal class EndPointRange
    {
        public int Start { get; set; }

        public int End { get; set; }
    }
}
