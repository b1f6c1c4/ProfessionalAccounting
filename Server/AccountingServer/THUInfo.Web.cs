using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AccountingServer.BLL;

namespace AccountingServer
{
    public partial class THUInfo : IDisposable
    {
        private readonly Accountant m_Accountant;
        private readonly CookieContainer m_CookieContainer = new CookieContainer();

        private string m_FileName;

        public THUInfo(Accountant accountant) { m_Accountant = accountant; }

        public void FetchData(string username, string password)
        {
            ServicePointManager.DefaultConnectionLimit = 20;

            LoginInfo(username, password);

            var url = GetUrl();

            LoginECard(url);

            using (var stream = DownloadXls())
                m_FileName = SaveTempFile(stream);
        }

        public void Dispose()
        {
            if (m_FileName != null)
                File.Delete(m_FileName);
        }

        private Stream DownloadXls()
        {
            var buf =
                Encoding.UTF8
                        .GetBytes("begindate=&enddate=&transtype=&dept=");
            var req = WebRequest.Create(@"http://ecard.tsinghua.edu.cn/user/ExDetailsDown.do?") as HttpWebRequest;

            if (req == null)
                throw new WebException();

            req.Host = @"ecard.tsinghua.edu.cn";
            req.KeepAlive = false;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = buf.Length;
            req.CookieContainer = m_CookieContainer;
            req.Referer = @"http://ecard.tsinghua.edu.cn/user/UserExDetails.do";
            req.AllowAutoRedirect = true;

            WebRequest.DefaultWebProxy = null;
            var requestStream = req.GetRequestStream();
            requestStream.Write(buf, 0, buf.Length);
            requestStream.Flush();

            var res = req.GetResponse();

            var stream = res.GetResponseStream();
            return stream;
        }

        private void LoginECard(string url)
        {
            var req = WebRequest.Create(url) as HttpWebRequest;

            if (req == null)
                throw new WebException();

            req.Host = @"ecard.tsinghua.edu.cn";
            req.KeepAlive = false;
            req.Method = "GET";
            req.ContentType = "application/x-www-form-urlencoded";
            req.CookieContainer = m_CookieContainer;
            req.Referer = @"http://info.tsinghua.edu.cn/render.userLayoutRootNode.uP";
            req.Accept = "text/html, application/xhtml+xml, */*";
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko";
            req.AllowAutoRedirect = false;

            req.GetResponse();
        }

        private string GetUrl()
        {
            string url;

            var req = WebRequest.Create(@"http://info.tsinghua.edu.cn/render.userLayoutRootNode.uP") as HttpWebRequest;

            if (req == null)
                throw new WebException();

            req.Host = @"info.tsinghua.edu.cn";
            req.KeepAlive = false;
            req.Method = "GET";
            req.ContentType = "application/x-www-form-urlencoded";
            req.CookieContainer = m_CookieContainer;
            req.Referer = @"http://info.tsinghua.edu.cn/prelogin.jsp?result=1";
            req.Accept = "text/html, application/xhtml+xml, */*";
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko";
            req.AllowAutoRedirect = false;

            var res = req.GetResponse();

            using (var stream = res.GetResponseStream())
            {
                if (stream == null)
                    throw new WebException();

                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    var s = reader.ReadToEnd();
                    var regex =
                        new Regex(
                            "src=\"(?<url>http://ecard\\.tsinghua\\.edu\\.cn/user/Login\\.do\\?portal=yes&amp;ticket=.*?)\"");
                    url = regex.Match(s).Groups["url"].Value.Replace("&amp;", "&");
                }
            }

            return url;
        }

        private void LoginInfo(string username, string password)
        {
            var buf =
                Encoding.GetEncoding("GBK")
                        .GetBytes(String.Format("userName={0}&password={1}&redirect=NO", username, password));
            var req = WebRequest.Create(@"http://info.tsinghua.edu.cn/Login") as HttpWebRequest;

            if (req == null)
                throw new WebException();

            req.Host = @"info.tsinghua.edu.cn";
            req.KeepAlive = false;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = buf.Length;
            req.CookieContainer = m_CookieContainer;
            req.Referer = @"http://info.tsinghua.edu.cn/";
            req.AllowAutoRedirect = true;

            WebRequest.DefaultWebProxy = null;
            var requestStream = req.GetRequestStream();
            requestStream.Write(buf, 0, buf.Length);
            requestStream.Flush();

            req.GetResponse();
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
