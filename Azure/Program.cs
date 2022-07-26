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
    public IAsyncEnumerable<string> Result { private get; init; }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        context.HttpContext.Response.StatusCode = Code;
        context.HttpContext.Response.ContentType = ContentType;
        if (!string.IsNullOrEmpty(CacheControl))
            context.HttpContext.Response.Headers["Cache-Control"] = CacheControl;
        if (!string.IsNullOrEmpty(Vary))
            context.HttpContext.Response.Headers["Vary"] = Vary;
        await SendContent(context.HttpContext.Response.Body, Result.Where(static s => s != null).Select(static s => Encoding.UTF8.GetBytes(s)));
    }
}

public static class GetSafe
{
    private static Facade facade;

    static GetSafe() => facade = new Facade();

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

    [FunctionName("safe")]
    public static async Task<IActionResult> ApiSafe(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
    {
        var session = CreateSession(req);
        if (session == null) {
            log.LogError("/api/safe: Missing request headers");
            return new StatusCodeResult(400);
        }

        var expr = req.Query["q"];
        // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var res = facade.SafeExecute(session, expr);
        return new HttpAdapter
            {
                CacheControl = "public, max-age=30",
                Vary = "X-User, X-Serializer, X-ClientDateTime, X-Limit",
                Result = res,
            };
    }
}
