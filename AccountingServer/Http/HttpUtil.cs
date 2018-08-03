using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace AccountingServer.Http
{
    public static class HttpUtil
    {
        public static string ReadToEnd(this HttpRequest request, int maxLength = 1048576)
        {
            if (!request.Header.ContainsKey("Content-Length"))
                return null;

            var len = Convert.ToInt32(request.Header["Content-Length"]);
            if (len > maxLength)
                throw new HttpException(413);

            var buff = new byte[len];
            if (len > 0)
                request.RequestStream.Read(buff, 0, len);

            return Encoding.UTF8.GetString(buff);
        }

        public static HttpResponse GenerateHttpResponse(string str, string contentType = "text/json")
        {
            var stream = new MemoryStream();
            var sw = new StreamWriter(stream);
            sw.Write(str);
            sw.Flush();
            return GenerateHttpResponse(stream, contentType);
        }

        public static HttpResponse GenerateHttpResponse(Stream stream, string contentType = "text/json")
        {
            stream.Position = 0;
            return new HttpResponse
                {
                    ResponseCode = 200,
                    Header =
                        new Dictionary<string, string>
                            {
                                { "Content-Type", contentType },
                                { "Content-Length", stream.Length.ToString(CultureInfo.InvariantCulture) }
                            },
                    ResponseStream = stream
                };
        }
    }
}
