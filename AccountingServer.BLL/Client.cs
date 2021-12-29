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

namespace AccountingServer.BLL;

/// <summary>
///     客户端
/// </summary>
public class Client
{
    /// <summary>
    ///     客户端用户
    /// </summary>
    public string User { get; init; }

    /// <summary>
    ///     客户端时间
    /// </summary>
    public DateTime Today { get; init; }
}

public interface IClientDependable
{
    Client Client { set; }
}
