using System;
using System.Collections.Generic;
using System.IO;

namespace AccountingServer.Http
{
    public class HttpResponse : IDisposable
    {
        public int ResponseCode { get; set; }
        public Dictionary<string, string> Header { get; set; }
        public Stream ResponseStream { get; set; }
        public void Dispose() => ResponseStream?.Dispose();
    }
}
