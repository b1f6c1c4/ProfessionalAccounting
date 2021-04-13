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

using System.IO;
using System.Text;

namespace AccountingServer.Http
{
    internal static class StreamHelper
    {
        private static readonly byte[] CrLf = { (byte)'\r', (byte)'\n' };

        internal static void Write(this Stream stream, string str, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            var data = encoding.GetBytes(str);
            stream.Write(data, 0, data.Length);
        }

        internal static void WriteLine(this Stream stream, string str = null, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            var data = encoding.GetBytes(str ?? "");
            stream.Write(data, 0, data.Length);

            stream.Write(CrLf, 0, CrLf.Length);
        }
    }
}
