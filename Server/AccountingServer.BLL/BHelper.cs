using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.DAL;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    public class BHelper
    {
        public const int FullYearProfit = 4103;
        public const int FixedAsset = 6601;
        public const int FixedAssetDepreciation = 6602;
        public const int FixedAssetImpairment = 6603;
        public const int Cost = 5000;

        private IDbHelper m_Db;
        //private string m_DbName;

        //private Dictionary<double, string> m_Titles;

        public void Connect(string un, string pw)
        {
            m_Db = new MongoDbHelper();
            //m_DbName = un;
            //GetTitles();
        }

        public void Disconnect()
        {
            m_Db.Dispose();
            m_Db = null;
        }

        //public bool IsCarry(Voucher entity) { return m_Db.SelectDetails(entity).Any(d => d.Title == FullYearProfit); }

        public double IsBalanced(Voucher entity)
        {
            // ReSharper disable once PossibleInvalidOperationException
            return entity.Details.Sum(d => d.Fund.Value);
            //return m_Db.SelectDetails(new VoucherDetail {Content = entity.ID.ToString()}).Sum(d => d.Fund.Value);
        }

        public double GetBalance(int? title, int? subTitle, string content = null, DateTime? dt = null)
        {
            var lstx = m_Db.SelectVouchers(new Voucher());
            if (dt.HasValue)
                lstx = lstx.Where(i => i.Date <= dt);
            var lst = lstx.Select(i => i.ID).ToList();
            return m_Db.SelectDetails(new VoucherDetail {Title = title, Content = content})
                       .Where(d => lst.Contains(d.Item))
                       .Sum(d => d.Fund.Value);
        }
        
        //public Dictionary<DateTime, double> GetDailyBalance(IEnumerable<double> titles, string content,
        //                                                     DateTime startDate, DateTime endDate, int dir = 0)
        //{
        //    var dic = new Dictionary<DateTime, double>();
        //    var balance = (double)0;
        //    var dt = startDate;
        //    var lst = titles.SelectMany(title => m_Db.GetDailyBalance(title, content, dir)).ToList();
        //    lst.Sort(
        //             (d1, d2) =>
        //             d1.Date.HasValue && d2.Date.HasValue
        //                 ? d1.Date.Value.CompareTo(d2.Date.Value)
        //                 : d1.Date.HasValue ? 1 : d2.Date.HasValue ? -1 : 0);
        //    foreach (var daily in lst)
        //    {
        //        if (!daily.Date.HasValue ||
        //            daily.Date < startDate)
        //        {
        //            balance += daily.Balance;
        //            continue;
        //        }
        //        if (daily.Date > endDate)
        //            break;
        //        while (dt < daily.Date.Value)
        //        {
        //            dic.Add(dt, balance);
        //            dt = dt.AddDays(1);
        //        }
        //        balance += daily.Balance;
        //    }
        //    while (dt <= endDate)
        //    {
        //        dic.Add(dt, balance);
        //        dt = dt.AddDays(1);
        //    }
        //    dic.Add(dt, balance);
        //    return dic;
        //}

        //public double GetABalance(DbTitle entity)
        //{
        //    entity.ABalance = GetBalance(entity);
        //    foreach (var t in m_Db.SelectTitles(new DbTitle {H_ID = entity.ID}))
        //        entity.ABalance += GetABalance(t);
        //    return entity.ABalance.Value;
        //}

        //private double GetABalance(DbTitle entity, ref IList<DbTitle> titles)
        //{
        //    entity.ABalance = GetBalance(entity);
        //    foreach (var t in m_Db.SelectTitles(new DbTitle {H_ID = entity.ID}))
        //        entity.ABalance += GetABalance(t, ref titles);
        //    return entity.ABalance.Value;
        //}

        //public double GetABalance(DbTitle entity, out List<DbTitle> titles, bool cascadeReturn)
        //{
        //    entity.ABalance = GetBalance(entity);
        //    IList<DbTitle> tl = new List<DbTitle>();
        //    foreach (var t in m_Db.SelectTitles(new DbTitle {H_ID = entity.ID}))
        //    {
        //        tl.Add(t);
        //        if (cascadeReturn)
        //            entity.ABalance += GetABalance(t, ref tl);
        //        else
        //            entity.ABalance += GetABalance(t);
        //    }
        //    titles = tl as List<DbTitle>;
        //    return entity.ABalance.Value;
        //}

        //public IEnumerable<VoucherDetail> GetXBalances(DbTitle entity = null, bool withZero = false, bool noCarry = false, int? sID = null, int? eID = null)
        //{
        //    var detail = entity == null ? new VoucherDetail() : new VoucherDetail {Title = entity.ID};
        //    var lst = m_Db.GetXBalances(detail, noCarry, sID, eID);
        //    return withZero ? lst : lst.Where(d => d.Fund != 0);
        //}

        //public IEnumerable<VoucherDetail> GetXBalances(int? title = null, bool withZero = false, bool noCarry = false, int? sID = null, int? eID = null)
        //{
        //    var detail = title == null ? new VoucherDetail() : new VoucherDetail {Title = title};
        //    var lst = m_Db.GetXBalances(detail, noCarry, sID, eID);
        //    return withZero ? lst : lst.Where(d => d.Fund != 0);
        //}

        //public IEnumerable<VoucherDetail> GetXBalancesD(int? title = null, int dir = 0, bool noCarry = false,
        //                                           int? sID = null, int? eID = null)
        //{
        //    var detail = title == null ? new VoucherDetail() : new VoucherDetail { Title = title };
        //    var lst = m_Db.GetXBalances(detail, noCarry, sID, eID, dir);
        //    return lst;
        //}

        //public VoucherDetail GetXBalances(VoucherDetail entity, bool noCarry = false, int? sID = null, int? eID = null)
        //{
        //    var lst = m_Db.GetXBalances(entity, noCarry, sID, eID);
        //    return lst.Single();
        //}

        //private void GetTitles()
        //{
        //    m_Titles = new Dictionary<double, string>();
        //    foreach (var title in m_Db.SelectTitles(new DbTitle()))
        //        if (title.ID != null)
        //            m_Titles.Add(title.ID.Value, title.Name);
        //}

        //public string GetTitleName(double? id)
        //{
        //    if (id.HasValue)
        //        if (m_Titles.ContainsKey(id.Value))
        //            return m_Titles[id.Value];
        //    return null;
        //}
        //public string GetFixedAssetName(Guid id)
        //{
        //    return m_Db.GetFixedAssetName(id);
        //}

        //public double Carry(DateTime? date)
        //{
        //    var dt = date ?? DateTime.Now.Date;
        //    var cnt = (double)0;
        //    // ReSharper disable once PossibleInvalidOperationException
        //    var lst = m_Db.SelectTitles(new DbTitle()).Where(t => IsToCarry(t.ID.Value));
        //    var titles = lst as IList<DbTitle> ?? lst.ToList();
        //    if (!titles.Any())
        //        return 0;
        //    var item = new Voucher {Date = dt};
        //    foreach (var t in titles)
        //    {
        //        var b = GetBalance(t, null, dt);
        //        m_Db.InsertDetail(new VoucherDetail {Item = item.ID, Title = t.ID, Fund = -b});
        //        cnt += b;
        //    }
        //    m_Db.InsertDetail(new VoucherDetail {Item = item.ID, Title = FullYearProfit, Fund = cnt});
        //    return cnt;
        //}

        //private static bool IsToCarry(double id) { return id >= Cost; }

        //public IEnumerable<DbTitle> SelectTitles(DbTitle entity) { return m_Db.SelectTitles(entity); }
        public long SelectVouchersCount(Voucher entity) { return m_Db.SelectVouchersCount(entity); }
        public Voucher SelectVoucher(IObjectID id) { return m_Db.SelectVoucher(id); }
        public IEnumerable<Voucher> SelectVouchers(Voucher entity) { return m_Db.SelectVouchers(entity); }
        public IEnumerable<Voucher> SelectVouchersWithDetail(VoucherDetail entity) { return m_Db.SelectVouchersWithDetail(entity); }
        //public IEnumerable<VoucherDetail> SelectDetails(Voucher entity) { return m_Db.SelectDetails(entity); }
        public IEnumerable<VoucherDetail> SelectDetails(VoucherDetail entity) { return m_Db.SelectDetails(entity); }
        public long SelectDetailsCount(VoucherDetail entity) { return m_Db.SelectDetailsCount(entity); }

        //public IEnumerable<DbFixedAsset> SelectFixedAssets(DbFixedAsset entity)
        //{
        //    return m_Db.SelectFixedAssets(entity);
        //}

        //public IEnumerable<DbShortcut> SelectShortcuts()
        //{
        //    foreach (var shortcut in m_Db.SelectShortcuts(new DbShortcut()))
        //    {
        //        var xpath = Regex.Split(shortcut.Path, @",(?=(?:[^']*'[^']*')*[^']*$)");
        //        shortcut.Balance = 0;
        //        foreach (var path in xpath)
        //        {
        //            var paths = path.Split(new[] {'/'});
        //            if (paths.Last().StartsWith("T"))
        //                shortcut.Balance += GetABalance(new DbTitle {ID = Convert.ToDecimal(paths.Last().Substring(1))});
        //            if (paths.Last().StartsWith("R"))
        //                shortcut.Balance +=
        //                    GetXBalances(
        //                                 new VoucherDetail
        //                                     {
        //                                         Title = Convert.ToDecimal(paths[paths.Length - 2].Substring(1)),
        //                                         Content = paths.Last().Substring(2, paths.Last().Length - 3)
        //                                     }).Fund;
        //        }
        //        yield return shortcut;
        //    }
        //}

        public bool InsertVoucher(Voucher item)
        {
            //if (item.ID.HasValue)
            //    if (m_Db.SelectVouchers(new Voucher {ID = item.ID}).Any())
            //        m_Db.DeltaItemsFrom(item.ID.Value);
            return m_Db.InsertVoucher(item);
        }

        //public bool InsertVoucher(Voucher item, IEnumerable<VoucherDetail> details)
        //{
        //    if (!InsertVoucher(item))
        //        return false;
        //    var flag = true;
        //    foreach (var detail in details)
        //    {
        //        detail.Item = item.ID;
        //        flag &= m_Db.InsertDetail(detail);
        //    }
        //    return flag;
        //}

        public bool InsertDetail(VoucherDetail entity) { return m_Db.InsertDetail(entity); }

        public int DeleteVouchers(Voucher entity)
        {
            //var count = 0;
            //foreach (var i in m_Db.SelectVouchers(entity))
            //{
            //    count += m_Db.DeleteItems(new Voucher {ID = i.ID});
            //    m_Db.SelectDetails(i);
            //}
            //return count;
            return m_Db.DeleteVouchers(entity);
        }

        public int DeleteDetails(VoucherDetail entity) { return m_Db.DeleteDetails(entity); }

        //public bool InsertFixedAsset(DbFixedAsset entity) { return m_Db.InsertFixedAsset(entity); }
        //public int DeleteFixedAssets(DbFixedAsset entity) { return m_Db.DeleteFixedAssets(entity); }

        //public bool InsertShortcut(DbShortcut entity) { return m_Db.InsertShortcuts(entity); }
        //public int DeleteShortcuts(DbShortcut entity) { return m_Db.DeleteShortcuts(new DbShortcut {ID = entity.ID}); }


        //public bool InsertTitle(DbTitle entity) { return m_Db.InsertTitle(entity); }
        //public int DeleteTitles(DbTitle entity) { return m_Db.DeleteTitles(entity); }

        public void Shrink(Voucher item)
        {
            //var lst = m_Db.SelectDetails(item)
            var lst = item.Details
                          .GroupBy(
                                   d => new VoucherDetail {Title = d.Title, Content = d.Content},
                                   (key, grp) =>
                                   {
                                       key.Item = item.ID;
                                       key.Fund =
                                           grp.Sum(
                                                   d =>
                                                   d.Fund);
                                       return key;
                                   });
            m_Db.DeleteDetails(item);
            foreach (var detail in lst)
                m_Db.InsertDetail(detail);
        }

        //public void GetFixedAssetDetail(DbFixedAsset entity, DateTime? dt = null)
        //{
        //    var fa = GetBalance(new DbTitle {ID = FixedAsset}, entity.ID, dt);
        //    var faD = GetBalance(new DbTitle {ID = FixedAssetDepreciation}, entity.ID, dt);
        //    var faI = GetBalance(new DbTitle {ID = FixedAssetImpairment}, entity.ID, dt);
        //    entity.DepreciatedValue1 = -faD;
        //    entity.DepreciatedValue2 = -faI;
        //    entity.XValue = fa + faD + faI;
        //}

        public void Depreciate()
        {
            //TODO: Depreciate
        }

        public void Carry()
        {
            //TODO: Carry
        }

        //public int GetIDFromDate(DateTime date) { return m_Db.GetIDFromDate(date); }
    }
}
