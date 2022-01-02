/* Copyright (C) 2020-2022 b1f6c1c4
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

namespace AccountingServer.Entities.Util;

public static class NumericHelper
{
    /// <summary>
    ///     判断是否为零
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>是否为零</returns>
    public static bool IsZero(this double value) => Math.Abs(value) < VoucherDetail.Tolerance;

    /// <summary>
    ///     判断是否为非负
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>是否非负</returns>
    public static bool IsNonNegative(this double value) => value > -VoucherDetail.Tolerance;

    /// <summary>
    ///     判断是否为非正
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>是否非正</returns>
    public static bool IsNonPositive(this double value) => value < VoucherDetail.Tolerance;
}
