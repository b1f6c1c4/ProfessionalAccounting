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
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AccountingServer.Http;

internal static class ResponseWriter
{
    public static async Task Write(Stream stream, HttpResponse response)
    {
        await stream.WriteLineAsync($"HTTP/1.1 {response.ResponseCode} {ResponseCodes.Get(response.ResponseCode)}");

        response.Header ??= new();
        response.Header["Connection"] = "close";

        if (response.ResponseStream != null)
        {
            if (!response.Header.ContainsKey("Content-Length"))
            {
                response.Header["Transfer-Encoding"] = "chunked";
                await SendHeader(stream, response.Header);
                await SendContent(stream, response.ResponseStream);
            }
            else
            {
                await SendHeader(stream, response.Header);
                await SendContent(stream, response.ResponseStream, Convert.ToInt64(response.Header["Content-Length"]));
            }
        }
        else if (response.ResponseAsyncEnumerable != null)
        {
            response.Header["Transfer-Encoding"] = "chunked";
            response.Header["Trailer"] = "X-Status";
            await SendHeader(stream, response.Header);
            var good = await SendContent(stream, response.ResponseAsyncEnumerable);
            await SendHeader(stream, new() { { "X-Status", good ? "200" : "500" } });
        }
        else
            await SendHeader(stream, response.Header);
    }

    private static async Task SendHeader(Stream stream, Dictionary<string, string> header)
    {
        foreach (var (key, value) in header)
            await stream.WriteLineAsync($"{key}: {value}");
        await stream.WriteLineAsync();
    }

    private static async Task SendContent(Stream stream, Stream src)
    {
        var buff = new byte[4096];
        while (true)
        {
            var sz = await src.ReadAsync(buff.AsMemory(0, 4096));
            await stream.WriteLineAsync($"{sz:x}");
            await stream.WriteAsync(buff.AsMemory(0, sz));
            await stream.WriteLineAsync();
            if (sz == 0)
                break;
        }

        await stream.WriteLineAsync($"{0:x}");
        await stream.WriteLineAsync();
    }

    private static async Task SendContent(Stream stream, Stream src, long length)
    {
        var buff = new byte[4096];
        var rest = length;
        while (rest > 0)
        {
            var sz = rest < 4096
                ? await src.ReadAsync(buff.AsMemory(0, (int)rest))
                : await src.ReadAsync(buff.AsMemory(0, 4096));
            await stream.WriteAsync(buff.AsMemory(0, sz));
            rest -= sz;
        }
    }

    private static async Task<bool> SendContent(Stream stream, IAsyncEnumerable<byte[]> iae)
    {
        try
        {
            await foreach (var s in iae.Where(static s => s.Length != 0))
            {
                await stream.WriteLineAsync($"{s.Length:x}");
                await stream.WriteAsync(s);
                await stream.WriteLineAsync();
                await stream.FlushAsync();
            }

            await stream.WriteLineAsync($"{0:x}");
            return true;
        }
        catch (Exception e)
        {
            var s = e.ToString().GetBytes();
            await stream.WriteLineAsync($"{s.Length:x}");
            await stream.WriteAsync(s);
            await stream.WriteLineAsync();
            await stream.FlushAsync();
            await stream.WriteLineAsync($"{0:x}");
            return false;
        }
    }
}
