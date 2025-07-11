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

var facade = new Facade();

var unlimited = false;
string assumedIdentity = null;
if (args.Length == 0)
{
    Console.WriteLine("Info: WebAuthn & certificate authentication enabled");
}
else if (args.Length == 1 && args[0] == "--unlimited")
{
    Console.WriteLine(@"WARNING: Authentication BYPASSED!
ALL unauthenticated connections have [[UNLIMITED resource accesses]]!");
    unlimited = true;
}
else if (args.Length == 2 && args[0] == "--invite")
{
    var ctx = facade.CreateCtx(null, DateTime.UtcNow.Date, Identity.Unlimited);
    await foreach (var s in facade.Invite(ctx, args[1]))
        Console.Write(s);

    return 0;
}
else
{
    Console.WriteLine("Invalid command line arguments");
    return 1;
}

var cookieRegex = new Regex(@"(?<=^|;\s*)session=(.*)(?=$|;)");

#if RELEASE
facade.EnableTimer();
#endif
var server = new HttpServer(IPAddress.Any, 30000);
server.OnHttpRequest += Server_OnHttpRequest;
server.Start();

async ValueTask<HttpResponse> Server_OnHttpRequest(HttpRequest request)
{
#if DEBUG
    Console.WriteLine($"{request.Method} {request.Uri}");
    if (request.Method == "GET")
    {
        var fn = Path.Combine(xwd, "../../../../nginx/dist");
        if (request.BaseUri == "/")
            fn = Path.Combine(fn, "index-desktop.html");
        else if (request.BaseUri == "/invite")
            fn = Path.Combine(fn, "invite.html");
        else
            fn = Path.Combine(fn, request.BaseUri.TrimStart('/'));

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
    }
#endif

    if (request.BaseUri == "/authn/at")
    {
        if (request.Method != "POST")
            return new() { ResponseCode = 405 };

        string uh = null;
        request.Parameters?.TryGetValue("q", out uh);
        if (string.IsNullOrEmpty(uh))
            return new() { ResponseCode = 400 };

        if (!request.Header.TryGetValue("content-type", out var ty) || string.IsNullOrEmpty(ty))
        {
            var aid = await facade.GetATChallenge(uh);
            HttpResponse response;
            if (aid == null)
            {
                response = GenerateHttpResponse("No such invitation");
                response.ResponseCode = 404;
            }
            else if (aid.AttestationOptions == null)
            {
                response = GenerateHttpResponse($"The invitation link has been used at {aid.CreatedAt:s} UTC");
                response.ResponseCode = 409;
            }
            else if (aid.ExpiresAt < DateTime.UtcNow)
            {
                response = GenerateHttpResponse($"The invitation link has expired at {aid.ExpiresAt:s} UTC");
                response.ResponseCode = 410;
            }
            else
            {
                response = GenerateHttpResponse(aid.AttestationOptions, "application/json");
                response.Header["X-ServerName"] = facade.ServerName;
            }
            return response;
        }

        try
        {
            await facade.RegisterWebAuthn(uh, request.ReadToEnd());
            return new() { ResponseCode = 201 };
        }
        catch (Exception e)
        {
            var response = GenerateHttpResponse(e.ToString());
            response.ResponseCode = 409;
            return response;
        }
    }

    if (request.BaseUri == "/authn/as")
    {
        if (request.Method != "POST")
            return new() { ResponseCode = 405 };

        if (!request.Header.TryGetValue("content-type", out var ty) || string.IsNullOrEmpty(ty))
        {
            var response = GenerateHttpResponse(await facade.CreateLogin(), "application/json");
            response.Header["X-ServerName"] = facade.ServerName;
            return response;
        }

        var session = await facade.VerifyLogin(request.ReadToEnd());
        if (session == null)
            return new() { ResponseCode = 401 };

        var cookie = $"session={session.Key}; HttpOnly; Secure; SameSite=Strict; Path=/api; Expires={session.MaxExpiresAt:r};";
        return new() { ResponseCode = 204, Header = new() { { "Set-Cookie", cookie } } };
    }

    if (!request.BaseUri.StartsWith("/api/", StringComparison.InvariantCulture))
        return new() { ResponseCode = 404 };

    string user = null;
    request.Parameters?.TryGetValue("u", out user);
    if (string.IsNullOrEmpty(user))
        return new() { ResponseCode = 400 };

    var dt = DateTime.UtcNow.Date;
    if (request.Header.ContainsKey("x-clientdatetime"))
        DateTimeParser.TryParse(request.Header["x-clientdatetime"], out dt);

    var limit = 0;
    string limitS = null;
    if (request.Parameters?.TryGetValue("limit", out limitS) == true)
        int.TryParse(limitS, out limit);

    string assume = null;
    request.Parameters?.TryGetValue("id", out assume);

    string spec = null;
    request.Parameters?.TryGetValue("spec", out spec);

    request.Header.TryGetValue("x-ssl-verify", out var verify);
    CertAuthn cert = null;
    if (verify == "SUCCESS") {
        cert = new CertAuthn();
        request.Header.TryGetValue("x-ssl-fingerprint", out cert.Fingerprint);
        request.Header.TryGetValue("x-ssl-subjectdn", out cert.SubjectDN);
        request.Header.TryGetValue("x-ssl-issuerdn", out cert.IssuerDN);
        request.Header.TryGetValue("x-ssl-serial", out cert.Serial);
        request.Header.TryGetValue("x-ssl-start", out cert.Start);
        request.Header.TryGetValue("x-ssl-end", out cert.End);
        if (string.IsNullOrEmpty(cert.Fingerprint))
            cert = null;
    }

    string sessionKey = null;
    if (request.Header.ContainsKey("cookie"))
    {
        var match = cookieRegex.Match(request.Header["cookie"]);
        if (match.Success)
            sessionKey = match.Groups[1].Value;
    }

    Context ctx;
    try
    {
        ctx = await facade.AuthnCtx(user, dt, sessionKey, cert, assume, spec, limit, unlimited);
    }
    catch (Exception e)
    {
        request.ReadToEnd(); // don't touch -- dark magic
        var response = GenerateHttpResponse(e.ToString());
        response.ResponseCode = 401;
        response.Header["Set-Cookie"] = $"session=; HttpOnly; Secure; SameSite=Strict; Path=/; Max-Age=0;";
        return response;
    }

    try
    {
        switch (request.BaseUri)
        {
            case "/api/execute":
                if (request.Method == "GET")
                {
                    var expr = request.Parameters?["q"];
                    var res = facade.SafeExecute(ctx, expr);
                    var response = GenerateHttpResponse(res);
                    response.Header["Cache-Control"] = "private, max-age=60";
                    response.Header["Vary"] = "X-Serializer, X-Limit";
                    return response;
                }
                else if (request.Method == "POST")
                {
                    var expr = request.Parameters?["q"];
                    var expr1 = request.ReadToEnd();
                    var res = facade.Execute(ctx, string.IsNullOrEmpty(expr1) ? expr : $"{expr}\n{expr1}");
                    return GenerateHttpResponse(res);
                }
                else
                    return new() { ResponseCode = 405 };
            case "/api/voucher":
                if (request.Method == "GET")
                {
                    var response = GenerateHttpResponse(facade.EmptyVoucher(ctx));
                    response.Header["Cache-Control"] = "public, max-age=86400";
                    response.Header["Vary"] = "X-Serializer";
                    return response;
                }
                return await CRUD(facade.ExecuteVoucherUpsert, facade.ExecuteVoucherRemoval);
            case "/api/asset":
                return await CRUD(facade.ExecuteAssetUpsert, facade.ExecuteAssetRemoval);
            case "/api/amort":
                return await CRUD(facade.ExecuteAmortUpsert, facade.ExecuteAmortRemoval);
            default:
                return new() { ResponseCode = 404 };
        }
    }
    catch (AccessDeniedException ade)
    {
        request.ReadToEnd(); // don't touch -- dark magic
        var response = GenerateHttpResponse(ade.Message);
        response.ResponseCode = 403;
        return response;
    }

    async ValueTask<HttpResponse> CRUD(
            Func<Context, string, ValueTask<string>> upsert,
            Func<Context, string, ValueTask<bool>> removal)
    {
        switch (request.Method)
        {
            case "POST":
                {
                    var code = request.ReadToEnd();
                    var res = await upsert(ctx, code);
                    return GenerateHttpResponse(res);
                }
            case "DELETE":
                {
                    var code = request.ReadToEnd();
                    var res = await removal(ctx, code);
                    return new() { ResponseCode = res ? 204 : 404 };
                }
            default:
                return new() { ResponseCode = 405 };
        }
    }
}

return 0;
