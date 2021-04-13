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

namespace AccountingServer.Entities.Util
{
    public static class AccountantHelper
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

        /// <summary>
        ///     获取指定月的最后一天
        /// </summary>
        /// <param name="year">年</param>
        /// <param name="month">月</param>
        /// <returns>此月最后一天</returns>
        public static DateTime LastDayOfMonth(int year, int month)
        {
            while (month > 12)
            {
                month -= 12;
                year++;
            }

            while (month < 1)
            {
                month += 12;
                year--;
            }

            return new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddDays(-1);
        }
    }
}
