using System;
using System.Collections.Generic;
using AccountingServer.Entities;

namespace AccountingServer.DAL
{
    public interface IDbHelper : IDisposable
    {
        Voucher SelectVoucher(IObjectID id);
        IEnumerable<Voucher> SelectVouchers(Voucher filter);
        int SelectVouchersCount(Voucher filter);
        bool InsertVoucher(Voucher entity);
        int DeleteVouchers(Voucher filter);

        VoucherDetail SelectDetail(IObjectID id);
        IEnumerable<Voucher> SelectItemsWithDetail(VoucherDetail filter);
        IEnumerable<VoucherDetail> SelectDetails(VoucherDetail filter);
        int SelectDetailsCount(VoucherDetail filter);
        bool InsertDetail(VoucherDetail entity);
        int DeleteDetails(VoucherDetail filter);


        DbAsset SelectAsset(Guid id);
        IEnumerable<DbAsset> SelectAssets(DbAsset filter);
        bool InsertAsset(DbAsset entity);
        int DeleteAssets(DbAsset filter);


        IEnumerable<VoucherDetail> GetXBalances(VoucherDetail filter, bool noCarry = false, int? sID = null, int? eID = null, int dir = 0);
        void Depreciate();
        void Carry();
        IEnumerable<DailyBalance> GetDailyBalance(decimal title, string remark, int dir = 0);
        IEnumerable<DailyBalance> GetDailyXBalance(decimal title, int dir = 0);
        string GetFixedAssetName(Guid id);
    }
}
