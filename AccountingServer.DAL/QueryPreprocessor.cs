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
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.DAL;

internal static class QueryPreprocessor
{
    public static SubtotalLevel Preprocess(this IVoucherGroupedQuery query)
    {
        if (query.Subtotal.GatherType != GatheringType.VoucherCount)
            throw new InvalidOperationException("记账凭证分类汇总只能计数");
        if (query.Subtotal.EquivalentDate.HasValue)
            throw new InvalidOperationException("记账凭证分类汇总不能等值");

        var level = query.Subtotal.Levels.Aggregate(SubtotalLevel.None, static (total, l) => total | l);
        if (query.Subtotal.AggrType != AggregationType.None)
            level |= query.Subtotal.AggrInterval;

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

        return level;
    }

    public static SubtotalLevel Preprocess(this IGroupedQuery query)
    {
        var level = query.Subtotal.Levels.Aggregate(SubtotalLevel.None, static (total, l) => total | l);
        if (query.Subtotal.AggrType != AggregationType.None)
            level |= query.Subtotal.AggrInterval;
        if (query.Subtotal.EquivalentDate.HasValue)
            level |= SubtotalLevel.Currency;

        return level;
    }

    public static bool ShouldAvoidZero(this IGroupedQuery query)
        => query.Subtotal.AggrType != AggregationType.ChangedDay &&
            query.Subtotal.Levels.LastOrDefault().HasFlag(SubtotalLevel.NonZero);
}
