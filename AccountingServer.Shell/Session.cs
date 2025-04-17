using System;
using AccountingServer.BLL;
using AccountingServer.Shell.Serializer;

namespace AccountingServer.Shell;

/// <summary>
///     客户端会话
/// </summary>
public class Session
{
    internal Session(DbSession db, string user = "anonymous", DateTime? dt = null,
            Identity id = null, string spec = null, int limit = 0)
    {
        Accountant = new(db, user, dt ?? DateTime.UtcNow.Date) { Limit = limit };
        Serializer = new SerializerFactory(Client).GetSerializer(spec);
        Identity = id;
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
}
