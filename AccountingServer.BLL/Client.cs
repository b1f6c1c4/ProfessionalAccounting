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
using System.Globalization;

namespace AccountingServer.BLL;

public class Client
{
    public ClientUser ClientUser { get; set; }
    public ClientDateTime ClientDateTime { get; set; }
}

public interface IClientDependable
{
    Func<Client> Client { set; }
}

internal interface IDate
{
    DateTime? AsDate();
}

/// <summary>
///     客户端用户
/// </summary>
public class ClientUser
{
    public string Name { get; }

    internal ClientUser(string user) => Name = user ?? throw new InvalidOperationException("必须有一个用户");
}

/// <summary>
///     客户端时间
/// </summary>
public class ClientDateTime
{
    public DateTime Today { get; }

    internal ClientDateTime(DateTime timestamp) => Today = timestamp.Date;

    // ReSharper disable once UnusedMember.Global
    public static DateTime Parse(string str) => DateTime.Parse(
        str,
        null,
        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

    // ReSharper disable once UnusedMember.Global
    public static DateTime ParseExact(string str, string format) => DateTime.ParseExact(
        str,
        format,
        null,
        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

    // ReSharper disable once UnusedMember.Global
    public static bool TryParse(string str, out DateTime result) => DateTime.TryParse(
        str,
        null,
        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
        out result);

    // ReSharper disable once UnusedMember.Global
    public static bool TryParseExact(string str, string format, out DateTime result) => DateTime.TryParseExact(
        str,
        format,
        null,
        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
        out result);
}