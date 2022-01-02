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
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AccountingServer.Entities;

namespace AccountingServer.Test;

public static class Util
{
    public static DateTime? ToDateTime(this string b1S)
        => b1S == null ? null : DateTimeParser.Parse(b1S);

    public static async ValueTask<string> Join(this IAsyncEnumerable<string> iae)
    {
        var sb = new StringBuilder();
        await foreach (var e in iae)
            sb.Append(e);
        return sb.ToString();
    }
}
