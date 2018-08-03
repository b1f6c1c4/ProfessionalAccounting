using System.IO;
using System.Text;

namespace AccountingServer.Http
{
    internal static class StreamHelper
    {
        private static readonly byte[] CrLf = { (byte)'\r', (byte)'\n' };

        internal static void Write(this Stream stream, string str, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            var data = encoding.GetBytes(str);
            stream.Write(data, 0, data.Length);
        }

        internal static void WriteLine(this Stream stream, string str = null, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            var data = encoding.GetBytes(str ?? "");
            stream.Write(data, 0, data.Length);

            stream.Write(CrLf, 0, CrLf.Length);
        }
    }
}
