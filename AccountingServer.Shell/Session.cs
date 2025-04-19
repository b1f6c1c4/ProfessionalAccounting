/* Copyright (C) 2025 b1f6c1c4
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
using System.Security.Cryptography;
using System.Threading;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;

namespace AccountingServer.Shell;

public class Session
{
    public string Key { get; internal set; }
    public WebAuthn Authn { get; internal set; }
    public DateTime CreatedAt { get; internal set; }
    public DateTime ExpiresAt { get; internal set; }
    public DateTime MaxExpiresAt { get; internal set; }

    internal void Extend()
    {
        ExpiresAt = DateTime.UtcNow.Add(AuthnManager.Config.SessionExpire);
        if (ExpiresAt > MaxExpiresAt)
            ExpiresAt = MaxExpiresAt;
    }
};

public class SessionManager : IDisposable
{
    private readonly Dictionary<string, Session> m_Sessions;
    private readonly Timer m_Cleanup;
    private readonly object m_Lock;
    private readonly DbSession m_Db;

    public SessionManager(DbSession db)
    {
        m_Sessions = new();
        m_Cleanup = new(CleanupSessions, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        m_Lock = new();
        m_Db = db;
    }

    public void Dispose()
        => m_Cleanup?.Dispose();

    public Session CreateSession(WebAuthn aid)
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
        var now = DateTime.UtcNow;
        var session = new Session
            {
                Key = key,
                Authn = aid,
                CreatedAt = now,
                ExpiresAt = now.Add(AuthnManager.Config.SessionExpire),
                MaxExpiresAt = now.Add(AuthnManager.Config.SessionMax),
            };

        lock (m_Lock)
            m_Sessions[key] = session;

        Console.WriteLine($"{now:s}: session created for {aid.IdentityName.AsId()} / {aid.StringID}");

        return session;
    }

    public Session AccessSession(string key)
    {
        if (key == null)
            return null;

        Session session;
        lock (m_Lock)
            if (!m_Sessions.TryGetValue(key, out session))
                return null;

        var now = DateTime.UtcNow;
        if (now <= session.CreatedAt)
            return null;

        if (now <= session.ExpiresAt && now <= session.MaxExpiresAt)
            return session;

        lock (m_Lock)
            m_Sessions.Remove(key);

        return null;
    }

    public void CleanupSessions(object _)
    {
        lock (m_Lock)
        {
            var now = DateTime.UtcNow;
            var lst = new List<string>();

            foreach (var kvp in m_Sessions)
                if (now >= kvp.Value.ExpiresAt)
                    lst.Add(kvp.Key);

            if (lst.Count > 0)
                Console.WriteLine($"{now:s}: cleaning up {lst.Count} stale sessions, {m_Sessions.Count - lst.Count} active");

            foreach (var key in lst)
                m_Sessions.Remove(key);
        }
    }

}
