using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using AccountingServer.Entities;
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

            Action<string, string, double?> action =
                (s, d, f) => m_Tcp.Write(String.Format("{0}={1}={2}", s, d, f.AsFullCurrency()));

            action("库存现金", "1001.00", m_Accountant.GetFinalBalance(new Balance { Title = 1001 }));
            action(
                   "学生卡",
                   "1012.05",
                   m_Accountant.GetFinalBalance(new Balance { Title = 1012, SubTitle = 05 }));
            foreach (var s in new[] { "3593", "5184", "9767" })
                action(
                       "借记卡" + s,
                       s,
                       m_Accountant.GetFinalBalance(new Balance { Title = 1002, Content = s }));
            action(
                   "贷记卡6439",
                   "2241.01",
                   -m_Accountant.GetFinalBalance(new Balance { Title = 2241, SubTitle = 01, Content = "6439" }));
            action(
                   "公交卡",
                   "1012.01",
                   m_Accountant.GetFinalBalance(new Balance { Title = 1012, SubTitle = 01, Content = "7094" }));
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
                       m_Accountant.GetFinalBalance(new Balance { Title = 1101, Content = s }));

            Action<string, int?, bool> actionGroup =
                (description, title, b) =>
                {
                    var filter = new VoucherDetail { Title = title };
                    var balances = m_Accountant.FilteredSelect(filter)
                                               .SelectMany(
                                                           v =>
                                                           v.Details.Select(
                                                                            d =>
                                                                            new Tuple<DateTime?, VoucherDetail>(
                                                                                v.Date,
                                                                                d)))
                                               .Where(d => d.Item2.IsMatch(filter))
                                               .GroupBy(
                                                        d => d.Item2.Content,
                                                        (c, ds) =>
                                                        {
                                                            var lst = ds as IList<Tuple<DateTime?, VoucherDetail>> ??
                                                                      ds.ToList();
                                                            var max =
                                                                lst.Max(
                                                                        d =>
                                                                        d.Item1.HasValue
                                                                            ? d.Item1.Value.Ticks
                                                                            : (long?)null);
                                                            return new Balance
                                                                       {
                                                                           Date =
                                                                               max.HasValue
                                                                                   ? new DateTime(max.Value)
                                                                                   : (DateTime?)null,
                                                                           Title = filter.Title,
                                                                           SubTitle = filter.SubTitle,
                                                                           Content = c,
                                                                           Fund = lst.Sum(d => d.Item2.Fund).Value
                                                                       };
                                                        });
                    foreach (var balance in balances)
                        m_Tcp.Write(
                                    String.Format(
                                                  "{0}={1}={2}",
                                                  String.Format(
                                                                "{0:0.####}元{1}-{2}",
                                                                b ? balance.Fund : -balance.Fund,
                                                                description,
                                                                balance.Date.AsDate()),
                                                  balance.Content,
                                                  balance.Content));
                };
            actionGroup("预付", 1123, true);
            actionGroup("应付", 2202, false);
            actionGroup("其他应收", 1221, true);
            actionGroup("其他应付", 2241, false);
            actionGroup("原材料", 1403, true);
            actionGroup("库存商品", 1405, true);

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

        private PatternAttribute GetPatternAttr(MemberInfo pattern)
        {
            var attr =
                (PatternAttribute)
                Attribute.GetCustomAttribute(pattern, typeof(PatternAttribute));
            return attr;
        }
    }
}
