/* Copyright (C) 2020-2021 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
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

        private HttpResponse Process(HttpRequest request)
        {
            if (OnHttpRequest == null)
                throw new HttpException(501);

            return OnHttpRequest(request);
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
#if DEBUG
                        if (request.Method == "OPTIONS")
                            response = new HttpResponse
                                {
                                    Header = new Dictionary<string, string>
                                        {
                                            { "Access-Control-Allow-Origin", "*" },
                                            { "Access-Control-Allow-Methods", "*" },
                                            { "Access-Control-Allow-Headers", "*" },
                                            { "Access-Control-Max-Time", "86400" },
                                        },
                                    ResponseCode = 200,
                                };
                        else
#endif
                            response = Process(request);
                    }
                    catch (HttpException e)
                    {
                        response = new HttpResponse { ResponseCode = e.ResponseCode };
                    }
                    catch (Exception e)
                    {
                        response = HttpUtil.GenerateHttpResponse(e.ToString(), "text/plain; charset=utf-8");
                        response.ResponseCode = 500;
                    }

#if DEBUG
                    response.Header ??= new Dictionary<string, string>();
                    if (!response.Header.ContainsKey("Access-Control-Allow-Origin"))
                        response.Header["Access-Control-Allow-Origin"] = "*";
#endif

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
