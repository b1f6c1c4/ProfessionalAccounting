using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AccountingServer.Http
{
    public class HttpServer
    {
        public delegate HttpResponse OnHttpRequestEventHandler(HttpRequest request);

        private readonly TcpListener m_Listener;

        private readonly Thread m_ListenerThread;

        public HttpServer(IPAddress ip, int port)
        {
            m_Listener = new TcpListener(ip, port);
            m_ListenerThread = new Thread(MainProcess)
                {
                    IsBackground = true,
                    Name = "HttpServer"
                };
        }

        public event OnHttpRequestEventHandler OnHttpRequest;

        public void Fork() => m_ListenerThread.Start();

        public void Start()
        {
            Fork();
            m_ListenerThread.Join();
        }

        private void MainProcess()
        {
            m_Listener.Start();
            while (true)
            {
                var tcp = m_Listener.AcceptTcpClient();
                var thr = new Thread(o => Process((TcpClient)o));
                thr.Start(tcp);
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
