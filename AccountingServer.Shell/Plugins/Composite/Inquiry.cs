using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace AccountingServer.Shell.Plugins.Composite
{
    public abstract class BaseInquiry
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        public abstract T Accept<T>(IInquiryVisitor<T> visitor);
    }

    public interface IInquiryVisitor<out T>
    {
        T Visit(InquiriesHub inq);
        T Visit(SimpleInquiry inq);
        T Visit(ComplexInquiry inq);
    }

    [Serializable]
    [XmlRoot("Templates")]
    public class CompositeTemplates
    {
        [XmlElement("Template")] public List<Template> Templates;
    }

    [Serializable]
    public class Template : InquiriesHub
    {
        /// <summary>
        ///     月分隔
        /// </summary>
        [XmlAttribute("day")]
        [DefaultValue(0)]
        public int Day { get; set; }
    }

    [Serializable]
    public class InquiriesHub : BaseInquiry
    {
        [XmlElement("InquiriesHub", typeof(InquiriesHub))]
        [XmlElement("Inquiry", typeof(SimpleInquiry))]
        [XmlElement("ComplexInquiry", typeof(ComplexInquiry))]
        public List<BaseInquiry> Inquiries;

        public override T Accept<T>(IInquiryVisitor<T> visitor) => visitor.Visit(this);
    }

    public abstract class Inquiry : BaseInquiry
    {
        [XmlAttribute("ext")]
        public bool IsLeftExtended { get; set; }

        [XmlAttribute("ratio")]
        public double Ratio { get; set; } = double.NaN;

        [XmlAttribute("general")]
        public bool General { get; set; } = true;

        [XmlAttribute("byCurrency")]
        public bool ByCurrency { get; set; } = true;

        [XmlAttribute("byTitle")]
        public bool ByTitle { get; set; }

        [XmlAttribute("bySubTitle")]
        public bool BySubTitle { get; set; }

        [XmlAttribute("byContent")]
        public bool ByContent { get; set; }

        [XmlAttribute("hideContent")]
        public bool HideContent { get; set; }

        [XmlAttribute("byRemark")]
        public bool ByRemark { get; set; }
    }

    [Serializable]
    public class SimpleInquiry : Inquiry
    {
        [XmlText]
        public string Query { get; set; }

        public override T Accept<T>(IInquiryVisitor<T> visitor) => visitor.Visit(this);
    }

    [Serializable]
    public class ComplexInquiry : Inquiry
    {
        [XmlElement("voucher")]
        public string VoucherQuery { get; set; }

        [XmlElement("emit")]
        public string Emit { get; set; }

        public override T Accept<T>(IInquiryVisitor<T> visitor) => visitor.Visit(this);
    }
}
