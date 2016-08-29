using System;
using System.Collections.Generic;
using System.Xml.Serialization;

// ReSharper disable once CheckNamespace

namespace AccountingServer.Plugins
{
    [Serializable]
    [XmlRoot("Plugins")]
    public class PluginInfos
    {
        [XmlElement("Plugin")] public List<PluginInfo> Infos;
    }

    [Serializable]
    public class PluginInfo
    {
        [XmlAttribute("alias")]
        public string Alias { get; set; }

        [XmlAttribute("asm")] public string AssemblyName;

        [XmlText] public string ClassName;
    }
}
