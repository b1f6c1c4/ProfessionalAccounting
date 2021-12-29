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
///     日期过滤器
/// </summary>
public class DateFilter
{
    /// <summary>
    ///     截止日期（含）
    /// </summary>
    public DateTime? EndDate;

    /// <summary>
    ///     是否允许无日期
    /// </summary>
    public bool Nullable;

    /// <summary>
    ///     是否只允许无日期（若为<c>true</c>，则无须考虑<c>Nullable</c>）
    /// </summary>
    public bool NullOnly;

    /// <summary>
    ///     开始日期（含）
    /// </summary>
    public DateTime? StartDate;

    public DateFilter(DateTime? startDate, DateTime? endDate)
    {
        NullOnly = false;
        Nullable = !startDate.HasValue;
        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>
    ///     任意日期
    /// </summary>
    public static DateFilter Unconstrained { get; } = new(null, null);

    /// <summary>
    ///     仅限无日期
    /// </summary>
    public static DateFilter TheNullOnly { get; } = new(null, null) { NullOnly = true };

    /// <summary>
    ///     非无日期
    /// </summary>
    public static DateFilter TheNotNull { get; } = new(null, null) { Nullable = false };

    /// <summary>
    ///     是否包含弱检索式
    /// </summary>
    /// <param name="loose">允许大范围</param>
    /// <returns>若包含则为<c>true</c>，否则为<c>false</c></returns>
    public bool IsDangerous(bool loose = false)
    {
        if (NullOnly)
            return true;
        if (Nullable)
            return true;
        if (!StartDate.HasValue)
            return true;
        if (!EndDate.HasValue)
            return true;
        if (loose)
            return false;
        if (EndDate.Value - StartDate.Value >= new TimeSpan(20 - 1, 0, 0, 0))
            return true;

        return false;
    }
}

/// <summary>
///     日期比较器
/// </summary>
public class DateComparer : IComparer<DateTime?>
{
    public int Compare(DateTime? x, DateTime? y) => DateHelper.CompareDate(x, y);
}

/// <summary>
///     日期辅助类
/// </summary>
public static class DateHelper
{
    /// <summary>
    ///     比较两日期（可以为无日期）的先后
    /// </summary>
    /// <param name="b1Date">第一个日期</param>
    /// <param name="b2Date">第二个日期</param>
    /// <returns>相等为0，第一个先为-1，第二个先为1（无日期按无穷长时间以前考虑）</returns>
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

    /// <summary>
    ///     判断日期是否符合日期过滤器
    /// </summary>
    /// <param name="dt">日期</param>
    /// <param name="rng">日期过滤器</param>
    /// <returns>是否符合</returns>
    public static bool Within(this DateTime? dt, DateFilter rng)
    {
        if (rng.NullOnly)
            return dt == null;

        if (!dt.HasValue)
            return rng.Nullable;

        if (dt < rng.StartDate)
            return false;

        return !(dt > rng.EndDate);
    }

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
