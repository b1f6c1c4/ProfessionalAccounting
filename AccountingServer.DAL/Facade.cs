using System;

namespace AccountingServer.DAL
{
    public static class Facade
    {
        public static IDbAdapter Create(string uri = null, string db = null)
        {
            uri = uri ?? Environment.GetEnvironmentVariable("MONGO_URI") ?? "mongodb://localhost";

            if (uri.StartsWith("mongodb://", StringComparison.Ordinal))
                return new MongoDbAdapter(uri, db);

            throw new NotSupportedException("Uri无效");
        }
    }
}
