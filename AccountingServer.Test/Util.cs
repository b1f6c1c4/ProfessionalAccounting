using System;
using AccountingServer.Entities;

namespace AccountingServer.Test
{
    public static class Util
    {
        public static DateTime? ToDateTime(this string b1S)
            => b1S == null ? (DateTime?)null : ClientDateTime.Parse(b1S);
    }
}
