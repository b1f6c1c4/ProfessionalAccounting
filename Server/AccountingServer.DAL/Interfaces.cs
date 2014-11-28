using System;
using System.Collections.Generic;
using AccountingServer.Entities;

namespace AccountingServer.DAL
{
    public interface IDbHelper : IDisposable
    {
        IEnumerable<DbTitle> SelectTitles(DbTitle filter);
        bool InsertTitle(DbTitle entity);
        int DeleteTitles(DbTitle filter);


        IEnumerable<DbItem> SelectItems(DbItem filter);
        int SelectItemsCount(DbItem filter);
        bool InsertItem(DbItem entity);
        int DeleteItems(DbItem filter);


        IEnumerable<DbDetail> SelectDetails(DbDetail filter);
        int SelectDetailsCount(DbDetail filter);
        bool InsertDetail(DbDetail entity);
        int DeleteDetails(DbDetail filter);


        IEnumerable<DbFixedAsset> SelectFixedAssets(DbFixedAsset filter);
        bool InsertFixedAsset(DbFixedAsset entity);
        int DeleteFixedAssets(DbFixedAsset filter);


        IEnumerable<DbShortcut> SelectShortcuts(DbShortcut filter);
        bool InsertShortcuts(DbShortcut entity);
        int DeleteShortcuts(DbShortcut filter);

        void DeltaItemsFrom(int id);

        IEnumerable<DbDetail> GetXBalances(DbDetail filter, bool noCarry = false, int? sID = null, int? eID = null, int dir = 0);
        void Depreciate();
        void Carry();
        IEnumerable<DailyBalance> GetDailyBalance(decimal title, string remark, int dir = 0);
        IEnumerable<DailyBalance> GetDailyXBalance(decimal title, int dir = 0);
        int GetIDFromDate(DateTime date);
        string GetFixedAssetName(Guid id);
    }
}
