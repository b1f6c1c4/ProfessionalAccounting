using System;

namespace AccountingServer.Entities
{
    /// <summary>
    /// ����ƾ֤�����
    /// </summary>
    public enum VoucherType
    {
        /// <summary>
        /// ��ͨ����ƾ֤
        /// </summary>
        Ordinal,
        /// <summary>
        /// ��ĩ��ת����ƾ֤
        /// </summary>
        Carry,
        /// <summary>
        /// ̯������ƾ֤
        /// </summary>
        Amortization,
        /// <summary>
        /// �۾ɼ���ƾ֤
        /// </summary>
        Depreciation,
        /// <summary>
        /// ��ֵ����ƾ֤
        /// </summary>
        Devalue,
        /// <summary>
        /// ��Ƚ�ת����ƾ֤
        /// </summary>
        AnnualCarry,
        /// <summary>
        /// ��׼ȷ����ƾ֤
        /// </summary>
        Uncertain
    }
    
    /// <summary>
    /// ����ƾ֤
    /// </summary>
    public class Voucher
    {
        /// <summary>
        /// ���
        /// </summary>
        public string ID { get; set; }
        /// <summary>
        /// ���ڣ���Ϊ<c>null</c>��ʾ������
        /// </summary>
        public DateTime? Date { get; set; }
        /// <summary>
        /// ��ע
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// ϸĿ
        /// </summary>
        public VoucherDetail[] Details { get; set; }
        /// <summary>
        /// ���
        /// </summary>
        public VoucherType? Type { get; set; }
    }

    /// <summary>
    /// ϸĿ
    /// </summary>
    public class VoucherDetail
    {
        /// <summary>
        /// ��������ƾ֤���
        /// </summary>
        public string Item { get; set; }
        /// <summary>
        /// ��ƿ�Ŀһ����Ŀ����
        /// </summary>
        public int? Title { get; set; }
        /// <summary>
        /// ��ƿ�Ŀ������Ŀ���룬��Ϊ<c>null</c>��ʾ�޶�����Ŀ
        /// </summary>
        public int? SubTitle { get; set; }
        /// <summary>
        /// ����
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// ���
        /// </summary>
        public double? Fund { get; set; }
        /// <summary>
        /// ��ע
        /// </summary>
        public string Remark { get; set; }
    }

    //public interface IAssetItem
    //{
        
    //}

    //public struct Depreciate : IAssetItem
    //{
    //    public DateTime? Date { get; set; }
    //    public double? Fund { get; set; }
    //}
    //public struct Devalue : IAssetItem
    //{
    //    public DateTime? Date { get; set; }
    //    public double? Fund { get; set; }
    //}

    //public class DbAsset
    //{
    //    public Guid ID { get; set; }
    //    public string Name { get; set; }
    //    public DateTime? Date { get; set; }
    //    public double? Value { get; set; }
    //    public int? Life { get; set; }
    //    public double? Salvge { get; set; }
    //    public int? Title { get; set; }
    //    public IAssetItem[] Schedule { get; set; }
    //}

    /// <summary>
    /// ������Ŀ
    /// </summary>
    public class Balance
    {
        /// <summary>
        /// ����
        /// </summary>
        public DateTime? Date { get; set; }
        /// <summary>
        /// һ����Ŀ���
        /// </summary>
        public int? Title { get; set; }
        /// <summary>
        /// ������Ŀ���
        /// </summary>
        public int? SubTitle { get; set; }
        /// <summary>
        /// ����
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// ���
        /// </summary>
        public double Fund { get; set; }
    }
}
