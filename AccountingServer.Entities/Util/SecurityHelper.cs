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

public static class SecurityHelper
{
    private class SecurityVisitor
        : IQueryVisitor<IVoucherQueryAtom, bool>,
            IQueryVisitor<IDetailQueryAtom, bool>,
            IQueryVisitor<IDistributedQueryAtom, bool>
    {
        public bool Visit(IVoucherQueryAtom query)
            => query.VoucherFilter.IsDangerous() && query.Range.IsDangerous() && query.DetailFilter.IsDangerous();

        public bool Visit(IQueryAry<IVoucherQueryAtom> query)
            => query.Operator switch
                {
                    OperatorType.None => query.Filter1.IsDangerous(),
                    OperatorType.Identity => query.Filter1.IsDangerous(),
                    OperatorType.Complement => true,
                    OperatorType.Union => query.Filter1.IsDangerous() || query.Filter2.IsDangerous(),
                    OperatorType.Subtract => query.Filter1.IsDangerous(),
                    OperatorType.Intersect => query.Filter1.IsDangerous() && query.Filter2.IsDangerous(),
                    _ => throw new ArgumentOutOfRangeException(),
                };

        public bool Visit(IDetailQueryAtom query)
            => query.Filter.IsDangerous()
                && string.IsNullOrEmpty(query.ContentPrefix)
                && string.IsNullOrEmpty(query.RemarkPrefix);

        public bool Visit(IQueryAry<IDetailQueryAtom> query)
            => query.Operator switch
                {
                    OperatorType.None => query.Filter1.IsDangerous(),
                    OperatorType.Identity => query.Filter1.IsDangerous(),
                    OperatorType.Complement => true,
                    OperatorType.Union => query.Filter1.IsDangerous() || query.Filter2.IsDangerous(),
                    OperatorType.Subtract => query.Filter1.IsDangerous(),
                    OperatorType.Intersect => query.Filter1.IsDangerous() && query.Filter2.IsDangerous(),
                    _ => throw new ArgumentOutOfRangeException(),
                };

        public bool Visit(IDistributedQueryAtom query)
            => query.Filter.IsDangerous() && query.Range.IsDangerous();

        public bool Visit(IQueryAry<IDistributedQueryAtom> query)
            => query.Operator switch
                {
                    OperatorType.None => query.Filter1.IsDangerous(),
                    OperatorType.Identity => query.Filter1.IsDangerous(),
                    OperatorType.Complement => true,
                    OperatorType.Union => query.Filter1.IsDangerous() || query.Filter2.IsDangerous(),
                    OperatorType.Subtract => query.Filter1.IsDangerous(),
                    OperatorType.Intersect => query.Filter1.IsDangerous() && query.Filter2.IsDangerous(),
                    _ => throw new ArgumentOutOfRangeException(),
                };
    }

    private static bool IsDangerous(this VoucherDetail filter)
        => !filter.Fund.HasValue && string.IsNullOrEmpty(filter.Content) && string.IsNullOrEmpty(filter.Remark);

    private static bool IsDangerous(this Voucher filter)
        => filter.ID == null && string.IsNullOrEmpty(filter.Remark);

    private static bool IsDangerous(this IDistributed filter)
        => !filter.ID.HasValue && string.IsNullOrEmpty(filter.Name) && string.IsNullOrEmpty(filter.Remark);

    public static bool IsDangerous(this IQueryCompounded<IVoucherQueryAtom> filter)
        => filter?.Accept<bool>(new SecurityVisitor()) != false;

    public static bool IsDangerous(this IQueryCompounded<IDetailQueryAtom> filter)
        => filter?.Accept<bool>(new SecurityVisitor()) != false;

    public static bool IsDangerous(this IQueryCompounded<IDistributedQueryAtom> filter)
        => filter?.Accept<bool>(new SecurityVisitor()) != false;

    public static bool IsDangerous(this IVoucherDetailQuery query)
        => query.VoucherQuery.IsDangerous() && query.ActualDetailFilter().IsDangerous();
}
