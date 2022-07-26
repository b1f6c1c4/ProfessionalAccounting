/* Copyright (C) 2022 b1f6c1c4
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AccountingServer.Entities;
using AccountingServer.Shell;
using static AccountingServer.Http.ResponseWriter;

namespace Azure;

internal class HttpAdapter : IActionResult
{
    public int Code { private get; init; } = 200;
    public string ContentType { private get; init; } = "text/plain; charset=utf-8";
    public string CacheControl { private get; init; }
    public string Vary { private get; init; }
    private byte[] ResultBytes { get; }
    private IAsyncEnumerable<byte[]> ResultIAE { get; }

    public HttpAdapter(IAsyncEnumerable<string> iae)
        => ResultIAE = iae.Where(static s => s != null).Select(static s => Encoding.UTF8.GetBytes(s));

    public HttpAdapter(string str)
        => ResultBytes = Encoding.UTF8.GetBytes(str);

    public HttpAdapter(bool success)
        => Code = success ? 204 : 404;

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var r = context.HttpContext.Response;
        r.StatusCode = Code;
        r.ContentType = ContentType;
        if (!string.IsNullOrEmpty(CacheControl))
            r.Headers["Cache-Control"] = CacheControl;
        if (!string.IsNullOrEmpty(Vary))
            r.Headers["Vary"] = Vary;
        if (ResultIAE != null)
            await SendContent(r.Body, ResultIAE);
        else {
            r.ContentLength = ResultBytes.Length;
            await r.Body.WriteAsync(ResultBytes);
        }
    }
}

public static class Program
{
    private static Facade facade;

    static Program() => facade = new Facade();

    private static Session CreateSession(HttpRequest req)
    {
        var user = req.Headers["x-user"];
        if (string.IsNullOrEmpty(user))
            return null;

        var cdt = req.Headers["x-clientdatetime"];
        if (!DateTimeParser.TryParse(cdt, out var dt))
            return null;

        var limit = 0;
        var lm = req.Headers["x-limit"];
        if (!string.IsNullOrEmpty(lm) && !int.TryParse(lm, out limit))
            return null;

        string spec = req.Headers["x-serializer"];
        if (string.IsNullOrEmpty(spec))
            spec = null;

        return facade.CreateSession(user, dt, spec, limit);
    }

    [FunctionName("emptyVoucher")]
    public static async Task<IActionResult> ApiEmptyVoucher(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
            ILogger log)
    {
        var session = CreateSession(req);
        if (session == null) {
            log.LogError("/api/emptyVoucher: Missing request headers");
            return new StatusCodeResult(400);
        }
        return new HttpAdapter(facade.EmptyVoucher(session))
            {
                CacheControl = "public, max-age=30",
                Vary = "X-Serializer, X-Limit",
            };
    }

    [FunctionName("safe")]
    public static async Task<IActionResult> ApiSafe(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
            ILogger log)
    {
        var session = CreateSession(req);
        if (session == null) {
            log.LogError("/api/safe: Missing request headers");
            return new StatusCodeResult(400);
        }
        var expr = req.Query["q"];
        return new HttpAdapter(facade.SafeExecute(session, expr))
            {
                CacheControl = "public, max-age=30",
                Vary = "X-User, X-Serializer, X-ClientDateTime, X-Limit",
            };
    }

    [FunctionName("execute")]
    public static async Task<IActionResult> ApiExecute(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
    {
        var session = CreateSession(req);
        if (session == null) {
            log.LogError("/api/execute: Missing request headers");
            return new StatusCodeResult(400);
        }
        var expr = await new StreamReader(req.Body).ReadToEndAsync();
        return new HttpAdapter(facade.Execute(session, expr));
    }

    [FunctionName("voucherUpsert")]
    public static async Task<IActionResult> ApiVoucherUpsert(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
    {
        var session = CreateSession(req);
        if (session == null) {
            log.LogError("/api/voucherUpsert: Missing request headers");
            return new StatusCodeResult(400);
        }
        var code = await new StreamReader(req.Body).ReadToEndAsync();
        return new HttpAdapter(await facade.ExecuteVoucherUpsert(session, code));
    }

    [FunctionName("voucherRemoval")]
    public static async Task<IActionResult> ApiVoucherRemoval(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
    {
        var session = CreateSession(req);
        if (session == null) {
            log.LogError("/api/voucherRemoval: Missing request headers");
            return new StatusCodeResult(400);
        }
        var code = await new StreamReader(req.Body).ReadToEndAsync();
        return new HttpAdapter(await facade.ExecuteVoucherRemoval(session, code));
    }

    [FunctionName("assetUpsert")]
    public static async Task<IActionResult> ApiAssetUpsert(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
    {
        var session = CreateSession(req);
        if (session == null) {
            log.LogError("/api/assetUpsert: Missing request headers");
            return new StatusCodeResult(400);
        }
        var code = await new StreamReader(req.Body).ReadToEndAsync();
        return new HttpAdapter(await facade.ExecuteAssetUpsert(session, code));
    }

    [FunctionName("assetRemoval")]
    public static async Task<IActionResult> ApiAssetRemoval(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
    {
        var session = CreateSession(req);
        if (session == null) {
            log.LogError("/api/assetRemoval: Missing request headers");
            return new StatusCodeResult(400);
        }
        var code = await new StreamReader(req.Body).ReadToEndAsync();
        return new HttpAdapter(await facade.ExecuteAssetRemoval(session, code));
    }

    [FunctionName("amortUpsert")]
    public static async Task<IActionResult> ApiAmortUpsert(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
    {
        var session = CreateSession(req);
        if (session == null) {
            log.LogError("/api/amortUpsert: Missing request headers");
            return new StatusCodeResult(400);
        }
        var code = await new StreamReader(req.Body).ReadToEndAsync();
        return new HttpAdapter(await facade.ExecuteAmortUpsert(session, code));
    }

    [FunctionName("amortRemoval")]
    public static async Task<IActionResult> ApiAmortRemoval(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
    {
        var session = CreateSession(req);
        if (session == null) {
            log.LogError("/api/amortRemoval: Missing request headers");
            return new StatusCodeResult(400);
        }
        var code = await new StreamReader(req.Body).ReadToEndAsync();
        return new HttpAdapter(await facade.ExecuteAmortRemoval(session, code));
    }
}
