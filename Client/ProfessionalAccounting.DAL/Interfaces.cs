using System;
using System.Collections.Generic;
using ProfessionalAccounting.Entities;

namespace ProfessionalAccounting.DAL
{
    public interface IDbHelper : IDisposable
    {
        IEnumerable<BalanceItem> GetBalanceItems();
        void AddBalanceItem(BalanceItem item);
        void ClearBalanceItems();

        IEnumerable<PatternUI> Patterns();
        void AddPattern(PatternUI pattern);
        void ClearPattern();

        IEnumerable<PatternData> GetDatas();
        void AddData(PatternData data);
        void RemoveData(PatternData data);
        void ClearData();
        void ClearData(int count);

        void SaveData();
    }
}
