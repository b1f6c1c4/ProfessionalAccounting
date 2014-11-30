using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;

namespace AccountingServer
{
    public static class THUInfo
    {
        public static async Task<Stream> FetchFromInfo()
        {
            var cookie = new CookieContainer();
            {
                var req = WebRequest.Create(@"http://info.tsinghua.edu.cn/") as HttpWebRequest;

                Debug.Assert(req != null, "req != null");

                req.KeepAlive = false;
                req.Method = "GET";
                req.CookieContainer = cookie;
                req.Accept = "text/html, application/xhtml+xml, */*";
                req.ContentType = "application/x-www-form-urlencoded";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko";
                req.Credentials = CredentialCache.DefaultCredentials;
                req.AllowAutoRedirect = false;
                var res = await req.GetResponseAsync();
                var responseStream = res.GetResponseStream();
                if (responseStream != null)
                    Debug.Print(SaveTempFile(responseStream));
                //using (var sr = new StreamReader(responseStream, Encoding.GetEncoding("GB2312")))
                //    Debug.Print(sr.ReadToEnd());
            }
            {
                var buf =
                    Encoding.GetEncoding("GB2312").GetBytes("redirect=NO&userName=2014010914&password=MTQyODU3&x=5&y=5");
                var req = WebRequest.Create(@"https://info.tsinghua.edu.cn:443/Login") as HttpWebRequest;

                Debug.Assert(req != null, "req != null");

                req.KeepAlive = false;
                req.Method = "POST";
                req.ContentLength = buf.Length;
                req.GetRequestStream().Write(buf, 0, buf.Length);
                req.GetRequestStream().Flush();
                req.CookieContainer = cookie;
                req.Referer = @"http://info.tsinghua.edu.cn/";
                req.Accept = "text/html, application/xhtml+xml, */*";
                req.ContentType = "application/x-www-form-urlencoded";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko";
                req.Credentials = CredentialCache.DefaultCredentials;
                req.AllowAutoRedirect = false;
                var res = await req.GetResponseAsync();
                var responseStream = res.GetResponseStream();
                if (responseStream != null)
                    using (var sr = new StreamReader(responseStream, Encoding.GetEncoding("GB2312")))
                        Debug.Print(sr.ReadToEnd());
            }
            return null;
            {
                var buf = Encoding.GetEncoding("GB2312").GetBytes("begindate=&enddate=&transtype=&dept=");
                var req = WebRequest.Create(@"http://ecard.tsinghua.edu.cn/user/ExDetailsDown.do?") as HttpWebRequest;

                Debug.Assert(req != null, "req != null");

                req.KeepAlive = false;
                req.Method = "POST";
                req.ContentLength = buf.Length;
                req.GetRequestStream().Write(buf, 0, buf.Length);
                req.CookieContainer = cookie;
                req.Accept = "text/html, application/xhtml+xml, */*";
                req.ContentType = "application/x-www-form-urlencoded";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko";
                req.Credentials = CredentialCache.DefaultCredentials;
                req.AllowAutoRedirect = false;
                req.Referer = @"http://ecard.tsinghua.edu.cn/user/UserExDetails.do?dir=jump&currentPage=1";

                var res = await req.GetResponseAsync();
                return res.GetResponseStream();
            }
        }

        public static void Analyze(Stream stream)
        {
            return;
            var tempFileName = SaveTempFile(stream);

            var excel = new ApplicationClass();
            excel.Workbooks.Add(true);
            excel.Visible = true;
            excel.Workbooks.Open(
                                 tempFileName,
                                 Missing.Value,
                                 Missing.Value,
                                 Missing.Value,
                                 Missing.Value,
                                 Missing.Value,
                                 Missing.Value,
                                 Missing.Value,
                                 Missing.Value,
                                 Missing.Value,
                                 Missing.Value,
                                 Missing.Value,
                                 Missing.Value,
                                 Missing.Value,
                                 Missing.Value);
        }

        private static string SaveTempFile(Stream stream)
        {
            var buf = new byte[1024];
            var tempFileName = Path.GetTempPath() + Path.GetRandomFileName() + ".xls";
            using (var s = File.OpenWrite(tempFileName))
            {
                var length = stream.Read(buf, 0, 1024);
                while (length > 0)
                {
                    s.Write(buf, 0, length);
                    length = stream.Read(buf, 0, 1024);
                }

                s.Flush();
            }
            return tempFileName;
        }
    }
}
