using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Serialization;
using AccountingServer.BLL;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Serializer
{
    internal class AbbrSerializer : ExprSerializer
    {
        private static readonly ConfigManager<Abbreviations> Abbrs;

        static AbbrSerializer() => Abbrs = new ConfigManager<Abbreviations>("Abbr.xml");

        protected override bool AlternativeTitle(ref string expr, ICollection<string> lst, ref ITitle title)
        {
            Abbreviation d = null;
            if (
                Parsing.Token(
                    ref expr,
                    false,
                    t => (d = Abbrs.Config.Abbrs.FirstOrDefault(a => a.Abbr == t)) != null) == null)
                return true;

            title = d;
            if (!d.Editable)
                lst.Add(d.Content);
            return false;
        }
    }

    [Serializable]
    [XmlRoot("Abbrs")]
    public class Abbreviations
    {
        [XmlElement("Abbr")] public List<Abbreviation> Abbrs;
    }

    [Serializable]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class Abbreviation : ITitle
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
    }
}
