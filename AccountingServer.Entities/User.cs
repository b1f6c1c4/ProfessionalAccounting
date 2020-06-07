/* Copyright (C) 2020 b1f6c1c4
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
using System.Threading;

namespace AccountingServer.Entities
{
    /// <summary>
    ///     客户端用户
    /// </summary>
    public class ClientUser
    {
        private static readonly ThreadLocal<ClientUser> Instances = new ThreadLocal<ClientUser>();

        private readonly string m_User;

        private ClientUser(string user) => m_User = user;

        public static string Name => Instances.Value?.m_User ?? throw new InvalidOperationException("必须有一个用户");

        public static void Set(string user)
            => Instances.Value = new ClientUser(user);
    }
}
