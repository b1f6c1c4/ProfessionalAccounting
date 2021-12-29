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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AccountingServer.Http;

public static class HttpUtil
{
    public static string ReadToEnd(this HttpRequest request, int maxLength = 1048576)
    {
        if (!request.Header.ContainsKey("content-length"))
            return null;

        var len = Convert.ToInt32(request.Header["content-length"]);
        if (len > maxLength)
            throw new HttpException(413);

        var buff = new byte[len];
        if (len > 0)
            request.RequestStream.Read(buff, 0, len);

        return Encoding.UTF8.GetString(buff);
    }

    public static HttpResponse GenerateHttpResponse(string str, string contentType = "application/json")
    {
        var stream = new MemoryStream();
        var sw = new StreamWriter(stream);
        sw.Write(str);
        sw.Flush();
        return GenerateHttpResponse(stream, contentType);
    }

    public static HttpResponse GenerateHttpResponse(Stream stream, string contentType = "application/json")
    {
        stream.Position = 0;
        return new()
            {
                ResponseCode = 200,
                Header =
                    new()
                        {
                            { "Content-Type", contentType },
                            { "Content-Length", stream.Length.ToString(CultureInfo.InvariantCulture) },
                        },
                ResponseStream = stream,
            };
    }

    public static HttpResponse GenerateHttpResponse(IAsyncEnumerable<string> iae,
        string contentType = "application/json")
        => new()
            {
                ResponseCode = 200,
                Header =
                    new() { { "Content-Type", contentType }, },
                ResponseAsyncEnumerable = iae.Select(s => s.GetBytes()),
            };
}
