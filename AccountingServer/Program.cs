/* Copyright (C) 2020-2025 b1f6c1c4
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
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell;
using AccountingServer.Shell.Util;
using Http;
using static Http.HttpUtil;

var xwd = AppDomain.CurrentDomain.BaseDirectory;

Cfg.DefaultLoader = m => Task.FromResult(new StreamReader(Path.Combine(xwd, "config.d", m + ".xml")));
Console.WriteLine("Loading config files");
await foreach (var s in ConfigFilesManager.InitializeConfigFiles())
    Console.Write(s);
Console.WriteLine("All config files loaded");

var cookieRegex = new Regex(@"(?<=^;\s*)session=(?<session>).*(?=$|;)");

var facade = new Facade();
#if RELEASE
facade.EnableTimer();
#endif
var server = new HttpServer(IPAddress.Any, 30000);
server.OnHttpRequest += Server_OnHttpRequest;
server.Start();

async IAsyncEnumerable<string> ServeInviteHTML(string userHandle)
{
    var ao = await facade.GetAttestationOptions(userHandle);
    await foreach (var s in ResourceHelper.ReadResource("Resources.invite.html", typeof(Program)))
        yield return s;

    yield return ao;
    yield return "\n  </script>\n</body>\n</html>\n";
}

async IAsyncEnumerable<string> ServeLoginHTML()
{
    var ao = await facade.CreateChallenge();
    await foreach (var s in ResourceHelper.ReadResource("Resources.login.html", typeof(Program)))
        yield return s;

    yield return ao;
    yield return "\n  </script>\n</body>\n</html>\n";
}

async ValueTask<HttpResponse> Server_OnHttpRequest(HttpRequest request)
{
#if DEBUG
    var fn = Path.Combine(
        Path.Combine(xwd, "../../../../nginx/dist"),
        (request.BaseUri == "/" ? "/index-desktop.html" : request.BaseUri).TrimStart('/'));
    if (request.Method == "GET")
        if (File.Exists(fn))
            return GenerateHttpResponse(File.OpenRead(fn), fn.Split(".")[^1] switch
                {
                    "html" => "text/html",
                    "js" => "text/javascript",
                    "css" => "text/css",
                    "png" => "image/png",
                    "jpg" or "jpeg" => "image/jpeg",
                    "ico" => "image/x-icon",
                    "txt" => "text/plain",
                    "xml" => "application/xml",
                    "webmanifest" => "application/manifest+json",
                    _ => "application/octet-stream",
                });

    if (request.BaseUri.StartsWith("/api", StringComparison.Ordinal))
        request.BaseUri = request.BaseUri[4..];
#endif

    if (request.Method == "GET" && request.BaseUri.StartsWith("/invite/"))
    {
        var response = GenerateHttpResponse(ServeInviteHTML(request.BaseUri.Substring(8)), "text/html");
        response.Header["Cache-Control"] = "public, max-age=3600";
        return response;
    }
    if (request.Method == "POST" && request.BaseUri.StartsWith("/invite/"))
    {
        if (await facade.RegisterWebAuthn(request.BaseUri.Substring(8), request.ReadToEnd()))
            return new() { ResponseCode = 204 };
        else
            return new() { ResponseCode = 409 };
    }
    if (request.Method == "GET" && request.BaseUri == "/login")
    {
        var response = GenerateHttpResponse(ServeLoginHTML(), "text/html");
        response.Header["Cache-Control"] = "no-store";
        return response;
    }
    if (request.Method == "POST" && request.BaseUri == "/login")
    {
        var session = await facade.VerifyLogin(request.ReadToEnd());
        if (session == null)
            return new() { ResponseCode = 401 };

        var cookie = $"session={session.Key}; HttpOnly; Secure; SameSite=Strict; Path=/; Expires={session.MaxExpiresAt:r};";
        return new()
            {
                ResponseCode = 302,
                Header = new()
                {
                    { "Set-Cookie", cookie },
                    { "Location", "/" },
                },
            };
    }


    string user;
    if (request.Header.ContainsKey("x-user"))
        user = request.Header["x-user"];
    else
        return new() { ResponseCode = 400 };
    if (string.IsNullOrEmpty(user))
        return new() { ResponseCode = 401 };

    string assume = null;
    request.Header.TryGetValue("x-assume-identity", out assume);

    var ca = new CertAuthn();
    request.Header.TryGetValue("x-ssl-fingerprint", out ca.Fingerprint);
    request.Header.TryGetValue("x-ssl-subjectdn", out ca.SubjectDN);
    request.Header.TryGetValue("x-ssl-issuerdn", out ca.IssuerDN);
    request.Header.TryGetValue("x-ssl-serial", out ca.Serial);
    request.Header.TryGetValue("x-ssl-start", out ca.Start);
    request.Header.TryGetValue("x-ssl-end", out ca.End);

    if (!request.Header.ContainsKey("X-ClientDateTime".ToLowerInvariant()) ||
        !DateTimeParser.TryParse(request.Header["X-ClientDateTime".ToLowerInvariant()], out var dt))
        return new() { ResponseCode = 400 };

    var limit = 0;
    if (request.Header.ContainsKey("x-limit") && !int.TryParse(request.Header["x-limit"], out limit))
        return new() { ResponseCode = 400 };

    string spec = null;
    if (request.Header.ContainsKey("x-serializer"))
        spec = request.Header["x-serializer"];

    string key = null;
    if (request.Header.ContainsKey("cookie"))
        key = cookieRegex.Match(request.Header["cookie"])?.Captures?[0].Value;
    var ctx = await facade.AuthnCtx(user, dt, key, ca, assume, spec, limit);

    if (request.Method == "GET")
        switch (request.BaseUri)
        {
            case "/emptyVoucher":
                {
                    var response = GenerateHttpResponse(facade.EmptyVoucher(ctx), "text/plain; charset=utf-8");
                    response.Header["Cache-Control"] = "public, max-age=86400";
                    response.Header["Vary"] = "X-Serializer, X-Limit";
                    return response;
                }
            case "/safe":
                {
                    var expr = request.Parameters?["q"];
                    var res = facade.SafeExecute(ctx, expr);
                    var response = GenerateHttpResponse(res, "text/plain; charset=utf-8");
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
                var res = facade.Execute(ctx, expr);
                return GenerateHttpResponse(res, "text/plain; charset=utf-8");
            }

        case "/voucherUpsert":
            {
                var code = request.ReadToEnd();
                var res = await facade.ExecuteVoucherUpsert(ctx, code);
                return GenerateHttpResponse(res, "text/plain; charset=utf-8");
            }
        case "/voucherRemoval":
            {
                var code = request.ReadToEnd();
                var res = await facade.ExecuteVoucherRemoval(ctx, code);
                return new() { ResponseCode = res ? 204 : 404 };
            }

        case "/assetUpsert":
            {
                var code = request.ReadToEnd();
                var res = await facade.ExecuteAssetUpsert(ctx, code);
                return GenerateHttpResponse(res, "text/plain; charset=utf-8");
            }
        case "/assetRemoval":
            {
                var code = request.ReadToEnd();
                var res = await facade.ExecuteAssetRemoval(ctx, code);
                return new() { ResponseCode = res ? 204 : 404 };
            }

        case "/amortUpsert":
            {
                var code = request.ReadToEnd();
                var res = await facade.ExecuteAmortUpsert(ctx, code);
                return GenerateHttpResponse(res, "text/plain; charset=utf-8");
            }
        case "/amortRemoval":
            {
                var code = request.ReadToEnd();
                var res = await facade.ExecuteAmortRemoval(ctx, code);
                return new() { ResponseCode = res ? 204 : 404 };
            }
        default:
            return new() { ResponseCode = 404 };
    }
}
