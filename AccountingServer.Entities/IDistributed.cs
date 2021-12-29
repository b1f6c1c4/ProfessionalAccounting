/* Copyright (C) 2020-2021 b1f6c1c4
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
///     分期
/// </summary>
public interface IDistributed
{
    /// <summary>
    ///     编号
    /// </summary>
    Guid? ID { get; }

    /// <summary>
    ///     用户
    /// </summary>
    string User { get; }

    /// <summary>
    ///     名称
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     入账日期
    /// </summary>
    DateTime? Date { get; }

    /// <summary>
    ///     全体
    /// </summary>
    double? Value { get; }

    /// <summary>
    ///     备注
    /// </summary>
    string Remark { get; }

    /// <summary>
    ///     计算表
    /// </summary>
    IEnumerable<IDistributedItem> TheSchedule { get; }
}

/// <summary>
///     分期计算表条目
/// </summary>
public interface IDistributedItem
{
    /// <summary>
    ///     记账凭证编号
    /// </summary>
    string VoucherID { get; }

    /// <summary>
    ///     记账日期
    /// </summary>
    DateTime? Date { get; }

    /// <summary>
    ///     剩余
    ///     <para>不存储在数据库中</para>
    /// </summary>
    double Value { get; }

    /// <summary>
    ///     备注
    /// </summary>
    string Remark { get; }
}
