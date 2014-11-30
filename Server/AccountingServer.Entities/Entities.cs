using System;

namespace AccountingServer.Entities
{
    /// <summary>
    /// 记账凭证的类别
    /// </summary>
    public enum VoucherType
    {
        /// <summary>
        /// 普通记账凭证
        /// </summary>
        Ordinal,
        /// <summary>
        /// 期末结转记账凭证
        /// </summary>
        Carry,
        /// <summary>
        /// 摊销记账凭证
        /// </summary>
        Amortization,
        /// <summary>
        /// 折旧记账凭证
        /// </summary>
        Depreciation,
        /// <summary>
        /// 减值记账凭证
        /// </summary>
        Devalue,
        /// <summary>
        /// 年度结转记账凭证
        /// </summary>
        AnnualCarry,
        /// <summary>
        /// 不准确记账凭证
        /// </summary>
        Uncertain
    }
    
    /// <summary>
    /// 记账凭证
    /// </summary>
    public class Voucher
    {
        /// <summary>
        /// 编号
        /// </summary>
        public string ID { get; set; }
        /// <summary>
        /// 日期，若为<c>null</c>表示无日期
        /// </summary>
        public DateTime? Date { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 细目
        /// </summary>
        public VoucherDetail[] Details { get; set; }
        /// <summary>
        /// 类别
        /// </summary>
        public VoucherType? Type { get; set; }
    }

    /// <summary>
    /// 细目
    /// </summary>
    public class VoucherDetail
    {
        /// <summary>
        /// 所属记账凭证编号
        /// </summary>
        public string Item { get; set; }
        /// <summary>
        /// 会计科目一级科目代码
        /// </summary>
        public int? Title { get; set; }
        /// <summary>
        /// 会计科目二级科目代码，若为<c>null</c>表示无二级科目
        /// </summary>
        public int? SubTitle { get; set; }
        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 金额
        /// </summary>
        public double? Fund { get; set; }
        /// <summary>
        /// 备注
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
    /// 余额表项目
    /// </summary>
    public class Balance
    {
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime? Date { get; set; }
        /// <summary>
        /// 一级科目编号
        /// </summary>
        public int? Title { get; set; }
        /// <summary>
        /// 二级科目编号
        /// </summary>
        public int? SubTitle { get; set; }
        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 余额
        /// </summary>
        public double Fund { get; set; }
    }
}
