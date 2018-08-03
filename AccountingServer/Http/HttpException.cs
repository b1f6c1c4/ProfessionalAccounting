using System;

namespace AccountingServer.Http
{
    public class HttpException : Exception
    {
        public HttpException(int code) => ResponseCode = code;
        public int ResponseCode { get; }
    }
}
