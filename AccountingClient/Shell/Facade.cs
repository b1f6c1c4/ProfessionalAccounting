using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.Remoting;
using System.Text;

namespace AccountingClient.Shell
{
    public interface IQueryResult
    {
        bool AutoReturn { get; }
    }

    internal class Facade
    {
        private readonly HttpClient m_Client = new HttpClient();

        public Facade()
        {
            m_Client.BaseAddress = new Uri(@"http://localhost:3000");
            EmptyVoucher = Run("GET", "/emptyVoucher");
        }

        public string EmptyVoucher { get; private set; }

        private HttpResult Run(string method, string url, string body = null)
        {
            HttpResponseMessage res;
            switch (method)
            {
                case "GET":
                    res = m_Client.GetAsync(url).Result;
                    break;
                case "POST":
                    var content = new StringContent(body, Encoding.UTF8, "text/plain");
                    res = m_Client.PostAsync(url, content).Result;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            var txt = res.Content.ReadAsStringAsync().Result;
            if (!res.IsSuccessStatusCode)
                throw new RemotingException(txt);

            return new HttpResult { Message = res, Result = txt };
        }

        public IQueryResult Execute(string code)
        {
            var res = Run("POST", "/execute", code);
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
