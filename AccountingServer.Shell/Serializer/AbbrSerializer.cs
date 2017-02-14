using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Serialization;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Serializer
{
    internal class AbbrSerializer : ExprSerializer
    {
        private static readonly ConfigManager<Abbreviations> Abbrs;

        static AbbrSerializer() { Abbrs = new ConfigManager<Abbreviations>("Abbr.xml"); }

        protected override VoucherDetail GetVoucherDetail(ref string expr)
        {
            var lst = new List<string>();

            Parsing.TrimStartComment(ref expr);
            var title = Parsing.Title(ref expr);
            if (title == null)
            {
                Abbreviation d = null;
                if (
                    Parsing.Token(
                        ref expr,
                        false,
                        t => (d = Abbrs.Config.Abbrs.FirstOrDefault(a => a.Abbr == t)) != null) == null)
                    return null;

                title = d;
                if (!d.Editable)
                    lst.Add(d.Content);
            }

            double? fund;

            while (true)
            {
                Parsing.TrimStartComment(ref expr);
                if ((fund = Parsing.Double(ref expr)) != null)
                    break;

                Parsing.TrimStartComment(ref expr);
                if (Parsing.Optional(ref expr, "null"))
                    break;

                if (lst.Count > 2)
                    throw new ArgumentException("语法错误", nameof(expr));

                Parsing.TrimStartComment(ref expr);
                lst.Add(Parsing.Token(ref expr));
            }

            var content = lst.Count >= 1 ? lst[0] : null;
            var remark = lst.Count >= 2 ? lst[1] : null;

            return new VoucherDetail
                {
                    Title = title.Title,
                    SubTitle = title.SubTitle,
                    Content = string.IsNullOrEmpty(content) ? null : content,
                    Fund = fund,
                    Remark = string.IsNullOrEmpty(remark) ? null : remark
                };
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
