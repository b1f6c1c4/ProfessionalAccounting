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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.Storage;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Sas;
using AccountingServer.Entities.Util;
using AccountingServer.Shell;

public static class Startup
{
    private static readonly ReaderWriterLockSlim m_Lock = new();

    private static Facade m_Facade;

    public static async Task<Facade> Get(ILogger log)
    {
        m_Lock.EnterReadLock();
        try
        {
            if (m_Facade != null)
                return m_Facade;
        }
        finally
        {
            m_Lock.ExitReadLock();
        }

        m_Lock.EnterWriteLock();
        try
        {
            if (m_Facade != null)
                return m_Facade;

            var shareName = Environment.GetEnvironmentVariable("AZURE_SHARE_NAME");
            if (string.IsNullOrEmpty(shareName)) {
                log.LogError("AZURE_SHARE_NAME is not set");
                return null;
            }
            var conn = Environment.GetEnvironmentVariable("AZURE_SHARE_CONN");
            if (string.IsNullOrEmpty(conn)) {
                log.LogError("AZURE_SHARE_CONN is not set");
                return null;
            }
            var share = new ShareClient(conn, shareName);
            if (!await share.ExistsAsync()) {
                log.LogError($"Cannot find AZURE_SHARE_CONN under {shareName}");
                return null;
            }

            var dir = share.GetDirectoryClient("config.d");
            if (!await dir.ExistsAsync()) {
                log.LogError("Cannot find config.d");
                return null;
            }

            Cfg.DefaultLoader = async (filename) => {
                var file = dir.GetFileClient(filename + ".xml");
                if (!await file.ExistsAsync())
                    throw new FileNotFoundException("Cannot find file", filename + ".xml");

                var download = (await file.DownloadAsync()).Value;
                return new(download.Content);
            };

            log.LogInformation("Loading config files");
            await foreach (var s in ConfigFilesManager.InitializeConfigFiles())
                log.LogInformation(s);
            log.LogInformation("All config files loaded");
            m_Facade = new Facade();
            return m_Facade;
        }
        catch (Exception e)
        {
            log.LogError(e.ToString());
            return null;
        }
        finally
        {
            m_Lock.ExitWriteLock();
        }
    }
}
