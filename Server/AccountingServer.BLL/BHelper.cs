using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AccountingServer.DAL;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    public class BHelper
    {
        public const decimal FullYearProfit = (decimal)4103.00;
        public const decimal FixedAsset = (decimal)1601.00;
        public const decimal FixedAssetDepreciation = (decimal)1602.00;
        public const decimal FixedAssetImpairment = (decimal)1603.00;
        public const decimal Cost = (decimal)5000.00;

        private IDbHelper m_Db;
        private string m_DbName;

        private Dictionary<decimal, string> m_Titles;

        public void Connect(string un, string pw)
        {
            m_Db = new SqlDbHelper(un, pw);
            m_DbName = un;
            GetTitles();
        }

        public void Disconnect()
        {
            m_Db.Dispose();
            m_Db = null;
        }

        public bool IsCarry(DbItem entity) { return m_Db.SelectDetails(entity).Any(d => d.Title == FullYearProfit); }

        public decimal IsBalanced(DbItem entity)
        {
            // ReSharper disable once PossibleInvalidOperationException
            return m_Db.SelectDetails(new DbDetail {Remark = entity.ID.ToString()}).Sum(d => d.Fund.Value);
        }

        public decimal GetBalance(decimal? title, string remark = null, DateTime? dt = null)
        {
            var lstx = m_Db.SelectItems(new DbItem());
            if (dt.HasValue)
                lstx = lstx.Where(i => i.DT <= dt);
            var lst = lstx.Select(i => i.ID).ToList();
            return m_Db.SelectDetails(new DbDetail {Title = title, Remark = remark})
                       .Where(d => lst.Contains(d.Item))
                       .Sum(d => d.Fund.Value);
        }

        public decimal GetBalance(DbTitle title, string remark = null, DateTime? dt = null)
        {
            title.Balance = GetBalance(title.ID, remark, dt);
            return title.Balance.Value;
        }

        public Dictionary<DateTime, decimal> GetDailyBalance(IEnumerable<decimal> titles, string remark,
                                                             DateTime startDate, DateTime endDate, int dir = 0)
        {
            var dic = new Dictionary<DateTime, decimal>();
            var balance = (decimal)0;
            var dt = startDate;
            var lst = titles.SelectMany(title => m_Db.GetDailyBalance(title, remark, dir)).ToList();
            lst.Sort(
                     (d1, d2) =>
                     d1.DT.HasValue && d2.DT.HasValue
                         ? d1.DT.Value.CompareTo(d2.DT.Value)
                         : d1.DT.HasValue ? 1 : d2.DT.HasValue ? -1 : 0);
            foreach (var daily in lst)
            {
                if (!daily.DT.HasValue ||
                    daily.DT < startDate)
                {
                    balance += daily.Balance;
                    continue;
                }
                if (daily.DT > endDate)
                    break;
                while (dt < daily.DT.Value)
                {
                    dic.Add(dt, balance);
                    dt = dt.AddDays(1);
                }
                balance += daily.Balance;
            }
            while (dt <= endDate)
            {
                dic.Add(dt, balance);
                dt = dt.AddDays(1);
            }
            dic.Add(dt, balance);
            return dic;
        }

        public decimal GetABalance(DbTitle entity)
        {
            entity.ABalance = GetBalance(entity);
            foreach (var t in m_Db.SelectTitles(new DbTitle {H_ID = entity.ID}))
                entity.ABalance += GetABalance(t);
            return entity.ABalance.Value;
        }

        private decimal GetABalance(DbTitle entity, ref IList<DbTitle> titles)
        {
            entity.ABalance = GetBalance(entity);
            foreach (var t in m_Db.SelectTitles(new DbTitle {H_ID = entity.ID}))
                entity.ABalance += GetABalance(t, ref titles);
            return entity.ABalance.Value;
        }

        public decimal GetABalance(DbTitle entity, out List<DbTitle> titles, bool cascadeReturn)
        {
            entity.ABalance = GetBalance(entity);
            IList<DbTitle> tl = new List<DbTitle>();
            foreach (var t in m_Db.SelectTitles(new DbTitle {H_ID = entity.ID}))
            {
                tl.Add(t);
                if (cascadeReturn)
                    entity.ABalance += GetABalance(t, ref tl);
                else
                    entity.ABalance += GetABalance(t);
            }
            titles = tl as List<DbTitle>;
            return entity.ABalance.Value;
        }

        public IEnumerable<DbDetail> GetXBalances(DbTitle entity = null, bool withZero = false, bool noCarry = false, int? sID = null, int? eID = null)
        {
            var detail = entity == null ? new DbDetail() : new DbDetail {Title = entity.ID};
            var lst = m_Db.GetXBalances(detail, noCarry, sID, eID);
            return withZero ? lst : lst.Where(d => d.Fund != 0);
        }

        public IEnumerable<DbDetail> GetXBalances(decimal? title = null, bool withZero = false, bool noCarry = false, int? sID = null, int? eID = null)
        {
            var detail = title == null ? new DbDetail() : new DbDetail {Title = title};
            var lst = m_Db.GetXBalances(detail, noCarry, sID, eID);
            return withZero ? lst : lst.Where(d => d.Fund != 0);
        }

        public IEnumerable<DbDetail> GetXBalancesD(decimal? title = null, int dir = 0, bool noCarry = false,
                                                   int? sID = null, int? eID = null)
        {
            var detail = title == null ? new DbDetail() : new DbDetail { Title = title };
            var lst = m_Db.GetXBalances(detail, noCarry, sID, eID, dir);
            return lst;
        }

        public DbDetail GetXBalances(DbDetail entity, bool noCarry = false, int? sID = null, int? eID = null)
        {
            var lst = m_Db.GetXBalances(entity, noCarry, sID, eID);
            return lst.Single();
        }

        private void GetTitles()
        {
            m_Titles = new Dictionary<decimal, string>();
            foreach (var title in m_Db.SelectTitles(new DbTitle()))
                if (title.ID != null)
                    m_Titles.Add(title.ID.Value, title.Name);
        }

        public string GetTitleName(decimal? id)
        {
            if (id.HasValue)
                if (m_Titles.ContainsKey(id.Value))
                    return m_Titles[id.Value];
            return null;
        }
        public string GetFixedAssetName(Guid id)
        {
            return m_Db.GetFixedAssetName(id);
        }

        public decimal Carry(DateTime? date)
        {
            var dt = date ?? DateTime.Now.Date;
            var cnt = (decimal)0;
            // ReSharper disable once PossibleInvalidOperationException
            var lst = m_Db.SelectTitles(new DbTitle()).Where(t => IsToCarry(t.ID.Value));
            var titles = lst as IList<DbTitle> ?? lst.ToList();
            if (!titles.Any())
                return 0;
            var item = new DbItem {DT = dt};
            foreach (var t in titles)
            {
                var b = GetBalance(t, null, dt);
                m_Db.InsertDetail(new DbDetail {Item = item.ID, Title = t.ID, Fund = -b});
                cnt += b;
            }
            m_Db.InsertDetail(new DbDetail {Item = item.ID, Title = FullYearProfit, Fund = cnt});
            return cnt;
        }

        private static bool IsToCarry(decimal id) { return id >= Cost; }

        public IEnumerable<DbTitle> SelectTitles(DbTitle entity) { return m_Db.SelectTitles(entity); }
        public int SelectItemsCount(DbItem entity) { return m_Db.SelectItemsCount(entity); }
        public DbItem SelectItem(int id) { return m_Db.SelectItems(new DbItem {ID = id}).Single(); }
        public IEnumerable<DbItem> SelectItems(DbItem entity) { return m_Db.SelectItems(entity); }
        public IEnumerable<DbDetail> SelectDetails(DbItem entity) { return m_Db.SelectDetails(entity); }
        public IEnumerable<DbDetail> SelectDetails(DbDetail entity) { return m_Db.SelectDetails(entity); }
        public int SelectDetailsCount(DbDetail entity) { return m_Db.SelectDetailsCount(entity); }

        public IEnumerable<DbFixedAsset> SelectFixedAssets(DbFixedAsset entity)
        {
            return m_Db.SelectFixedAssets(entity);
        }

        public IEnumerable<DbShortcut> SelectShortcuts()
        {
            foreach (var shortcut in m_Db.SelectShortcuts(new DbShortcut()))
            {
                var xpath = Regex.Split(shortcut.Path, @",(?=(?:[^']*'[^']*')*[^']*$)");
                shortcut.Balance = 0;
                foreach (var path in xpath)
                {
                    var paths = path.Split(new[] {'/'});
                    if (paths.Last().StartsWith("T"))
                        shortcut.Balance += GetABalance(new DbTitle {ID = Convert.ToDecimal(paths.Last().Substring(1))});
                    if (paths.Last().StartsWith("R"))
                        shortcut.Balance +=
                            GetXBalances(
                                         new DbDetail
                                             {
                                                 Title = Convert.ToDecimal(paths[paths.Length - 2].Substring(1)),
                                                 Remark = paths.Last().Substring(2, paths.Last().Length - 3)
                                             }).Fund;
                }
                yield return shortcut;
            }
        }

        public bool InsertItem(DbItem item)
        {
            if (item.ID.HasValue)
                if (m_Db.SelectItems(new DbItem {ID = item.ID}).Any())
                    m_Db.DeltaItemsFrom(item.ID.Value);
            return m_Db.InsertItem(item);
        }

        public bool InsertItem(DbItem item, IEnumerable<DbDetail> details)
        {
            if (!InsertItem(item))
                return false;
            var flag = true;
            foreach (var detail in details)
            {
                detail.Item = item.ID;
                flag &= m_Db.InsertDetail(detail);
            }
            return flag;
        }

        public bool InsertDetail(DbDetail entity) { return m_Db.InsertDetail(entity); }

        public int DeleteItems(DbItem entity)
        {
            var count = 0;
            foreach (var i in m_Db.SelectItems(entity))
            {
                count += m_Db.DeleteItems(new DbItem {ID = i.ID});
                m_Db.SelectDetails(i);
            }
            return count;
        }

        public int DeleteDetails(DbDetail entity) { return m_Db.DeleteDetails(entity); }

        public bool InsertFixedAsset(DbFixedAsset entity) { return m_Db.InsertFixedAsset(entity); }
        public int DeleteFixedAssets(DbFixedAsset entity) { return m_Db.DeleteFixedAssets(entity); }

        public bool InsertShortcut(DbShortcut entity) { return m_Db.InsertShortcuts(entity); }
        public int DeleteShortcuts(DbShortcut entity) { return m_Db.DeleteShortcuts(new DbShortcut {ID = entity.ID}); }


        public bool InsertTitle(DbTitle entity) { return m_Db.InsertTitle(entity); }
        public int DeleteTitles(DbTitle entity) { return m_Db.DeleteTitles(entity); }

        public void Shrink(DbItem item)
        {
            var lst = m_Db.SelectDetails(item)
                          .GroupBy(
                                   d => new DbDetail {Title = d.Title, Remark = d.Remark},
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

        public void GetFixedAssetDetail(DbFixedAsset entity, DateTime? dt = null)
        {
            var fa = GetBalance(new DbTitle {ID = FixedAsset}, entity.ID, dt);
            var faD = GetBalance(new DbTitle {ID = FixedAssetDepreciation}, entity.ID, dt);
            var faI = GetBalance(new DbTitle {ID = FixedAssetImpairment}, entity.ID, dt);
            entity.DepreciatedValue1 = -faD;
            entity.DepreciatedValue2 = -faI;
            entity.XValue = fa + faD + faI;
        }

        public void Depreciate() { m_Db.Depreciate(); }
        public void Carry() { m_Db.Carry(); }

        public int GetIDFromDate(DateTime date) { return m_Db.GetIDFromDate(date); }
    }
}
