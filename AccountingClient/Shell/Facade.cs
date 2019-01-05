using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Resources;
using System.Runtime.Remoting;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AccountingClient.Properties;

namespace AccountingClient.Shell
{
    public interface IQueryResult
    {
        bool AutoReturn { get; }

        bool Dirty { get; }
    }

    internal class Facade
    {
        private HttpClient m_Client;
        private Exception m_Exception;
        private WebRequestHandler m_Handler;
        private string m_SerializerSpec;

        public Facade() => ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        public async Task<bool> Init() => await TryConnect();

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

        private async Task<bool> TryConnect(string uri = null)
        {
            m_Exception = null;

            try
            {
                m_Handler = new WebRequestHandler();
                var certificate = GetClientCertificate();
                if (certificate != null)
                    m_Handler.ClientCertificates.AddRange(certificate);
                m_Client = new HttpClient(m_Handler) { BaseAddress = new Uri(uri ?? Settings.Default.Server) };
                EmptyVoucher = await Run("GET", "/emptyVoucher");
                return true;
            }
            catch (Exception e)
            {
                m_Exception = e;
                return false;
            }
        }

        private async Task<HttpResult> Run(string method, string url, string body = null, string spec = null)
        {
            if (m_Exception != null)
                throw m_Exception;

            m_Client.DefaultRequestHeaders.Remove("X-ClientDateTime");
            m_Client.DefaultRequestHeaders.Add("X-ClientDateTime", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffff"));
            m_Client.DefaultRequestHeaders.Remove("X-Serializer");
            m_Client.DefaultRequestHeaders.Add("X-Serializer", spec ?? m_SerializerSpec);

            HttpResponseMessage res;
            switch (method)
            {
                case "GET":
                    res = await m_Client.GetAsync("/api" + url);
                    break;
                case "POST":
                    var content = new StringContent(body, Encoding.UTF8, "text/plain");
                    res = await m_Client.PostAsync("/api" + url, content);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            var txt = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new RemotingException(txt);

            return new HttpResult { Message = res, Result = txt };
        }

        public async Task<IQueryResult> Execute(string expr)
        {
            if (expr == "exit")
                Environment.Exit(0);

            if (expr == "??")
            {
                const string resName = "AccountingClient.Resources.Document.txt";
                using (var stream = typeof(Facade).Assembly.GetManifestResourceStream(resName))
                {
                    if (stream == null)
                        throw new MissingManifestResourceException();

                    using (var reader = new StreamReader(stream))
                        return new QueryResult { Result = reader.ReadToEnd(), AutoReturn = true };
                }
            }

            if (expr == "use local")
            {
                if (!await TryConnect("http://localhost:30000/"))
                    throw m_Exception;

                return new QueryResult { Result = "OK", AutoReturn = true };
            }

            if (expr.StartsWith("spec ", StringComparison.OrdinalIgnoreCase))
            {
                m_SerializerSpec = expr.Substring(5);
                EmptyVoucher = await Run("GET", "/emptyVoucher");

                return new QueryResult { Result = "OK", AutoReturn = true };
            }

            if (expr.StartsWith("use ", StringComparison.OrdinalIgnoreCase))
            {
                if (!await TryConnect(expr.Substring(4)))
                    throw m_Exception;

                return new QueryResult { Result = "OK", AutoReturn = true };
            }

            if (expr.StartsWith("always use cert ", StringComparison.OrdinalIgnoreCase))
            {
                Settings.Default.Cert = expr.Substring(16);
                Settings.Default.Save();
                if (!await TryConnect())
                    throw m_Exception;

                return new QueryResult { Result = "OK", AutoReturn = true };
            }

            if (expr.StartsWith("always use ", StringComparison.OrdinalIgnoreCase))
            {
                Settings.Default.Server = expr.Substring(11);
                Settings.Default.Save();
                if (!await TryConnect())
                    throw m_Exception;

                return new QueryResult { Result = "OK", AutoReturn = true };
            }

            var regex = new Regex(@"^(?<spec>[a-z](?:[^-]|-[^-]|---)+)--(?<expr>.*)$");
            var m = regex.Match(expr);
            string spec = null;
            if (m.Success)
            {
                spec = m.Groups["spec"].Value;
                expr = m.Groups["expr"].Value;
            }

            var res = await Run("POST", "/execute", expr, spec);
            return new QueryResult
                {
                    Result = res,
                    AutoReturn = res.Message.Headers.GetValues("X-AutoReturn").Single() == "true",
                    Dirty = res.Message.Headers.GetValues("X-Dirty").Single() == "true"
                };
        }

        public async Task<string> ExecuteVoucherUpsert(string code) => await Run("POST", "/voucherUpsert", code);

        public async Task<string> ExecuteAssetUpsert(string code) => await Run("POST", "/assetUpsert", code);

        public async Task<string> ExecuteAmortUpsert(string code) => await Run("POST", "/amortUpsert", code);

        public async Task<bool> ExecuteVoucherRemoval(string code) =>
            (await Run("POST", "/voucherRemoval", code)).Message.IsSuccessStatusCode;

        public async Task<bool> ExecuteAssetRemoval(string code) =>
            (await Run("POST", "/assetRemoval", code)).Message.IsSuccessStatusCode;

        public async Task<bool> ExecuteAmortRemoval(string code) =>
            (await Run("POST", "/amortRemoval", code)).Message.IsSuccessStatusCode;

        private struct QueryResult : IQueryResult
        {
            public string Result;
            public bool AutoReturn { get; set; }
            public bool Dirty { get; set; }
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
