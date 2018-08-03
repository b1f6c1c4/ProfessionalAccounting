using System.Net;
using AccountingServer.Http;
using AccountingServer.Shell;
using static AccountingServer.Http.HttpUtil;

namespace AccountingServer
{
    internal class Program
    {
        private static readonly Facade Facade = new Facade();

        private static void Main()
        {
            var server = new HttpServer(IPAddress.Any, 3000);
            server.OnHttpRequest += Server_OnHttpRequest;
            server.Start();
        }

        private static HttpResponse Server_OnHttpRequest(HttpRequest request)
        {
            if (request.Method == "GET")
            {
                if (request.BaseUri == "/emptyVoucher")
                    return GenerateHttpResponse(Facade.EmptyVoucher, "text/plain");

                return new HttpResponse { ResponseCode = 404 };
            }

            if (request.Method != "POST")
                return new HttpResponse { ResponseCode = 405 };

            switch (request.BaseUri)
            {
                case "/execute":
                    {
                        var expr = request.ReadToEnd();
                        var res = Facade.Execute(expr);
                        var response = GenerateHttpResponse(res.ToString(), "text/plain");
                        response.Header["X-Type"] = res.GetType().Name;
                        response.Header["X-AutoReturn"] = res.AutoReturn ? "true" : "false";
                        return response;
                    }

                case "/voucherUpsert":
                    {
                        var code = request.ReadToEnd();
                        var res = Facade.ExecuteVoucherUpsert(code);
                        return GenerateHttpResponse(res, "text/plain");
                    }
                case "/voucherRemoval":
                    {
                        var code = request.ReadToEnd();
                        var res = Facade.ExecuteVoucherRemoval(code);
                        return new HttpResponse { ResponseCode = res ? 204 : 404 };
                    }

                case "/assetUpsert":
                    {
                        var code = request.ReadToEnd();
                        var res = Facade.ExecuteAssetUpsert(code);
                        return GenerateHttpResponse(res, "text/plain");
                    }
                case "/assertRemoval":
                    {
                        var code = request.ReadToEnd();
                        var res = Facade.ExecuteAssetRemoval(code);
                        return new HttpResponse { ResponseCode = res ? 204 : 404 };
                    }

                case "/amortUpsert":
                    {
                        var code = request.ReadToEnd();
                        var res = Facade.ExecuteAmortUpsert(code);
                        return GenerateHttpResponse(res, "text/plain");
                    }
                case "/amortRemoval":
                    {
                        var code = request.ReadToEnd();
                        var res = Facade.ExecuteAmortRemoval(code);
                        return new HttpResponse { ResponseCode = res ? 204 : 404 };
                    }
                default:
                    return new HttpResponse { ResponseCode = 404 };
            }
        }
    }
}
