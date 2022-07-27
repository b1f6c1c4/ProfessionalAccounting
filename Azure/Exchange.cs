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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AccountingServer.Shell;

public class Exchange
{
    [FunctionName("exchange")]
    public async Task Run(
            [TimerTrigger("14 1 * * * *")] TimerInfo myTimer,
            ILogger log)
    {
        try
        {
            if (await Startup.Get(log) is var facade && facade == null)
                return;
            await facade.ImmediateExchange((s, e) => {
                if (e)
                    log.LogError(s);
                else
                    log.LogInformation(s);
            });
        }
        catch (Exception ex)
        {
            log.LogError(ex.ToString());
        }
    }

    [FunctionName("exchangeNow")]
    public static async Task<IActionResult> RunNow(
            [HttpTrigger(AuthorizationLevel.Admin, "post")] HttpRequest req,
            ILogger log)
    {
        try
        {
            log.LogInformation("Forceful exchange triggered");
            if (await Startup.Get(log) is var facade && facade == null)
                return new StatusCodeResult(500);
            await facade.ImmediateExchange((s, e) => {
                if (e)
                    log.LogError(s);
                else
                    log.LogInformation(s);
            });
            return new StatusCodeResult(204);
        }
        catch (Exception ex)
        {
            log.LogError(ex.ToString());
            return new BadRequestObjectResult(ex);
        }
    }
}
