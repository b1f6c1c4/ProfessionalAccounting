using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AccountingServer.Http
{
    public class HttpServer
    {
        public delegate HttpResponse OnHttpRequestEventHandler(HttpRequest request);

        private readonly TcpListener m_Listener;

        public HttpServer(IPAddress ip, int port) => m_Listener = new TcpListener(ip, port);

        public event OnHttpRequestEventHandler OnHttpRequest;

        public void Start()
        {
            m_Listener.Start();
            while (true)
            {
                var tcp = m_Listener.AcceptTcpClient();
                Task.Run(() => Process(tcp));
            }

            // ReSharper disable once FunctionNeverReturns
        }

        private void Process(TcpClient tcp)
        {
            try
            {
                using (var stream = tcp.GetStream())
                {
                    HttpResponse response;
                    try
                    {
                        var request = RequestParser.Parse(stream);
                        if (OnHttpRequest == null)
                            throw new HttpException(501);

                        response = OnHttpRequest(request);
                    }
                    catch (HttpException e)
                    {
                        response = new HttpResponse { ResponseCode = e.ResponseCode };
                    }
                    catch (Exception e)
                    {
                        response = HttpUtil.GenerateHttpResponse(e.ToString(), "text/plain");
                        response.ResponseCode = 500;
                    }

                    using (response)
                        ResponseWriter.Write(stream, response);

                    stream.Close();
                }

                tcp.Close();
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
