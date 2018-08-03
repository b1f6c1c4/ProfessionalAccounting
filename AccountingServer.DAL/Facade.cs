using System;

namespace AccountingServer.DAL
{
    public static class Facade
    {
        public static IDbAdapter Create()
        {
            var uri = Environment.GetEnvironmentVariable("MONGO_URI") ?? "mongodb://localhost";

            if (uri.StartsWith("mongodb://", StringComparison.Ordinal))
                return new MongoDbAdapter(uri);

            throw new NotSupportedException("Uri无效");
        }
    }
}
