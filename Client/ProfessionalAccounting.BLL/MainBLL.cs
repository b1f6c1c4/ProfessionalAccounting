using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ProfessionalAccounting.DAL;
using ProfessionalAccounting.Entities;
using ProfessionalAccounting.TCP;

namespace ProfessionalAccounting.BLL
{
    public class MainBLL
    {
        private readonly IDbHelper m_Db;
        private readonly TcpHelper m_Tcp;

        public MainBLL()
        {
            m_Db = new TextFileHelper();
            m_Tcp = new TcpHelper();
            m_Tcp.ReceivedCommand += cmd =>
                                     {
                                         switch (cmd)
                                         {
                                             case CommandType.ClearBalances:
                                                 m_Db.ClearBalanceItems();
                                                 break;
                                             case CommandType.ClearPatterns:
                                                 m_Db.ClearPattern();
                                                 break;
                                             case CommandType.Finished:
                                                 m_Db.SaveData();
                                                 break;
                                         }
                                     };
            m_Tcp.ReceivedBalanceItem += m_Db.AddBalanceItem;
            m_Tcp.ReceivedPattern += m_Db.AddPattern;
            m_Tcp.ReceivedClearDatas += count => m_Db.ClearData(count);
        }

        public async Task ExchangeDataAsync(IPEndPoint ipEndPoint)
        {
            await m_Tcp.ConnectAsync(ipEndPoint);
            foreach (var data in m_Db.GetDatas())
                m_Tcp.Send(data);
            m_Tcp.Send("FINISHED");
            m_Tcp.Receive();
        }

        public IEnumerable<BalanceItem> GetBalanceItems() { return m_Db.GetBalanceItems(); }
        public IEnumerable<PatternData> GetPatternDatas() { return m_Db.GetDatas(); }
        public IEnumerable<PatternUI> GetPatterns() { return m_Db.Patterns(); }
        public void AddData(PatternData data) { m_Db.AddData(data); }
        public void RemoveData(PatternData data) { m_Db.RemoveData(data); }
        public void SaveData() { m_Db.SaveData(); }
    }
}
