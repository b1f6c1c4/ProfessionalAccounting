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

namespace AccountingServer.BLL;

/// <summary>
///     会计科目
/// </summary>
public interface ITitle
{
    /// <summary>
    ///     会计科目一级科目代码
    /// </summary>
    int? Title { get; }

    /// <summary>
    ///     会计科目二级科目代码，若为<c>null</c>表示无二级科目
    /// </summary>
    int? SubTitle { get; }
}
