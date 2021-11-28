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
using System.Net;
using AccountingServer.Entities;
using AccountingServer.Http;
using AccountingServer.Shell;
using static AccountingServer.Http.HttpUtil;

var facade = new Facade();
var server = new HttpServer(IPAddress.Any, 30000);
server.OnHttpRequest += Server_OnHttpRequest;
server.Start();

HttpResponse Server_OnHttpRequest(HttpRequest request)
{
#if DEBUG
    var fn = Path.Combine(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../nginx/"),
        (request.BaseUri == "/" ? "/index.html" : request.BaseUri).TrimStart('/'));
    if (request.Method == "GET")
        if (File.Exists(fn))
            return GenerateHttpResponse(File.OpenRead(fn), "text/html");

    if (request.BaseUri.StartsWith("/api", StringComparison.Ordinal))
        request.BaseUri = request.BaseUri[4..];
#endif

    string user;
    if (request.Header.ContainsKey("x-user"))
        user = request.Header["x-user"];
    else
        return new() { ResponseCode = 400 };
    if (user == "anonymous")
        return new() { ResponseCode = 401 };

    if (!request.Header.ContainsKey("x-clientdatetime") ||
        !ClientDateTime.TryParse(request.Header["x-clientdatetime"], out var timestamp))
        return new() { ResponseCode = 400 };

    int limit = 0;
    if (request.Header.ContainsKey("x-limit") && !int.TryParse(request.Header["x-limit"], out limit))
        return new() { ResponseCode = 400 };

    ClientUser.Set(user);
    ClientDateTime.Set(timestamp);
    facade.AdjustLimit(limit);

    string spec = null;
    if (request.Header.ContainsKey("x-serializer"))
        spec = request.Header["x-serializer"];

    if (request.Method == "GET")
        switch (request.BaseUri)
        {
            case "/emptyVoucher":
                return GenerateHttpResponse(facade.EmptyVoucher(spec), "text/plain; charset=utf-8");
            case "/autocomplete":
                {
                    var response = GenerateHttpResponse("[]", "application/json; charset=utf-8");
                    response.Header["Cache-Control"] = "public, max-age=30";
                    return response;
                }
            default:
                return new() { ResponseCode = 404 };
        }

    if (request.Method != "POST")
        return new() { ResponseCode = 405 };

    switch (request.BaseUri)
    {
        case "/execute":
            {
                var expr = request.ReadToEnd();
                var res = facade.Execute(expr, spec);
                var response = GenerateHttpResponse(res.ToString(), "text/plain; charset=utf-8");
                response.Header["X-Type"] = res.GetType().Name;
                response.Header["X-AutoReturn"] = res.AutoReturn ? "true" : "false";
                response.Header["X-Dirty"] = res.Dirty ? "true" : "false";
                return response;
            }

        case "/voucherUpsert":
            {
                var code = request.ReadToEnd();
                var res = facade.ExecuteVoucherUpsert(code, spec);
                return GenerateHttpResponse(res, "text/plain; charset=utf-8");
            }
        case "/voucherRemoval":
            {
                var code = request.ReadToEnd();
                var res = facade.ExecuteVoucherRemoval(code, spec);
                return new() { ResponseCode = res ? 204 : 404 };
            }

        case "/assetUpsert":
            {
                var code = request.ReadToEnd();
                var res = facade.ExecuteAssetUpsert(code, spec);
                return GenerateHttpResponse(res, "text/plain; charset=utf-8");
            }
        case "/assetRemoval":
            {
                var code = request.ReadToEnd();
                var res = facade.ExecuteAssetRemoval(code, spec);
                return new() { ResponseCode = res ? 204 : 404 };
            }

        case "/amortUpsert":
            {
                var code = request.ReadToEnd();
                var res = facade.ExecuteAmortUpsert(code, spec);
                return GenerateHttpResponse(res, "text/plain; charset=utf-8");
            }
        case "/amortRemoval":
            {
                var code = request.ReadToEnd();
                var res = facade.ExecuteAmortRemoval(code, spec);
                return new() { ResponseCode = res ? 204 : 404 };
            }
        default:
            return new() { ResponseCode = 404 };
    }
}
