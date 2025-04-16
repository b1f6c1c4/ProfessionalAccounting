/* Copyright (C) 2020-2025 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AccountingServer.Entities;

/// <summary>
///     资产折旧计算表条目
/// </summary>
public abstract class AssetItem : IDistributedItem
{
    /// <summary>
    ///     忽略标志
    /// </summary>
    public const string IgnoranceMark = "reconciliation";

    /// <inheritdoc />
    public string VoucherID { get; set; }

    /// <inheritdoc />
    public DateTime? Date { get; set; }

    /// <summary>
    ///     账面价值
    ///     <para>不存储在数据库中</para>
    /// </summary>
    public double Value { get; set; }

    /// <inheritdoc />
    public string Remark { get; set; }
}

/// <summary>
///     取得资产
/// </summary>
public class AcquisitionItem : AssetItem
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
    DoubleDeclineMethod,
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
public class Asset : IDistributed
{
    /// <summary>
    ///     忽略标志
    /// </summary>
    public const string IgnoranceMark = "reconciliation";

    /// <summary>
    ///     编号的标准存储格式
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public string StringID { get => ID.ToString().ToUpperInvariant(); set => ID = Guid.Parse(value); }

    /// <summary>
    ///     币种
    /// </summary>
    public string Currency { get; set; }

    /// <summary>
    ///     预计净残值
    /// </summary>
    public double? Salvage { get; set; }

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
    public List<AssetItem> Schedule { get; set; }

    /// <inheritdoc />
    public Guid? ID { get; set; }

    /// <inheritdoc />
    public string User { get; set; }

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public DateTime? Date { get; set; }

    /// <summary>
    ///     资产原值
    /// </summary>
    public double? Value { get; set; }

    /// <inheritdoc />
    public string Remark { get; set; }

    public IEnumerable<IDistributedItem> TheSchedule => Schedule;
}

/// <summary>
///     资产折旧计算表条目比较器
/// </summary>
public class AssetItemComparer : IComparer<AssetItem>
{
    /// <inheritdoc />
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public int Compare(AssetItem x, AssetItem y)
    {
        var res = DateHelper.CompareDate(x.Date, y.Date);
        if (res != 0)
            return res;

        static int GetTy(AssetItem t)
            => t switch
                {
                    DepreciateItem => 1,
                    DevalueItem => 2,
                    // 不对资产的取得和处置加以区分
                    AcquisitionItem => 3,
                    DispositionItem => 3,
                    _ => throw new ArgumentException("计算表条目类型未知"),
                };

        return GetTy(x).CompareTo(GetTy(y));
    }
}
