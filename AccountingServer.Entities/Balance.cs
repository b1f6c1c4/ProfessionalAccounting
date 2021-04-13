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

namespace AccountingServer.Entities
{
    /// <summary>
    ///     余额表条目
    /// </summary>
    public class Balance
    {
        /// <summary>
        ///     日期
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        ///     一级科目编号
        /// </summary>
        public int? Title { get; set; }

        /// <summary>
        ///     二级科目编号
        /// </summary>
        public int? SubTitle { get; set; }

        /// <summary>
        ///     内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        ///     备注
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        ///     币种
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        ///     用户
        /// </summary>
        public string User { get; set; }

        /// <summary>
        ///     余额
        /// </summary>
        public double Fund { get; set; }
    }
}
