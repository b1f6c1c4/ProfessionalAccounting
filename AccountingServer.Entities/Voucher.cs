using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace AccountingServer.Entities
{
    /// <summary>
    ///     ����ƾ֤���
    /// </summary>
    [Serializable]
    public enum VoucherType
    {
        /// <summary>
        ///     ��ͨ����ƾ֤
        /// </summary>
        Ordinary,

        /// <summary>
        ///     �ǽ�ת����ƾ֤
        ///     <remarks>������ʱʹ��</remarks>
        /// </summary>
        General,

        /// <summary>
        ///     ��ĩ��ת����ƾ֤
        /// </summary>
        Carry,

        /// <summary>
        ///     ̯������ƾ֤
        /// </summary>
        Amortization,

        /// <summary>
        ///     �۾ɼ���ƾ֤
        /// </summary>
        Depreciation,

        /// <summary>
        ///     ��ֵ����ƾ֤
        /// </summary>
        Devalue,

        /// <summary>
        ///     ��ת����ƾ֤
        /// </summary>
        AnnualCarry,

        /// <summary>
        ///     ��׼ȷ����ƾ֤
        /// </summary>
        Uncertain
    }

    /// <summary>
    ///     ����ƾ֤
    /// </summary>
    [Serializable]
    public class Voucher
    {
        /// <summary>
        ///     ���˱�λ��
        /// </summary>
        public const string BaseCurrency = "CNY";

        /// <summary>
        ///     ���
        /// </summary>
        [XmlAttribute("id")]
        public string ID { get; set; }

        /// <summary>
        ///     ���ڣ���Ϊ<c>null</c>��ʾ������
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        ///     ��ע
        /// </summary>
        [XmlAttribute("remark")]
        public string Remark { get; set; }

        /// <summary>
        ///     ϸĿ
        /// </summary>
        [XmlElement("Detail")]
        public List<VoucherDetail> Details { get; set; }

        /// <summary>
        ///     ���
        /// </summary>
        [DefaultValue(VoucherType.Ordinary)]
        public VoucherType? Type { get; set; }

        /// <summary>
        ///     ����
        /// </summary>
        [XmlAttribute("currency")]
        public string Currency { get; set; }
    }

    /// <summary>
    ///     ϸĿ
    /// </summary>
    [Serializable]
    public class VoucherDetail
    {
        /// <summary>
        ///     ��ƿ�Ŀһ����Ŀ����
        /// </summary>
        public int? Title { get; set; }

        /// <summary>
        ///     ��ƿ�Ŀ������Ŀ���룬��Ϊ<c>null</c>��ʾ�޶�����Ŀ
        /// </summary>
        public int? SubTitle { get; set; }

        /// <summary>
        ///     ����
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        ///     ���
        /// </summary>
        public double? Fund { get; set; }

        /// <summary>
        ///     ��ע
        /// </summary>
        public string Remark { get; set; }
    }
}
