/* Copyright (C) 2020-2024 b1f6c1c4
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

using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Http;

internal static class StreamHelper
{
    private static readonly byte[] CrLf = { (byte)'\r', (byte)'\n' };

    internal static byte[] GetBytes(this string str, Encoding encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return encoding.GetBytes(str);
    }

    internal static async ValueTask WriteLineAsync(this Stream stream, string str = null, Encoding encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var data = encoding.GetBytes(str ?? "");
        await stream.WriteAsync(data);
        await stream.WriteAsync(CrLf);
    }
}
