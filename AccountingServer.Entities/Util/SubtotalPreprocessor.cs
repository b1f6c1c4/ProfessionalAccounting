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
using System.Linq;

namespace AccountingServer.Entities.Util;

public static class SubtotalPreprocessor
{
    public static SubtotalLevel PreprocessVoucher(this ISubtotal query)
    {
        if (query.GatherType != GatheringType.VoucherCount)
            throw new InvalidOperationException("记账凭证分类汇总只能计数");
        if (query.EquivalentDate.HasValue)
            throw new InvalidOperationException("记账凭证分类汇总不能等值");

        var level = query.Levels.Aggregate(SubtotalLevel.None, static (total, l) => total | l);
        if (query.AggrType != AggregationType.None)
            level |= query.AggrInterval;

        if (level.HasFlag(SubtotalLevel.User))
            throw new InvalidOperationException("记账凭证不能按用户分类汇总");
        if (level.HasFlag(SubtotalLevel.Currency))
            throw new InvalidOperationException("记账凭证不能按币种分类汇总");
        if (level.HasFlag(SubtotalLevel.Title))
            throw new InvalidOperationException("记账凭证不能按一级科目分类汇总");
        if (level.HasFlag(SubtotalLevel.SubTitle))
            throw new InvalidOperationException("记账凭证不能按二级科目分类汇总");
        if (level.HasFlag(SubtotalLevel.Content))
            throw new InvalidOperationException("记账凭证不能按内容分类汇总");
        if (level.HasFlag(SubtotalLevel.Remark))
            throw new InvalidOperationException("记账凭证不能按备注分类汇总");
        if (level.HasFlag(SubtotalLevel.Value))
            throw new InvalidOperationException("记账凭证不能按金额分类汇总");

        return level;
    }

    public static SubtotalLevel PreprocessDetail(this ISubtotal query)
    {
        var level = query.Levels.Aggregate(SubtotalLevel.None, static (total, l) => total | l);
        if (query.AggrType != AggregationType.None)
            level |= query.AggrInterval;
        if (query.EquivalentDate.HasValue)
            level |= SubtotalLevel.Currency;

        return level;
    }

    public static bool ShouldAvoidZero(this ISubtotal query)
        => query.AggrType != AggregationType.ChangedDay &&
            query.Levels.LastOrDefault().HasFlag(SubtotalLevel.NonZero);

    public static DateTime? Project(this DateTime? dt, SubtotalLevel level)
    {
        if (!dt.HasValue)
            return null;
        if (!level.HasFlag(SubtotalLevel.Day))
            return null;
        if (!level.HasFlag(SubtotalLevel.Week))
            return dt;
        if (level.HasFlag(SubtotalLevel.Year))
            return new(dt!.Value.Year, 1, 1);
        if (level.HasFlag(SubtotalLevel.Month))
            return new(dt!.Value.Year, dt!.Value.Month, 1);
        // if (level.HasFlag(SubtotalLevel.Week))
        return dt.Value.DayOfWeek switch
            {
                DayOfWeek.Monday => dt.Value.AddDays(-0),
                DayOfWeek.Tuesday => dt.Value.AddDays(-1),
                DayOfWeek.Wednesday => dt.Value.AddDays(-2),
                DayOfWeek.Thursday => dt.Value.AddDays(-3),
                DayOfWeek.Friday => dt.Value.AddDays(-4),
                DayOfWeek.Saturday => dt.Value.AddDays(-5),
                DayOfWeek.Sunday => dt.Value.AddDays(-6),
                _ => throw new ArgumentOutOfRangeException(),
            };
    }
}
