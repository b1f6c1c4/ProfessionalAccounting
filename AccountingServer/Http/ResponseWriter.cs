using System;
using System.Collections.Generic;
using System.IO;

namespace AccountingServer.Http
{
    internal static class ResponseWriter
    {
        public static void Write(Stream stream, HttpResponse response)
        {
            stream.WriteLine($"HTTP/1.1 {response.ResponseCode} {ResponseCodes.Get(response.ResponseCode)}");

            if (response.Header == null)
                response.Header = new Dictionary<string, string>();

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
            foreach (var kvp in response.Header)
                stream.WriteLine($"{kvp.Key}: {kvp.Value}");
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
