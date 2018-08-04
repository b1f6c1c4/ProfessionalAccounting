using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AccountingClient.Properties;

namespace AccountingClient.Shell
{
    public interface IQueryResult
    {
        bool AutoReturn { get; }
    }

    internal class Facade
    {
        private HttpClient m_Client;
        private Exception m_Exception;
        private WebRequestHandler m_Handler;

        public Facade()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            TryConnect();
        }

        public string EmptyVoucher { get; private set; }

        private static X509Certificate2Collection GetClientCertificate()
        {
            if (Settings.Default.Cert == "")
                return null;

            using (var userCaStore = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                userCaStore.Open(OpenFlags.ReadOnly);
                var certificatesInStore = userCaStore.Certificates;
                var findResult = certificatesInStore.Find(X509FindType.FindBySubjectName, Settings.Default.Cert, false);
                return findResult.Count > 0 ? findResult : throw new AuthenticationException("找不到证书");
            }
        }

        private bool TryConnect(string uri = null)
        {
            m_Exception = null;

            try
            {
                m_Handler = new WebRequestHandler();
                var certificate = GetClientCertificate();
                if (certificate != null)
                    m_Handler.ClientCertificates.AddRange(certificate);
                m_Client = new HttpClient(m_Handler) { BaseAddress = new Uri(uri ?? Settings.Default.Server) };
                EmptyVoucher = Run("GET", "/emptyVoucher");
                return true;
            }
            catch (Exception e)
            {
                m_Exception = e;
                return false;
            }
        }

        private HttpResult Run(string method, string url, string body = null)
        {
            if (m_Exception != null)
                throw m_Exception;

            m_Client.DefaultRequestHeaders.Add(
                "X-ClientDateTime",
                DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZZ"));

            HttpResponseMessage res;
            switch (method)
            {
                case "GET":
                    res = m_Client.GetAsync("/api" + url).Result;
                    break;
                case "POST":
                    var content = new StringContent(body, Encoding.UTF8, "text/plain");
                    res = m_Client.PostAsync("/api" + url, content).Result;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            var txt = res.Content.ReadAsStringAsync().Result;
            if (!res.IsSuccessStatusCode)
                throw new RemotingException(txt);

            return new HttpResult { Message = res, Result = txt };
        }

        public IQueryResult Execute(string expr)
        {
            if (expr == "exit")
                Environment.Exit(0);

            if (expr.StartsWith("use ", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryConnect(expr.Substring(4)))
                    throw m_Exception;

                return new QueryResult { Result = "OK", AutoReturn = true };
            }

            if (expr.StartsWith("always use cert ", StringComparison.OrdinalIgnoreCase))
            {
                Settings.Default.Cert = expr.Substring(16);
                Settings.Default.Save();
                if (!TryConnect())
                    throw m_Exception;

                return new QueryResult { Result = "OK", AutoReturn = true };
            }

            if (expr.StartsWith("always use ", StringComparison.OrdinalIgnoreCase))
            {
                Settings.Default.Server = expr.Substring(11);
                Settings.Default.Save();
                if (!TryConnect())
                    throw m_Exception;

                return new QueryResult { Result = "OK", AutoReturn = true };
            }

            var res = Run("POST", "/execute", expr);
            return new QueryResult
                {
                    Result = res,
                    AutoReturn = res.Message.Headers.GetValues("X-AutoReturn").Single() == "true"
                };
        }

        public string ExecuteVoucherUpsert(string code) => Run("POST", "/voucherUpsert", code);

        public string ExecuteAssetUpsert(string code) => Run("POST", "/assetUpsert", code);

        public string ExecuteAmortUpsert(string code) => Run("POST", "/amortUpsert", code);

        public bool ExecuteVoucherRemoval(string code) =>
            Run("POST", "/voucherRemoval").Message.IsSuccessStatusCode;

        public bool ExecuteAssetRemoval(string code) =>
            Run("POST", "/assetRemoval").Message.IsSuccessStatusCode;

        public bool ExecuteAmortRemoval(string code) =>
            Run("POST", "/amortRemoval").Message.IsSuccessStatusCode;

        private struct QueryResult : IQueryResult
        {
            public string Result;
            public bool AutoReturn { get; set; }
            public override string ToString() => Result;
        }

        private struct HttpResult
        {
            public string Result;
            public HttpResponseMessage Message;
            public static implicit operator string(HttpResult hr) => hr.Result;
        }
    }
}
