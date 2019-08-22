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

        public static void Set(string user) { Instances.Value = new ClientUser(user); }
    }
}
