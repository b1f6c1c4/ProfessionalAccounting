using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AccountingServer.Http
{
    internal static class RequestParser
    {
        public static HttpRequest Parse(Stream stream)
        {
            var method = Parse(stream, ParsingState.Method);
            var uri = Parse(stream, ParsingState.Uri);
            Dictionary<string, string> par;
            var baseUri = ParseUri(uri, out par);
            var request = new HttpRequest
                {
                    Method = method,
                    Uri = uri,
                    BaseUri = baseUri,
                    Parameters = par,
                    Header = new Dictionary<string, string>(),
                    RequestStream = stream
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
            par = spp.ToDictionary(s => s.Substring(0, s.IndexOf('=')), s => s.Substring(s.IndexOf('=') + 1));
            return sp[0];
        }

        private static string Parse(Stream stream, ParsingState st)
        {
            switch (st)
            {
                case ParsingState.Method:
                    return ParseMethod(stream);
                case ParsingState.Uri:
                    return ParseUri(stream);
                case ParsingState.HeaderKey:
                    return ParseHeaderKey(stream);
                case ParsingState.HeaderValue:
                    return ParseHeaderValue(stream);
                default:
                    throw new ApplicationException();
            }
        }

        private static string ParseHeaderValue(Stream stream)
        {
            var sb = new StringBuilder();
            while (true)
            {
                var ch = stream.ReadByte();
                if (ch < 0 ||
                    ch > 255)
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
                if (ch < 0 ||
                    ch > 255)
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
                if (ch < 0 ||
                    ch > 255)
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
                if (ch < 0 ||
                    ch > 255)
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
            HeaderValue
        }
    }
}
