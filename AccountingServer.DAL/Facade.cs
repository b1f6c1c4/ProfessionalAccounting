using System;
using MongoDB.Driver;

namespace AccountingServer.DAL
{
    public static class Facade
    {
        public static IDbAdapter Create(string uri)
        {
            uri = uri ?? "mongodb://localhost";

            if (uri.StartsWith("mongodb://", StringComparison.Ordinal))
                return new MongoDbAdapter(MongoClientSettings.FromUrl(new MongoUrl(uri)));

            throw new NotSupportedException("Uri无效");
        }
    }
}
