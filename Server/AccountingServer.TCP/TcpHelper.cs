using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AccountingServer.TCP
{
    public delegate void ClientConnectedEventHandler(IPEndPoint ipEndPoint);

    public delegate void ClientDisconnectedEventHandler(IPEndPoint ipEndPoint);

    public delegate void DataArrivalEventHandler(string str);

    public class TcpHelper : ITcpHelper
    {
        private const int Port = 14285;
        private Thread m_ListenThread;
        private readonly Thread m_ReadThread;
        private readonly MemoryStream m_Stream;
        private readonly TcpListener m_Listener;
        private Socket m_Client;
        private IPEndPoint m_EndPoint;
        public event ClientConnectedEventHandler ClientConnected;
        public event ClientDisconnectedEventHandler ClientDisconnected;

        public event DataArrivalEventHandler DataArrival;

        protected void OnClientConnected(IPEndPoint ipEndPoint)
        {
            var handler = ClientConnected;
            if (handler != null)
                handler(ipEndPoint);
        }

        protected void OnClientDisconnected(IPEndPoint ipendpoint)
        {
            var handler = ClientDisconnected;
            if (handler != null)
                handler(ipendpoint);
        }

        protected void OnDataArrival(string str)
        {
            var handler = DataArrival;
            if (handler != null)
                handler(str);
        }

        public TcpHelper()
        {
            m_Stream = new MemoryStream();

            m_Listener = new TcpListener(IPAddress.Any, Port);
            m_Listener.Start(10);
            m_ReadThread = new Thread(Read) { IsBackground = true, Name = "Read" };
            m_ReadThread.Start();
            m_ListenThread = new Thread(Listen) { IsBackground = true, Name = "Listen" };
            m_ListenThread.Start();
        }

        private void Read()
        {
            var readPosition = 0;
            while (true)
                try
                {
                    using (var textStream = new MemoryStream())
                    {
                        while (true)
                        {
                            int ch;
                            lock (m_Stream)
                            {
                                m_Stream.Seek(readPosition, SeekOrigin.Begin);
                                ch = m_Stream.ReadByte();
                            }
                            if (ch == -1)
                            {
                                m_ReadThread.Suspend();
                                continue;
                            }
                            readPosition++;
                            if (ch == 0x0a)
                                break;
                            textStream.WriteByte((byte)ch);
                        }
                        textStream.Seek(0, SeekOrigin.Begin);
                        var textReader = new StreamReader(textStream, Encoding.UTF8);

                        var str = textReader.ReadToEnd();
                        OnDataArrival(str);
                    }
                }
                catch (ThreadAbortException)
                {
                    Console.WriteLine("[ReadTask Aborted]");
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("[Exception From ReadTask]");
                    Console.WriteLine(e);
                }
        }

        ~TcpHelper() { m_Listener.Stop(); }

        public string IPEndPointString
        {
            get
            {
                var nw = NetworkInterface.GetAllNetworkInterfaces();
                //nw.Single(n=>n.Name)
                var n = nw[3];
                var ipai =
                    n.GetIPProperties()
                     .UnicastAddresses.First(a => a.Address.AddressFamily == AddressFamily.InterNetwork);

                //var ipe = Dns.GetHostEntry(Dns.GetHostName());
                //var ipa = ipe.AddressList[1];
                return String.Format("{0}:{1:#}", ipai.Address, Port);
            }
        }

        public void Write(string s)
        {
            var buff = Encoding.UTF8.GetBytes(s + "\n");
            //var buff = Encoding.GetEncoding("GB2312").GetBytes(s + "\r\n");
            Write(buff, buff.Length);
        }

        private void Write(byte[] buff, int length)
        {
            //m_Client.BeginSend(buff, 0, length, SocketFlags.None, ar => { }, m_Client);
            m_Client.Send(buff, 0, length, SocketFlags.None);
        }

        private void Listen()
        {
            while (true)
                try
                {
                    m_Client = m_Listener.AcceptSocket();

                    m_EndPoint = (IPEndPoint)m_Client.RemoteEndPoint;

                    OnClientConnected(m_EndPoint);

                    var data = new byte[1024];

                    while (true)
                    {
                        var recv = m_Client.Receive(data);

                        if (recv == 0)
                            break;

                        lock (m_Stream)
                        {
                            m_Stream.Seek(0, SeekOrigin.End);
                            m_Stream.Write(data, 0, recv);
                        }
                        if (m_ReadThread.ThreadState.HasFlag(ThreadState.Suspended))
                            m_ReadThread.Resume();
                    }

                    Disconnect();
                }
                catch (ThreadAbortException e)
                {
                    Console.WriteLine("[ListenTask Aborted]");
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("[Exception From ListenTask]");
                    Console.WriteLine(e);
                }
        }

        public void Disconnect()
        {
            if (m_Client == null)
                return;
            lock (m_Client)
                if (m_Client != null)
                {
                    lock (m_Stream)
                    {
                        m_Stream.Seek(0, SeekOrigin.End);
                        m_Stream.WriteByte((byte)'\n');
                    }
                    if (m_ReadThread.ThreadState.HasFlag(ThreadState.Suspended))
                        m_ReadThread.Resume();

                    m_ListenThread.Abort();

                    m_Client.Shutdown(SocketShutdown.Both);
                    if (m_Client.Connected)
                        m_Client.Disconnect(true);
                    m_Client.Close();
                    m_Client.Dispose();
                    m_Client = null;
                    OnClientDisconnected(m_EndPoint);

                    m_ListenThread = new Thread(Listen) { IsBackground = true, Name = "Listen" };
                    m_ListenThread.Start();
                }
        }

        public void Stop()
        {
            m_ReadThread.Abort();
            m_ListenThread.Abort();
        }
    }
}
