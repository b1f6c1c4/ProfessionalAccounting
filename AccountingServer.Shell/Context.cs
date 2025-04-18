/* Copyright (C) 2021-2025 b1f6c1c4
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
using AccountingServer.BLL;
using AccountingServer.Shell.Serializer;
using AccountingServer.Entities;

namespace AccountingServer.Shell;

/// <summary>
///     客户端上下文
/// </summary>
public class Context
{
    internal Context(DbSession db, string user = "anonymous", DateTime? dt = null,
            Identity id = null, Identity tid = null, Session session = null, CertAuthn cert = null,
            string spec = null, int limit = 0)
    {
        Accountant = new(db, user, dt ?? DateTime.UtcNow.Date) { Limit = limit };
        Identity = id;
        TrueIdentity = tid;
        Session = session;
        Certificate = cert;
        Serializer = new SerializerFactory(Client, Identity).GetSerializer(spec);
    }

    /// <summary>
    ///     基本会计业务处理类
    /// </summary>
    internal Accountant Accountant { get; }

    /// <summary>
    ///     表示器
    /// </summary>
    internal IEntitiesSerializer Serializer { get; }

    /// <summary>
    ///     客户端
    /// </summary>
    public Client Client => Accountant.Client ?? throw new ApplicationException("Client should have been set");

    /// <summary>
    ///     客户端身份
    /// </summary>
    public Identity Identity { get; }

    /// <summary>
    ///     客户端真实身份
    /// </summary>
    public Identity TrueIdentity { get; }

    /// <summary>
    ///     客户端会话
    /// </summary>
    public Session Session { get; }

    /// <summary>
    ///     客户端证书
    /// </summary>
    public CertAuthn Certificate { get; }
}

public interface IIdentityDependable
{
    Identity Identity { set; }
}

public static class IdentityHelper
{
    public static T Assign<T>(this T value, Identity identity) where T : IIdentityDependable
    {
        if (value is { })
            value.Identity = identity;
        return value;
    }
}
