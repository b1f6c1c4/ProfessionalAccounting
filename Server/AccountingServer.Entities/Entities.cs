using System;
using System.Collections.Generic;

namespace AccountingServer.Entities
{
    /// <summary>
    ///     记账凭证的类别
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
        public VoucherDetail[] Details { get; set; }

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
        ///     所属记账凭证编号
        /// </summary>
        public string Item { get; set; }

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

    /// <summary>
    ///     资产折旧计算表栏目
    /// </summary>
    public abstract class AssetItem
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
        ///     账面价值
        ///     <para>不存储在数据库中</para>
        /// </summary>
        public double BookValue { get; set; }

        /// <summary>
        ///     备注
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    ///     取得资产
    /// </summary>
    public class AcquisationItem : AssetItem
    {
        /// <summary>
        ///     原值
        /// </summary>
        public double OrigValue { get; set; }
    }

    /// <summary>
    ///     计提折旧方法
    /// </summary>
    public enum DepreciationMethod
    {
        /// <summary>
        ///     不计提折旧
        /// </summary>
        None,

        /// <summary>
        ///     年限平均法
        /// </summary>
        StraightLine,

        /// <summary>
        ///     年数总和法
        /// </summary>
        SumOfTheYear,

        /// <summary>
        ///     双倍余额递减法
        /// </summary>
        DoubleDeclineMethod
    }

    /// <summary>
    ///     计提资产折旧
    /// </summary>
    public class DepreciateItem : AssetItem
    {
        /// <summary>
        ///     折旧额
        /// </summary>
        public double Amount { get; set; }
    }

    /// <summary>
    ///     计提资产减值准备
    /// </summary>
    public class DevalueItem : AssetItem
    {
        /// <summary>
        ///     公允价值
        /// </summary>
        public double FairValue { get; set; }
    }

    /// <summary>
    ///     处置资产
    /// </summary>
    public class DispositionItem : AssetItem
    {
        /// <summary>
        ///     原账面净值
        /// </summary>
        public double NetValue { get; set; }
    }

    /// <summary>
    ///     资产
    /// </summary>
    public class Asset
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
        ///     名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     入账日期
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        ///     资产原值
        /// </summary>
        public double? Value { get; set; }

        /// <summary>
        ///     预计净残值
        /// </summary>
        public double? Salvge { get; set; }

        /// <summary>
        ///     使用寿命
        /// </summary>
        public int? Life { get; set; }

        /// <summary>
        ///     原值所在科目编号
        /// </summary>
        public int? Title { get; set; }

        /// <summary>
        ///     折旧所在科目编号
        /// </summary>
        public int? DepreciationTitle { get; set; }

        /// <summary>
        ///     减值准备所在科目编号
        /// </summary>
        public int? DevaluationTitle { get; set; }

        /// <summary>
        ///     费用所在科目编号
        /// </summary>
        public int? ExpenseTitle { get; set; }

        /// <summary>
        ///     费用所在二级科目编号
        /// </summary>
        public int? ExpenseSubTitle { get; set; }

        /// <summary>
        ///     折旧方法
        /// </summary>
        public DepreciationMethod? Method { get; set; }

        /// <summary>
        ///     资产折旧计算表
        /// </summary>
        public AssetItem[] Schedule { get; set; }

        /// <summary>
        ///     备注
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    ///     余额表项目
    /// </summary>
    public class Balance
    {
        /// <summary>
        ///     日期
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        ///     一级科目编号
        /// </summary>
        public int? Title { get; set; }

        /// <summary>
        ///     二级科目编号
        /// </summary>
        public int? SubTitle { get; set; }

        /// <summary>
        ///     内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        ///     余额
        /// </summary>
        public double Fund { get; set; }
    }

    public class AssetItemComparer : IComparer<AssetItem>
    {
        public int Compare(AssetItem x, AssetItem y)
        {
            var res = BalanceComparer.CompareDate(x.Date, y.Date);
            if (res != 0)
                return res;

            Func<AssetItem, int> getType = t =>
                                            {
                                                if (t is AcquisationItem)
                                                    return 0;
                                                if (t is DispositionItem)
                                                    return 1;
                                                if (t is DepreciateItem)
                                                    return 2;
                                                if (t is DevalueItem)
                                                    return 3;
                                                throw new InvalidOperationException();
                                            };
            return getType(x).CompareTo(getType(y));
        }
    }

    public class BalanceEqualityComparer : EqualityComparer<Balance>
    {
        public override bool Equals(Balance x, Balance y)
        {
            return x.Title == y.Title && x.SubTitle == y.SubTitle && x.Content == y.Content;
        }

        public override int GetHashCode(Balance obj)
        {
            var t = obj.Title ?? Int32.MinValue;
            var s = obj.SubTitle ?? Int32.MaxValue;
            var c = obj.Content == null ? Int32.MinValue : obj.Content.GetHashCode();
            return t ^ (s << 3) ^ c;
        }
    }

    public class BalanceComparer : Comparer<Balance>
    {
        /// <summary>
        ///     比较两日期（可以为无日期）的早晚
        /// </summary>
        /// <param name="b1Date">第一个日期</param>
        /// <param name="b2Date">第二个日期</param>
        /// <returns>相等为0，第一个早为-1，第二个早为1（无日期按无穷长时间以前考虑）</returns>
        public static int CompareDate(DateTime? b1Date, DateTime? b2Date)
        {
            if (b1Date.HasValue &&
                b2Date.HasValue)
                return b1Date.Value.CompareTo(b2Date.Value);
            if (b1Date.HasValue)
                return 1;
            if (b2Date.HasValue)
                return -1;
            return 0;
        }

        public override int Compare(Balance x, Balance y)
        {
            if (x != null &&
                y != null)
            {
                switch (CompareDate(x.Date, y.Date))
                {
                    case 1:
                        return 1;
                    case -1:
                        return -1;
                }

                if (x.Title.HasValue &&
                    y.Title.HasValue)
                {
                    if (x.Title < y.Title)
                        return -1;
                    if (x.Title > y.Title)
                        return 1;
                }
                else if (x.Title.HasValue)
                    return 1;
                else if (y.Title.HasValue)
                    return -1;

                if (x.SubTitle.HasValue &&
                    y.SubTitle.HasValue)
                {
                    if (x.SubTitle < y.SubTitle)
                        return -1;
                    if (x.SubTitle > y.SubTitle)
                        return 1;
                }
                else if (x.SubTitle.HasValue)
                    return 1;
                else if (y.SubTitle.HasValue)
                    return -1;

                if (x.Content != null &&
                    y.Content != null)
                    return String.Compare(x.Content, y.Content, StringComparison.Ordinal);
                if (x.Content != null)
                    return 1;
                if (y.Content != null)
                    return -1;
            }
            if (x != null)
                return -1;
            if (y != null)
                return 1;
            return 0;
        }
    }
}
