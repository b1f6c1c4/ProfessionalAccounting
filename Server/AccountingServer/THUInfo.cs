using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Office.Interop.Excel;

namespace AccountingServer
{
    public static class THUInfo
    {
        private static readonly CookieContainer cookie = new CookieContainer();

        private static Stream Get(string url, Encoding encoding, StringBuilder sb, string referer, string host,
                                  bool allowAutoRedirect, bool keepAlive = false, bool showResponse = true)
        {
            var req = WebRequest.Create(url) as HttpWebRequest;

            Debug.Assert(req != null, "req != null");

            req.Host = host;
            req.KeepAlive = keepAlive;
            req.Method = "GET";
            req.ContentType = "application/x-www-form-urlencoded";
            req.CookieContainer = cookie;
            req.Referer = referer;
            req.Accept = "text/html, application/xhtml+xml, */*";
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko";
            req.AllowAutoRedirect = allowAutoRedirect;

            sb.AppendLine("###################################");
            sb.AppendFormat("GET {0} HTTP 1.1", url);
            sb.AppendLine();
            foreach (var key in req.Headers.AllKeys)
            {
                sb.AppendFormat("{0}{1}", key.CPadRight(25), req.Headers[key]);
                sb.AppendLine();
            }
            sb.AppendLine("-----------------------------------");
            sb.AppendLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            var res = req.GetResponse();

            foreach (var key in res.Headers.AllKeys)
            {
                sb.AppendFormat("{0}{1}", key.CPadRight(25), res.Headers[key]);
                sb.AppendLine();
            }
            sb.AppendLine("-----------------------------------");
            var responseStream = res.GetResponseStream();
            if (responseStream != null && showResponse)
                using (var sr = new StreamReader(responseStream, encoding))
                    sb.AppendLine(sr.ReadToEnd());
            sb.AppendLine("###################################");

            return responseStream;
        }

        private static Stream Post(string url, string post, Encoding encoding, StringBuilder sb, string referer,
                                   string host, bool allowAutoRedirect, bool keepAlive = false, bool showResponse = true)
        {
            var buf = encoding.GetBytes(post);
            var req = WebRequest.Create(url) as HttpWebRequest;

            Debug.Assert(req != null, "req != null");

            req.Host = host;
            req.KeepAlive = keepAlive;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = buf.Length;
            WebRequest.DefaultWebProxy = null;
            var requestStream = req.GetRequestStream();
            requestStream.Write(buf, 0, buf.Length);
            requestStream.Flush();
            req.CookieContainer = cookie;
            req.Referer = referer;
            req.Accept = "text/html, application/xhtml+xml, */*";
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko";
            req.AllowAutoRedirect = allowAutoRedirect;

            sb.AppendLine("###################################");
            sb.AppendFormat("POST {0} HTTP 1.1", url);
            sb.AppendLine();
            foreach (var key in req.Headers.AllKeys)
            {
                sb.AppendFormat("{0}{1}", key.CPadRight(25), req.Headers[key]);
                sb.AppendLine();
            }
            sb.AppendLine("-----------------------------------");
            sb.AppendLine(post);
            sb.AppendLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            var res = req.GetResponse();

            foreach (var key in res.Headers.AllKeys)
            {
                sb.AppendFormat("{0}{1}", key.CPadRight(25), res.Headers[key]);
                sb.AppendLine();
            }
            sb.AppendLine("-----------------------------------");
            var responseStream = res.GetResponseStream();
            if (responseStream != null && showResponse)
                using (var sr = new StreamReader(responseStream, encoding))
                    sb.AppendLine(sr.ReadToEnd());
            sb.AppendLine("###################################");

            return responseStream;
        }
        private static Stream Post2(string url, string post, Encoding encoding, StringBuilder sb, string referer,
                                   string host, bool allowAutoRedirect, bool keepAlive = false, bool showResponse = true)
        {
            var buf = encoding.GetBytes(post);
            var req = WebRequest.Create(url) as HttpWebRequest;

            Debug.Assert(req != null, "req != null");

            req.Host = host;
            req.KeepAlive = keepAlive;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = buf.Length;
            req.CookieContainer = cookie;
            req.Referer = referer;
            req.Accept = "text/html, application/xhtml+xml, */*";
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko";
            req.AllowAutoRedirect = allowAutoRedirect;
            WebRequest.DefaultWebProxy = null;
            var requestStream = req.GetRequestStream();
            requestStream.Write(buf, 0, buf.Length);
            requestStream.Flush();

            sb.AppendLine("###################################");
            sb.AppendFormat("POST {0} HTTP 1.1", url);
            sb.AppendLine();
            foreach (var key in req.Headers.AllKeys)
            {
                sb.AppendFormat("{0}{1}", key.CPadRight(25), req.Headers[key]);
                sb.AppendLine();
            }
            sb.AppendLine("-----------------------------------");
            sb.AppendLine(post);
            sb.AppendLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            var res = req.GetResponse();

            foreach (var key in res.Headers.AllKeys)
            {
                sb.AppendFormat("{0}{1}", key.CPadRight(25), res.Headers[key]);
                sb.AppendLine();
            }
            sb.AppendLine("-----------------------------------");
            var responseStream = res.GetResponseStream();
            if (responseStream != null && showResponse)
                using (var sr = new StreamReader(responseStream, encoding))
                    sb.AppendLine(sr.ReadToEnd());
            sb.AppendLine("###################################");

            return responseStream;
        }

        public static void ShowCookies(StringBuilder sb)
        {
            sb.AppendLine("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
            var res = cookie.GetCookies(new Uri(@"http://info.tsinghua.edu.cn"));
            for (var i = 0; i < res.Count; i++)
            {
                var c = res[i];
                sb.AppendFormat(
                                "{0},{1},{2},{3},{4}",
                                c.Name.CPadRight(25),
                                c.Value.CPadRight(25),
                                c.Path,
                                c.Comment,
                                c.CommentUri);
                sb.AppendLine();
            }
            sb.AppendLine("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
        }

        public static Stream FetchFromInfo(out string response)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 100;
            var sb = new StringBuilder();

            Post(
                 @"http://info.tsinghua.edu.cn/Login",
                 "userName=2014010914&password=MTQyODU3&redirect=NO",
                 Encoding.GetEncoding("GBK"),
                 sb,
                 @"http://info.tsinghua.edu.cn/",
                 @"info.tsinghua.edu.cn",
                 true, false);
                string url;
            using (var reader = new StreamReader(
                Get(
                    @"http://info.tsinghua.edu.cn/render.userLayoutRootNode.uP",
                    Encoding.GetEncoding("UTF-8"),
                    sb,
                    @"http://info.tsinghua.edu.cn/prelogin.jsp?result=1",
                    @"info.tsinghua.edu.cn",
                    false, false,
                    false)))

            {
                var s = reader.ReadToEnd();
                var regex =
                    new Regex("src=\"(?<url>http://ecard\\.tsinghua\\.edu\\.cn/user/Login\\.do\\?portal=yes&amp;ticket=.*?)\"");
                url = regex.Match(s).Groups["url"].Value.Replace("&amp;","&");
            }

            Get(
                url,
                Encoding.UTF8,
                sb,
                @"http://info.tsinghua.edu.cn/render.userLayoutRootNode.uP",
                @"ecard.tsinghua.edu.cn",
                false, false,
                false);

            Get(
                @"http://ecard.tsinghua.edu.cn/user/UserExDetails.do",
                Encoding.UTF8,
                sb,
                @"http://ecard.tsinghua.edu.cn/user/UserIndex.do?",
                @"ecard.tsinghua.edu.cn",
                false, false,
                false);

            sb.AppendLine(SaveTempFile(

                Post2(
                 @"http://ecard.tsinghua.edu.cn/user/ExDetailsDown.do?",
                 "begindate=&enddate=&transtype=&dept=",
                 Encoding.UTF8,
                 sb,
                 @"http://ecard.tsinghua.edu.cn/user/UserExDetails.do",
                 @"ecard.tsinghua.edu.cn",
                 true, true,
                 false)
                
                ));

            response = sb.ToString();
            return null;
        }

        public static void Analyze(Stream stream)
        {
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
