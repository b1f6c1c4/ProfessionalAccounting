/* Copyright (C) 2020-2025 b1f6c1c4
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

using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace AccountingServer.DAL;

internal static class AsyncUtil
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IAsyncCursor<T> cursor)
    {
        while (await cursor.MoveNextAsync())
            foreach (var c in cursor.Current)
                yield return c;
    }

    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this Task<IAsyncCursor<T>> cursor)
    {
        var cur = await cursor;
        while (await cur.MoveNextAsync())
            foreach (var c in cur.Current)
                yield return c;
    }

    public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IAsyncCursorSource<T> cursorSource)
        => cursorSource.ToCursorAsync().ToAsyncEnumerable();
}
