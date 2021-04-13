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

namespace AccountingServer.Entities.Util
{
    public static class SecurityHelper
    {
        /// <summary>
        ///     是否包含弱检索式
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <returns>若包含则为<c>true</c>，否则为<c>false</c></returns>
        public static bool IsDangerous(this VoucherDetail filter)
        {
            if (filter.Fund.HasValue)
                return false;
            if (!string.IsNullOrEmpty(filter.Content))
                return false;
            if (!string.IsNullOrEmpty(filter.Remark))
                return false;

            return true;
        }

        /// <summary>
        ///     是否包含弱检索式
        /// </summary>
        /// <param name="filter">记账凭证过滤器</param>
        /// <returns>若包含则为<c>true</c>，否则为<c>false</c></returns>
        public static bool IsDangerous(this Voucher filter)
        {
            if (filter.ID != null)
                return false;

            return true;
        }

        /// <summary>
        ///     是否包含弱检索式
        /// </summary>
        /// <param name="filter">分期过滤器</param>
        /// <returns>若包含则为<c>true</c>，否则为<c>false</c></returns>
        public static bool IsDangerous(this IDistributed filter)
        {
            if (filter.ID.HasValue)
                return false;
            if (filter.Name != null)
                return false;
            if (!string.IsNullOrEmpty(filter.Remark))
                return false;

            return true;
        }

        /// <summary>
        ///     是否包含弱检索式
        /// </summary>
        /// <param name="query">记账凭证细目映射检索式</param>
        /// <returns>若包含则为<c>true</c>，否则为<c>false</c></returns>
        public static bool IsDangerous(this IVoucherDetailQuery query)
            => query.VoucherQuery.IsDangerous() && (query.DetailEmitFilter?.DetailFilter?.IsDangerous() ?? true);
    }
}
