using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using AccountingServer.Entities;
using AccountingServer.TCP;
using ZXing;
using ZXing.QrCode;

namespace AccountingServer.BLL
{
    public static class MainBLL
    {
        private static bool m_Parsing;
        private static int m_SucceedCount;
        private static ITcpHelper m_Tcp;
        private static BHelper m_BHelper;

        private static readonly IEnumerable<MethodInfo> Patterns =
            typeof(Patterns).GetMethods(BindingFlags.Public | BindingFlags.Static);

        public static void Connect(BHelper helper, Action<IPEndPoint> connected, Action<string> received,
                                   Action<IPEndPoint> disconnected)
        {
            m_BHelper = helper;
            m_Tcp = new TcpHelper();
            m_Tcp.ClientConnected += ep =>
                                     {
                                         connected(ep);
                                         m_SucceedCount = 0;
                                         m_Parsing = true;
                                     };
            m_Tcp.DataArrival += str =>
                                 {
                                     received(str);
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
            m_Tcp.ClientDisconnected += ep => disconnected(ep);
        }

        public static System.Drawing.Bitmap GetQRCode(int w,int h)
        {
            var str = m_Tcp.IPEndPointString;

            var qr = new QRCodeWriter();
            var matrix = qr.encode(str, BarcodeFormat.QR_CODE, w, h);
            var bqw = new BarcodeWriter();
            return bqw.Write(matrix);
        }

        public static void Disconnect() { m_Tcp.Disconnect(); }

        public static void SendAllData()
        {
            m_Tcp.Write(String.Format("ClearDatas{0:0}", m_SucceedCount));

            m_Tcp.Write("ClearPatterns");

            foreach (var pattern in Patterns)
            {
                var attr = GetPatternAttr(pattern);
                m_Tcp.Write(String.Format("P{0}={1}={2}", attr.Name, attr.TextPattern, attr.UI));
            }

            m_Tcp.Write("ClearBalances");

            Action<string, string, decimal?> action =
                (s, d, f) => m_Tcp.Write(String.Format("{0}={1}={2}", s, d, f.AsCurrency()));

            action("库存现金", "1001.00", m_BHelper.GetBalance((decimal)1001.00));
            action("学生卡", "1012.05", m_BHelper.GetBalance((decimal)1012.05));
            foreach (var s in new[] { "3593", "5184", "9767" })
                action("借记卡" + s, s, m_BHelper.GetBalance((decimal)1002.00, s));
            action("贷记卡6439", "2241.01", -m_BHelper.GetBalance((decimal)2241.01, "6439"));
            action("公交卡", "1012.01", m_BHelper.GetBalance((decimal)1012.01, ""));
            action("公交卡1476", "1012.01", m_BHelper.GetBalance((decimal)1012.01, "1476"));
            foreach (
                var s in
                    new[]
                        {
                            "广发基金天天红", "民生加银理财月度1027期", "余额宝",
                            "中银活期宝", "中银增利", "中银优选", "中银纯债C",
                            "定存宝A", "月息通 YAD14I3000", "富盈人生第34期"
                        })
                action(
                       s,
                       s,
                       m_BHelper.GetBalance((decimal)1101.01, s) +
                       m_BHelper.GetBalance(new DbTitle {ID = (decimal)1101.02}, s));

            Action<string, decimal?, bool> actionGroup =
                (s, d, b) =>
                {
                    foreach (var detail in m_BHelper.GetXBalances(title: d))
                    {
                        var id = m_BHelper.SelectDetails(
                                                         new DbDetail
                                                             {
                                                                 Title = detail.Title,
                                                                 Remark = detail.Remark
                                                             }).Last().Item;
                        m_Tcp.Write(
                                    String.Format(
                                                  "{0}={1}={2}",
                                                  String.Format(
                                                                "{0:0.####}元{1}-{2}",
                                                                b ? detail.Fund : -detail.Fund,
                                                                s,
                                                                m_BHelper.SelectItem(id.Value).DT.AsDT()),
                                                  detail.Remark,
                                                  detail.Remark));
                    }
                };
            actionGroup("预付", (decimal)1123.00, true);
            actionGroup("应付", (decimal)2202.00, false);
            actionGroup("其他应收", (decimal)1221.00, true);
            actionGroup("其他应付", (decimal)2241.00, false);
            actionGroup("原材料", (decimal)1403.00, true);
            actionGroup("库存商品", (decimal)1405.00, true);

            m_Tcp.Write("FINISHED");
        }


        private static void ParseData(string str)
        {
            var sp = str.Split(new[] {','}, 2);
            foreach (var pattern in from pattern in Patterns
                                    where GetPatternAttr(pattern).Name == sp[0]
                                    select pattern)
            {
                pattern.Invoke(null, new object[] {sp[1], m_BHelper});
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

        public static void Stop() { m_Tcp.Stop(); }
    }
}
