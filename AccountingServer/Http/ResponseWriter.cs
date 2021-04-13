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
using System.IO;

namespace AccountingServer.Http
{
    internal static class ResponseWriter
    {
        public static void Write(Stream stream, HttpResponse response)
        {
            stream.WriteLine($"HTTP/1.1 {response.ResponseCode} {ResponseCodes.Get(response.ResponseCode)}");

            response.Header ??= new();
            response.Header["Connection"] = "close";

            if (response.ResponseStream != null &&
                !response.Header.ContainsKey("Content-Length"))
                response.Header["Transfer-Encoding"] = "chunked";

            SendHeader(stream, response);

            if (response.ResponseStream == null)
                return;

            if (response.Header.ContainsKey("Content-Length"))
                SendContent(stream, response, Convert.ToInt64(response.Header["Content-Length"]));
            else
                SendContent(stream, response);
        }

        private static void SendHeader(Stream stream, HttpResponse response)
        {
            foreach (var (key, value) in response.Header)
                stream.WriteLine($"{key}: {value}");
            stream.WriteLine();
        }

        private static void SendContent(Stream stream, HttpResponse response)
        {
            var buff = new byte[4096];
            while (true)
            {
                var sz = response.ResponseStream.Read(buff, 0, 4096);
                stream.WriteLine($"{sz:x}");
                stream.Write(buff, 0, sz);
                stream.WriteLine();
                if (sz == 0)
                    break;
            }
        }

        private static void SendContent(Stream stream, HttpResponse response, long length)
        {
            var buff = new byte[4096];
            var rest = length;
            while (rest > 0)
            {
                var sz = rest < 4096
                    ? response.ResponseStream.Read(buff, 0, (int)rest)
                    : response.ResponseStream.Read(buff, 0, 4096);
                stream.Write(buff, 0, sz);
                rest -= sz;
            }
        }
    }
}
