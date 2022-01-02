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

namespace AccountingServer.Shell.Plugins.Interest;

/// <summary>
///     自动计算利息支出和还款
/// </summary>
internal class InterestExpense : InterestBase
{
    /// <inheritdoc />
    protected override string MajorFilter() => "T2202+T2203+T224100";

    /// <inheritdoc />
    protected override int Dir() => -1;

    /// <inheritdoc />
    protected override int MinorSubTitle() => 01;
}
