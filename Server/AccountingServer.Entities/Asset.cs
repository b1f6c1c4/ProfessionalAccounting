using System;
using System.Collections.Generic;

namespace AccountingServer.Entities
{
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

        /// <summary>
        ///     减值额
        ///     <para>不存储在数据库中</para>
        /// </summary>
        public double Amount { get; set; }
    }

    /// <summary>
    ///     处置资产
    /// </summary>
    public class DispositionItem : AssetItem { }

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
        ///     编号的标准存储格式
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public string StringID { get { return ID.ToString().ToUpperInvariant(); } set { ID = Guid.Parse(value); } }

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
        ///     累计折旧所在科目编号
        /// </summary>
        public int? DepreciationTitle { get; set; }

        /// <summary>
        ///     减值准备所在科目编号
        /// </summary>
        public int? DevaluationTitle { get; set; }

        /// <summary>
        ///     折旧费用所在科目编号
        /// </summary>
        public int? DepreciationExpenseTitle { get; set; }

        /// <summary>
        ///     折旧费用所在二级科目编号
        /// </summary>
        public int? DepreciationExpenseSubTitle { get; set; }

        /// <summary>
        ///     减值损失所在科目编号
        /// </summary>
        public int? DevaluationExpenseTitle { get; set; }

        /// <summary>
        ///     减值损失所在二级科目编号
        /// </summary>
        public int? DevaluationExpenseSubTitle { get; set; }

        /// <summary>
        ///     折旧方法
        /// </summary>
        public DepreciationMethod? Method { get; set; }

        /// <summary>
        ///     资产折旧计算表
        /// </summary>
        public IList<AssetItem> Schedule { get; set; }

        /// <summary>
        ///     备注
        /// </summary>
        public string Remark { get; set; }
    }

    public class AssetItemComparer : IComparer<AssetItem>
    {
        public int Compare(AssetItem x, AssetItem y)
        {
            var res = DateHelper.CompareDate(x.Date, y.Date);
            if (res != 0)
                return res;

            Func<AssetItem, int> getType = t =>
                                           {
                                               if (t is AcquisationItem)
                                                   return 0;
                                               if (t is DispositionItem)
                                                   return 0; // Undistinguished
                                               if (t is DepreciateItem)
                                                   return 2;
                                               if (t is DevalueItem)
                                                   return 3;
                                               throw new InvalidOperationException();
                                           };
            return getType(x).CompareTo(getType(y));
        }
    }
}
