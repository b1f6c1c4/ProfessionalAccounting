using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using AccountingServer.TCP;
using ZXing;
using ZXing.QrCode;

namespace AccountingServer.BLL
{
    public class MobileComm : IDisposable
    {
        private bool m_Parsing;
        private int m_SucceedCount;
        private ITcpHelper m_Tcp;
        private Accountant m_Accountant;

        private static readonly IEnumerable<MethodInfo> Patterns =
            typeof(MobilePatterns).GetMethods(BindingFlags.Public | BindingFlags.Static);

        public event ClientConnectedEventHandler ClientConnected;
        public event ClientDisconnectedEventHandler ClientDisconnected;
        public event DataArrivalEventHandler DataArrival;

        public void Connect(Accountant helper)
        {
            m_Accountant = helper;
            m_Tcp = new TcpHelper();
            m_Tcp.ClientConnected += ep =>
                                     {
                                         ClientConnected(ep);
                                         m_SucceedCount = 0;
                                         m_Parsing = true;
                                     };
            m_Tcp.DataArrival += str =>
                                 {
                                     DataArrival(str);
                                     if (String.IsNullOrWhiteSpace(str))
                                         return;
                                     if (str == "FINISHED")
                                     {
                                         SendAllData();
                                         //Thread.Sleep(100);
                                         Disconnect();
                                         return;
                                     }
                                     if (m_Parsing)
                                         try
                                         {
                                             ParseData(str);
                                             m_SucceedCount++;
                                         }
                                         catch (Exception e)
                                         {
                                             Console.WriteLine("[FROM Data Parser]{0}", m_SucceedCount + 1);
                                             Console.WriteLine(e);
                                         }
                                 };
            m_Tcp.ClientDisconnected += ClientDisconnected;
        }

        public void Dispose() { m_Tcp.Dispose(); }

        public Bitmap GetQRCode(int w, int h)
        {
            var str = m_Tcp.IPEndPointString;

            var qr = new QRCodeWriter();
            var matrix = qr.encode(str, BarcodeFormat.QR_CODE, w, h);
            var bqw = new BarcodeWriter();
            return bqw.Write(matrix);
        }

        public void Disconnect() { m_Tcp.Disconnect(); }

        public void SendAllData()
        {
            m_Tcp.Write(String.Format("ClearDatas{0:0}", m_SucceedCount));

            m_Tcp.Write("ClearPatterns");

            foreach (var pattern in Patterns)
            {
                var attr = GetPatternAttr(pattern);
                m_Tcp.Write(String.Format("P{0}={1}={2}", attr.Name, attr.TextPattern, attr.UI));
            }

            m_Tcp.Write("ClearBalances");


            m_Tcp.Write("FINISHED");
        }


        private void ParseData(string str)
        {
            var sp = str.Split(new[] { ',' }, 2);
            foreach (var pattern in from pattern in Patterns
                                    where GetPatternAttr(pattern).Name == sp[0]
                                    select pattern)
            {
                pattern.Invoke(null, new object[] { sp[1], m_Accountant });
                break;
            }
        }

        private static PatternAttribute GetPatternAttr(MemberInfo pattern)
        {
            var attr =
                (PatternAttribute)
                Attribute.GetCustomAttribute(pattern, typeof(PatternAttribute));
            return attr;
        }
    }
}
