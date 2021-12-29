using System;
using AccountingServer.BLL;
using AccountingServer.Shell.Serializer;

namespace AccountingServer.Shell;

public class Session
{
    /// <summary>
    ///     基本会计业务处理类
    /// </summary>
    internal Accountant Accountant { get; }

    internal IEntitiesSerializer Serializer { get; }

    public Client Client => Accountant.Client ?? throw new ApplicationException("Client should have been set");

    internal Session(DbSession db, string user = "anonymous", DateTime? dt = null, string spec = null, int limit = 0)
    {
        Accountant = new(db, user, dt ?? DateTime.UtcNow.Date) { Limit = limit };
        Serializer = new SerializerFactory(Client).GetSerializer(spec);
    }
}
