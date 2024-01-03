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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace Http;

internal static class RequestParser
{
    public static HttpRequest Parse(Stream stream)
    {
        var method = Parse(stream, ParsingState.Method);
        var uri = Parse(stream, ParsingState.Uri);
        var baseUri = ParseUri(uri, out var par);
        var request = new HttpRequest
            {
                Method = method,
                Uri = uri,
                BaseUri = baseUri,
                Parameters = par,
                Header = new(),
                RequestStream = stream,
            };
        while (true)
        {
            var key = Parse(stream, ParsingState.HeaderKey);
            if (string.IsNullOrEmpty(key))
                break;
            var value = Parse(stream, ParsingState.HeaderValue);
            request.Header.Add(key, value);
        }

        return request;
    }

    private static string ParseUri(string uri, out Dictionary<string, string> par)
    {
        var sp = uri.Split(new[] { '?' }, 2);
        if (sp.Length == 1)
        {
            par = null;
            return sp[0];
        }

        var spp = sp[1].Split('&');
        par = spp.ToDictionary(static s => s[..s.IndexOf('=')],
            static s => HttpUtility.UrlDecode(s[(s.IndexOf('=') + 1)..]));
        return sp[0];
    }

    private static string Parse(Stream stream, ParsingState st)
        => st switch
            {
                ParsingState.Method => ParseMethod(stream),
                ParsingState.Uri => ParseUri(stream),
                ParsingState.HeaderKey => ParseHeaderKey(stream),
                ParsingState.HeaderValue => ParseHeaderValue(stream),
                _ => throw new ApplicationException(),
            };

    private static string ParseHeaderValue(Stream stream)
    {
        var sb = new StringBuilder();
        while (true)
        {
            var ch = stream.ReadByte();
            if (ch is < 0 or > 255)
                throw new HttpException(400);
            if (ch == ' ' &&
                sb.Length == 0)
                continue;
            if (ch == '\r')
            {
                ch = stream.ReadByte();
                if (ch != '\n')
                    throw new HttpException(400);
                break;
            }

            sb.Append((char)ch);
        }

        return sb.ToString();
    }

    private static string ParseHeaderKey(Stream stream)
    {
        var sb = new StringBuilder();
        while (true)
        {
            var ch = stream.ReadByte();
            if (ch is < 0 or > 255)
                throw new HttpException(400);
            if (ch == ':')
                break;
            if (ch == '\r')
            {
                if (sb.Length != 0)
                    throw new HttpException(400);
                ch = stream.ReadByte();
                if (ch != '\n')
                    throw new HttpException(400);
                break;
            }

            sb.Append((char)ch);
        }

        return sb.ToString().ToLowerInvariant();
    }

    private static string ParseUri(Stream stream)
    {
        var sb = new StringBuilder();
        while (true)
        {
            var ch = stream.ReadByte();
            if (ch is < 0 or > 255)
                throw new HttpException(400);
            if (ch == '\r')
            {
                ch = stream.ReadByte();
                if (ch != '\n')
                    throw new HttpException(400);
                break;
            }

            sb.Append((char)ch);
        }

        if (sb.ToString(sb.Length - 8, 8) != "HTTP/1.1")
            throw new HttpException(505);
        return sb.ToString(0, sb.Length - 9);
    }

    private static string ParseMethod(Stream stream)
    {
        var sb = new StringBuilder();
        while (true)
        {
            var ch = stream.ReadByte();
            if (ch is < 0 or > 255)
                throw new HttpException(400);
            if (ch == ' ')
                break;
            sb.Append((char)ch);
        }

        switch (sb.ToString())
        {
            case "OPTIONS":
            case "GET":
            case "HEAD":
            case "POST":
            case "PUT":
            case "DELETE":
            case "TRACE":
            case "CONNECT":
                return sb.ToString();
            default:
                throw new HttpException(400);
        }
    }

    private enum ParsingState
    {
        Method,
        Uri,
        HeaderKey,
        HeaderValue,
    }
}
