using System;
using System.IO;
using System.Net;
using AccountingServer.Entities;
using AccountingServer.Http;
using AccountingServer.Shell;
using static AccountingServer.Http.HttpUtil;

namespace AccountingServer
{
    internal static class Program
    {
        private static readonly Facade Facade = new Facade();

        private static void Main()
        {
            var server = new HttpServer(IPAddress.Any, 30000);
            server.OnHttpRequest += Server_OnHttpRequest;
            server.Start();
        }

        private static HttpResponse Server_OnHttpRequest(HttpRequest request)
        {
#if DEBUG
            if (request.BaseUri == "/")
            {
                if (request.Method != "GET")
                    return new HttpResponse { ResponseCode = 405 };

                var fn = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../nginx/index.html");
                return !File.Exists(fn)
                    ? new HttpResponse { ResponseCode = 404 }
                    : GenerateHttpResponse(File.OpenRead(fn), "text/html");
            }

            if (request.BaseUri.StartsWith("/api", StringComparison.Ordinal))
                request.BaseUri = request.BaseUri.Substring(4);
#endif
            string spec = null;
            if (request.Header.ContainsKey("x-serializer"))
                spec = request.Header["x-serializer"];

            if (request.Method == "GET")
            {
                if (request.BaseUri == "/emptyVoucher")
                    return GenerateHttpResponse(Facade.EmptyVoucher(spec), "text/plain; charset=utf-8");

                return new HttpResponse { ResponseCode = 404 };
            }

            if (request.Method != "POST")
                return new HttpResponse { ResponseCode = 405 };

            if (!request.Header.ContainsKey("x-clientdatetime") ||
                !ClientDateTime.TryParse(request.Header["x-clientdatetime"], out var timestamp))
                return new HttpResponse { ResponseCode = 400 };

            ClientDateTime.Set(timestamp);

            switch (request.BaseUri)
            {
                case "/execute":
                    {
                        var expr = request.ReadToEnd();
                        var res = Facade.Execute(expr, spec);
                        var response = GenerateHttpResponse(res.ToString(), "text/plain; charset=utf-8");
                        response.Header["X-Type"] = res.GetType().Name;
                        response.Header["X-AutoReturn"] = res.AutoReturn ? "true" : "false";
                        response.Header["X-Dirty"] = res.Dirty ? "true" : "false";
                        return response;
                    }

                case "/voucherUpsert":
                    {
                        var code = request.ReadToEnd();
                        var res = Facade.ExecuteVoucherUpsert(code, spec);
                        return GenerateHttpResponse(res, "text/plain; charset=utf-8");
                    }
                case "/voucherRemoval":
                    {
                        var code = request.ReadToEnd();
                        var res = Facade.ExecuteVoucherRemoval(code, spec);
                        return new HttpResponse { ResponseCode = res ? 204 : 404 };
                    }

                case "/assetUpsert":
                    {
                        var code = request.ReadToEnd();
                        var res = Facade.ExecuteAssetUpsert(code, spec);
                        return GenerateHttpResponse(res, "text/plain; charset=utf-8");
                    }
                case "/assertRemoval":
                    {
                        var code = request.ReadToEnd();
                        var res = Facade.ExecuteAssetRemoval(code, spec);
                        return new HttpResponse { ResponseCode = res ? 204 : 404 };
                    }

                case "/amortUpsert":
                    {
                        var code = request.ReadToEnd();
                        var res = Facade.ExecuteAmortUpsert(code, spec);
                        return GenerateHttpResponse(res, "text/plain; charset=utf-8");
                    }
                case "/amortRemoval":
                    {
                        var code = request.ReadToEnd();
                        var res = Facade.ExecuteAmortRemoval(code, spec);
                        return new HttpResponse { ResponseCode = res ? 204 : 404 };
                    }
                default:
                    return new HttpResponse { ResponseCode = 404 };
            }
        }
    }
}
