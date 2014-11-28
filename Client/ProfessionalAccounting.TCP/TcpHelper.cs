using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ProfessionalAccounting.Entities;

namespace ProfessionalAccounting.TCP
{
    public enum CommandType
    {
        Connected,
        ClearPatterns,
        ClearBalances,
        Finished,
        Disconnected
    }

    public delegate void ReceivedCommandEventHandler(CommandType type);

    public delegate void ReceivedBalanceItemEventHandler(BalanceItem item);

    public delegate void ReceivedPatternEventHandler(PatternUI pattern);

    public delegate void ReceivedClearDatasEventHandler(int count);

    public class TcpHelper
    {
        private MemoryStream m_Stream;
        private Socket m_Client;

        public event ReceivedCommandEventHandler ReceivedCommand;
        public event ReceivedBalanceItemEventHandler ReceivedBalanceItem;
        public event ReceivedPatternEventHandler ReceivedPattern;
        public event ReceivedClearDatasEventHandler ReceivedClearDatas;

        public async Task ConnectAsync(IPEndPoint ipEndPoint)
        {
            m_Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                await Task.Factory.StartNew(
                                            () =>
                                            {
                                                m_Client.Connect(ipEndPoint);
                                                ReceivedCommand(CommandType.Connected);
                                            });
            }
            catch (Exception e)
            {
                Debug.Print(e.ToString());
                m_Client.Close();
                m_Client.Dispose();
                throw;
            }
        }

        ~TcpHelper()
        {
            if (m_Client != null)
                m_Client.Dispose();
        }

        public void Disconnect()
        {
            m_Client.Close();
            m_Client.Dispose();
        }

        public void Send(PatternData data) { Send(data.ToString()); }

        public void Send(string str) { m_Client.Send(Encoding.UTF8.GetBytes(str + "\n")); }

        public void Receive()
        {
            var data = new byte[1024];
            m_Stream = new MemoryStream();

            Task.Factory.StartNew(Read, TaskCreationOptions.LongRunning);

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
            }
            ReceivedCommand(CommandType.Disconnected);
            Disconnect();
            m_Stream.Close();
            m_Stream.Dispose();
        }

        private void Read()
        {
            var readPosition = 0;
            while (true)
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
                            continue;
                        readPosition++;
                        if (ch == 0x0a)
                            break;
                        textStream.WriteByte((byte)ch);
                    }
                    textStream.Seek(0, SeekOrigin.Begin);
                    var textReader = new StreamReader(textStream, Encoding.UTF8);

                    var str = textReader.ReadToEnd();
                    if (str == "FINISHED")
                        break;
                    if (str == "ClearPatterns")
                        ReceivedCommand(CommandType.ClearPatterns);
                    else if (str == "ClearBalances")
                        ReceivedCommand(CommandType.ClearBalances);
                    else if (str.StartsWith("ClearDatas"))
                        ReceivedClearDatas(Convert.ToInt32(str.Substring(10)));
                    else if (str.StartsWith("P"))
                        ReceivedPattern(PatternUI.Parse(str));
                    else if (str.Length > 0)
                        ReceivedBalanceItem(BalanceItem.Parse(str));
                }
            ReceivedCommand(CommandType.Finished);
        }
    }
}
