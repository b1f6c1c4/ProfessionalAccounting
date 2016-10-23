using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace AccountingServer.Plugins.THUInfo
{
    internal class Crawler
    {
        /// <summary>
        ///     Cookies容器
        /// </summary>
        private readonly CookieContainer m_CookieContainer = new CookieContainer();

        /// <summary>
        ///     临时文件文件名
        /// </summary>
        private string m_FileName;

        /// <summary>
        ///     数据
        /// </summary>
        private List<TransactionRecord> m_Data;

        /// <summary>
        ///     异常
        /// </summary>
        private Exception m_Exception;

        public List<TransactionRecord> Result
        {
            get
            {
                if (m_Exception != null)
                    throw new MemberAccessException("获取数据时发生错误", m_Exception);

                return m_Data;
            }
        }

        /// <summary>
        ///     获取数据
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        public void FetchData(string username, string password)
        {
            m_Exception = null;
            try
            {
                ServicePointManager.DefaultConnectionLimit = 20;

                LoginInfo(username, password);

                PrepareGetUrl();

                var url = GetUrl();

                LoginECard(url);

                using (var stream = DownloadXls())
                    m_FileName = SaveTempFile(stream);

                m_Data = GetData().ToList();

                File.Delete(m_FileName);
            }
            catch (Exception e)
            {
                m_Exception = e;
            }
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
                throw new WebException("下载xls文档时出现错误");

            req.Host = @"ecard.tsinghua.edu.cn";
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
                throw new WebException("登录学生卡管理系统时出现错误");

            req.Host = @"ecard.tsinghua.edu.cn";
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
            var req =
                WebRequest.Create(@"http://info.tsinghua.edu.cn/minichan/roamaction.jsp?id=159") as HttpWebRequest;

            if (req == null)
                throw new WebException("获取令牌时出现错误");

            req.Host = @"info.tsinghua.edu.cn";
            req.Method = "GET";
            req.ContentType = "application/x-www-form-urlencoded";
            req.CookieContainer = m_CookieContainer;
            req.Referer = @"http://info.tsinghua.edu.cn/render.userLayoutRootNode.uP";
            req.Accept = "text/html, application/xhtml+xml, */*";
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko";
            req.AllowAutoRedirect = false;

            var res = req.GetResponse();
            return res.Headers["Location"];
        }

        /// <summary>
        ///     预获取令牌
        /// </summary>
        private void PrepareGetUrl()
        {
            var req =
                WebRequest.Create(@"http://info.tsinghua.edu.cn/render.userLayoutRootNode.uP") as HttpWebRequest;

            if (req == null)
                throw new WebException("获取令牌时出现错误");

            req.Host = @"info.tsinghua.edu.cn";
            req.Method = "GET";
            req.ContentType = "application/x-www-form-urlencoded";
            req.CookieContainer = m_CookieContainer;
            req.Referer = @"http://info.tsinghua.edu.cn/prelogin.jsp?result=1";
            req.Accept = "text/html, application/xhtml+xml, */*";
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko";
            req.AllowAutoRedirect = false;

            req.GetResponse();
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
                        .GetBytes($"redirect=NO&userName={username}&password={password}");
            var req = WebRequest.Create(@"http://info.tsinghua.edu.cn/Login") as HttpWebRequest;

            if (req == null)
                throw new WebException("登录信息门户时出现错误");

            req.Host = @"info.tsinghua.edu.cn";
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

        /// <summary>
        ///     读取xls数据
        /// </summary>
        /// <returns>数据</returns>
        private IEnumerable<TransactionRecord> GetData()
        {
            var strCon = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + m_FileName +
                         ";Extended Properties='Excel 8.0;IMEX=1'";
            var conn = new OleDbConnection(strCon);
            conn.Open();

            var cmd = new OleDbDataAdapter("SELECT * FROM [Sheet1$]", conn);
            var ds = new DataSet();

            cmd.Fill(ds, "[Sheet1$]");

            conn.Close();

            return from DataRow row in ds.Tables[0].Rows
                   where !row.IsNull(4) && !row.IsNull(5)
                   select new TransactionRecord
                              {
                                  Index = Convert.ToInt32(row[0]),
                                  Location = row[1].ToString().Trim(),
                                  Type = row[2].ToString().Trim(),
                                  Endpoint = row[3].ToString().Trim(),
                                  Time = Convert.ToDateTime(row[4]),
                                  Fund = Convert.ToDouble(row[5])
                              };
        }
    }
}
