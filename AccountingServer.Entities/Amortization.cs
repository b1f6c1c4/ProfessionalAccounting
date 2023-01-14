/* Copyright (C) 2020-2023 b1f6c1c4
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

namespace AccountingServer.Entities;

/// <summary>
///     摊销周期
/// </summary>
public enum AmortizeInterval
{
    /// <summary>
    ///     每日
    /// </summary>
    EveryDay,

    /// <summary>
    ///     每周同一天
    /// </summary>
    SameDayOfWeek,

    /// <summary>
    ///     每周日
    /// </summary>
    LastDayOfWeek,

    /// <summary>
    ///     每月同一天
    /// </summary>
    SameDayOfMonth,

    /// <summary>
    ///     每月最后一天
    /// </summary>
    LastDayOfMonth,

    /// <summary>
    ///     每年同一天
    /// </summary>
    SameDayOfYear,

    /// <summary>
    ///     每年最后一天
    /// </summary>
    LastDayOfYear,
}

/// <summary>
///     摊销计算表条目
/// </summary>
public class AmortItem : IDistributedItem
{
    /// <summary>
    ///     忽略标志
    /// </summary>
    public const string IgnoranceMark = "reconciliation";

    /// <summary>
    ///     摊销额
    /// </summary>
    public double Amount { get; set; }

    /// <inheritdoc />
    public string VoucherID { get; set; }

    /// <inheritdoc />
    public DateTime? Date { get; set; }

    /// <inheritdoc />
    public double Value { get; set; }

    /// <inheritdoc />
    public string Remark { get; set; }
}

/// <summary>
///     摊销
/// </summary>
public class Amortization : IDistributed
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
    public List<AmortItem> Schedule { get; set; }

    /// <inheritdoc />
    public Guid? ID { get; set; }

    /// <inheritdoc />
    public string User { get; set; }

    /// <inheritdoc />
    public string Name { get; set; }

    /// <summary>
    ///     总额
    /// </summary>
    public double? Value { get; set; }

    /// <summary>
    ///     开始日期
    /// </summary>
    public DateTime? Date { get; set; }

    /// <inheritdoc />
    public string Remark { get; set; }

    public IEnumerable<IDistributedItem> TheSchedule => Schedule;
}
