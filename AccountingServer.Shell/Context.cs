using System;
using AccountingServer.BLL;
using AccountingServer.Shell.Serializer;

namespace AccountingServer.Shell;

/// <summary>
///     客户端上下文
/// </summary>
public class Context
{
    internal Context(DbSession db, string user = "anonymous", DateTime? dt = null,
            Identity id = null, Identity tid = null, string spec = null, int limit = 0)
    {
        Accountant = new(db, user, dt ?? DateTime.UtcNow.Date) { Limit = limit };
        Identity = id;
        TrueIdentity = tid;
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
