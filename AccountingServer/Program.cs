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
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Http;
using AccountingServer.Shell;
using static AccountingServer.Http.HttpUtil;

var facade = new Facade();
#if RELEASE
facade.EnableTimer();
#endif
var server = new HttpServer(IPAddress.Any, 30000);
server.OnHttpRequest += Server_OnHttpRequest;
server.Start();

HttpResponse Server_OnHttpRequest(HttpRequest request)
{
#if DEBUG
    var fn = Path.Combine(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../nginx/dist"),
        (request.BaseUri == "/" ? "/index-desktop.html" : request.BaseUri).TrimStart('/'));
    if (request.Method == "GET")
        if (File.Exists(fn))
            return GenerateHttpResponse(File.OpenRead(fn), fn.Split(".")[^1] switch
                {
                    "html" => "text/html",
                    "js" => "application/javascript",
                    "css" => "text/css",
                    "png" => "image/png",
                    "ico" => "image/x-icon",
                    "txt" => "text/plain",
                    "xml" => "application/xml",
                    _ => "application/octet-stream",
                });

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
        !DateTimeParser.TryParse(request.Header["x-clientdatetime"], out var dt))
        return new() { ResponseCode = 400 };

    var limit = 0;
    if (request.Header.ContainsKey("x-limit") && !int.TryParse(request.Header["x-limit"], out limit))
        return new() { ResponseCode = 400 };

    string spec = null;
    if (request.Header.ContainsKey("x-serializer"))
        spec = request.Header["x-serializer"];

    var session = facade.CreateSession(user, dt, spec, limit);

    if (request.Method == "GET")
        switch (request.BaseUri)
        {
            case "/emptyVoucher":
                {
                    var response = GenerateHttpResponse(facade.EmptyVoucher(session), "text/plain; charset=utf-8");
                    response.Header["Cache-Control"] = "public, max-age=30";
                    response.Header["Vary"] = "X-Serializer, X-Limit";
                    return response;
                }
            case "/safe":
                {
                    var expr = request.Parameters["q"];
                    var res = facade.SafeExecute(session, expr);
                    var response = GenerateHttpResponse(res.ToString(), "text/plain; charset=utf-8");
                    response.Header["X-Type"] = res.GetType().Name;
                    response.Header["X-AutoReturn"] = res.AutoReturn ? "true" : "false";
                    response.Header["X-Dirty"] = res.Dirty ? "true" : "false";
                    response.Header["Cache-Control"] = "public, max-age=30";
                    response.Header["Vary"] = "X-User, X-Serializer, X-ClientDateTime, X-Limit";
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
                var res = facade.Execute(session, expr);
                var response = GenerateHttpResponse(res.ToString(), "text/plain; charset=utf-8");
                response.Header["X-Type"] = res.GetType().Name;
                response.Header["X-AutoReturn"] = res.AutoReturn ? "true" : "false";
                response.Header["X-Dirty"] = res.Dirty ? "true" : "false";
                return response;
            }

        case "/voucherUpsert":
            {
                var code = request.ReadToEnd();
                var res = facade.ExecuteVoucherUpsert(session, code);
                return GenerateHttpResponse(res, "text/plain; charset=utf-8");
            }
        case "/voucherRemoval":
            {
                var code = request.ReadToEnd();
                var res = facade.ExecuteVoucherRemoval(session, code);
                return new() { ResponseCode = res ? 204 : 404 };
            }

        case "/assetUpsert":
            {
                var code = request.ReadToEnd();
                var res = facade.ExecuteAssetUpsert(session, code);
                return GenerateHttpResponse(res, "text/plain; charset=utf-8");
            }
        case "/assetRemoval":
            {
                var code = request.ReadToEnd();
                var res = facade.ExecuteAssetRemoval(session, code);
                return new() { ResponseCode = res ? 204 : 404 };
            }

        case "/amortUpsert":
            {
                var code = request.ReadToEnd();
                var res = facade.ExecuteAmortUpsert(session, code);
                return GenerateHttpResponse(res, "text/plain; charset=utf-8");
            }
        case "/amortRemoval":
            {
                var code = request.ReadToEnd();
                var res = facade.ExecuteAmortRemoval(session, code);
                return new() { ResponseCode = res ? 204 : 404 };
            }
        default:
            return new() { ResponseCode = 404 };
    }
}
