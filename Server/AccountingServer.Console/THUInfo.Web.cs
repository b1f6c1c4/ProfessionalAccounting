using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AccountingServer.BLL;

namespace AccountingServer.Console
{
    /// <summary>
    ///     从info.tsinghua.edu.cn更新账户
    /// </summary>
    public partial class THUInfo : IDisposable
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        /// <summary>
        ///     Cookies容器
        /// </summary>
        private readonly CookieContainer m_CookieContainer = new CookieContainer();

        /// <summary>
        ///     临时文件文件名
        /// </summary>
        private string m_FileName;

        public THUInfo(Accountant accountant) { m_Accountant = accountant; }

        /// <summary>
        ///     获取数据
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        public void FetchData(string username, string password)
        {
            ServicePointManager.DefaultConnectionLimit = 20;

            LoginInfo(username, password);

            var url = GetUrl();

            LoginECard(url);

            using (var stream = DownloadXls())
                m_FileName = SaveTempFile(stream);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (m_FileName != null)
                File.Delete(m_FileName);
        }

        /// <summary>
        ///     下载xls文档
        /// </summary>
        /// <returns>流</returns>
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

        /// <summary>
        ///     登录学生卡管理系统
        /// </summary>
        /// <param name="url">令牌</param>
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

        /// <summary>
        ///     获取令牌
        /// </summary>
        /// <returns>令牌</returns>
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

        /// <summary>
        ///     登录信息门户
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
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

        /// <summary>
        ///     保存临时文件
        /// </summary>
        /// <param name="stream">流</param>
        /// <returns>临时文件名</returns>
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
