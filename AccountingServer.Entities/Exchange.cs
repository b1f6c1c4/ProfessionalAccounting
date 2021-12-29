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

namespace AccountingServer.Entities;

/// <summary>
///     汇率
/// </summary>
[Serializable]
public class ExchangeRecord
{
    /// <summary>
    ///     日期
    /// </summary>
    public DateTime Time { get; set; }

    /// <summary>
    ///     购汇币种
    /// </summary>
    public string From { get; set; }

    /// <summary>
    ///     结汇币种
    /// </summary>
    public string To { get; set; }

    /// <summary>
    ///     汇率
    /// </summary>
    public double Value { get; set; }
}
