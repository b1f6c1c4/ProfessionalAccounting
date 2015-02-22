using System;
using System.Collections.Generic;

namespace AccountingServer.Entities
{
    /// <summary>
    ///     记账凭证类别
    /// </summary>
    public enum VoucherType
    {
        /// <summary>
        ///     普通记账凭证
        /// </summary>
        Ordinal,

        /// <summary>
        ///     期末结转记账凭证
        /// </summary>
        Carry,

        /// <summary>
        ///     摊销记账凭证
        /// </summary>
        Amortization,

        /// <summary>
        ///     折旧记账凭证
        /// </summary>
        Depreciation,

        /// <summary>
        ///     减值记账凭证
        /// </summary>
        Devalue,

        /// <summary>
        ///     年度结转记账凭证
        /// </summary>
        AnnualCarry,

        /// <summary>
        ///     不准确记账凭证
        /// </summary>
        Uncertain
    }

    /// <summary>
    ///     记账凭证
    /// </summary>
    public class Voucher
    {
        /// <summary>
        ///     对账忽略标志
        /// </summary>
        public const string ReconciliationMark = "reconciliation";

        /// <summary>
        ///     编号
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        ///     日期，若为<c>null</c>表示无日期
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        ///     备注
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        ///     细目
        /// </summary>
        public IList<VoucherDetail> Details { get; set; }

        /// <summary>
        ///     类别
        /// </summary>
        public VoucherType? Type { get; set; }
    }

    /// <summary>
    ///     细目
    /// </summary>
    public class VoucherDetail
    {
        /// <summary>
        ///     会计科目一级科目代码
        /// </summary>
        public int? Title { get; set; }

        /// <summary>
        ///     会计科目二级科目代码，若为<c>null</c>表示无二级科目
        /// </summary>
        public int? SubTitle { get; set; }

        /// <summary>
        ///     内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        ///     金额
        /// </summary>
        public double? Fund { get; set; }

        /// <summary>
        ///     备注
        /// </summary>
        public string Remark { get; set; }
    }
}
