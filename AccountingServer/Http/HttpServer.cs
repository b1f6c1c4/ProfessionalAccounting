/* Copyright (C) 2020-2022 b1f6c1c4
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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Http;

public class HttpServer
{
    public delegate ValueTask<HttpResponse> OnHttpRequestEventHandler(HttpRequest request);

    private readonly TcpListener m_Listener;

    public HttpServer(IPAddress ip, int port) => m_Listener = new(ip, port);

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

    private ValueTask<HttpResponse> Process(HttpRequest request)
    {
        if (OnHttpRequest == null)
            throw new HttpException(501);

        return OnHttpRequest(request);
    }

    private async ValueTask Process(TcpClient tcp)
    {
        tcp.NoDelay = true;
        try
        {
            await using var ns = tcp.GetStream();
            await using var stream = new BufferedStream(ns);

            HttpResponse response;
            try
            {
                var request = RequestParser.Parse(stream);
#if DEBUG
                if (request.Method == "OPTIONS")
                    response = new()
                        {
                            Header = new()
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
                    response = await Process(request);
            }
            catch (HttpException e)
            {
                response = new() { ResponseCode = e.ResponseCode };
            }
            catch (Exception e)
            {
                response = HttpUtil.GenerateHttpResponse(e.ToString(), "text/plain; charset=utf-8");
                response.ResponseCode = 500;
            }

#if DEBUG
            response.Header ??= new();
            if (!response.Header.ContainsKey("Access-Control-Allow-Origin"))
                response.Header["Access-Control-Allow-Origin"] = "*";
#endif

            using (response)
                await ResponseWriter.Write(stream, response);

            stream.Close();

            tcp.Close();
        }
        catch (Exception)
        {
            // ignored
        }
    }
}
