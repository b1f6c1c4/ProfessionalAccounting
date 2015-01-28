using System;
using System.Collections.Generic;

namespace AccountingServer.Entities
{
    /// <summary>
    ///     摊销周期
    /// </summary>
    public enum AmortizeInterval
    {
        EveryDay,
        SameDayOfWeek,
        LastDayOfWeek,
        SameDayOfMonth,
        LastDayOfMonth,
        SameDayOfYear,
        LastDayOfYear,
    }

    /// <summary>
    ///     摊销计算表条目
    /// </summary>
    public class AmortItem
    {
        /// <summary>
        ///     规范忽略标志
        /// </summary>
        public const string IgnoranceMark = "reconciliation";

        /// <summary>
        ///     记账凭证编号
        /// </summary>
        public string VoucherID { get; set; }

        /// <summary>
        ///     记账日期
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        ///     摊销额
        /// </summary>
        public double Amount { get; set; }

        /// <summary>
        ///     待摊额
        ///     <para>不存储在数据库中</para>
        /// </summary>
        public double Residue { get; set; }

        /// <summary>
        ///     备注
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    ///     摊销
    /// </summary>
    public class Amortization
    {
        /// <summary>
        ///     规范忽略标志
        /// </summary>
        public const string IgnoranceMark = "reconciliation";

        /// <summary>
        ///     编号
        /// </summary>
        public Guid? ID { get; set; }

        /// <summary>
        ///     编号的标准存储格式
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public string StringID { get { return ID.ToString().ToUpperInvariant(); } set { ID = Guid.Parse(value); } }

        /// <summary>
        ///     名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     总额
        /// </summary>
        public double? Value { get; set; }

        /// <summary>
        ///     开始日期
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        ///     总日数
        /// </summary>
        public int? TotalDays { get; set; }

        /// <summary>
        ///     周期
        /// </summary>
        public AmortizeInterval? Interval { get; set; }

        /// <summary>
        ///     模板
        /// </summary>
        public Voucher Template { get; set; }

        /// <summary>
        ///     摊销计算表
        /// </summary>
        public IList<AmortItem> Schedule { get; set; }

        /// <summary>
        ///     备注
        /// </summary>
        public string Remark { get; set; }
    }
}
