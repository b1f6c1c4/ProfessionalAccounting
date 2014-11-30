using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using AccountingServer.DAL;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    /// <summary>
    /// 基本会计业务处理类
    /// </summary>
    public class Accountant
    {
        /// <summary>
        /// 本年利润
        /// </summary>
        public const int FullYearProfit = 4103;
        /// <summary>
        /// 费用
        /// </summary>
        public const int Cost = 5000;

        /// <summary>
        /// 数据库访问
        /// </summary>
        private IDbHelper m_Db;

        private IDbHelper m_OldDb;

        /// <summary>
        /// 连接数据库
        /// </summary>
        /// <param name="un">用户名</param>
        /// <param name="pw">密码</param>
        public void Connect(string un, string pw)
        {
            m_Db = new MongoDbHelper();

            m_OldDb = new SqlDbHelper(un, pw);
        }

        /// <summary>
        /// 断开数据库连接
        /// </summary>
        public void Disconnect()
        {
            m_Db.Dispose();
            m_Db = null;

            m_OldDb.Dispose();
            m_OldDb = null;
        }

        public void CopyDb()
        {
            m_Db.DeleteVouchers(new Voucher { Date = DateTime.Parse("2013-09-30") });
            m_Db.InsertVoucher(
                               new Voucher
                                   {
                                       Date = null,
                                       Type = VoucherType.Ordinal,
                                       Details = m_OldDb.SelectDetails(new VoucherDetail { Remark = "1" })
                                                        .Where(d => d.Title != 4001)
                                                        .Concat(
                                                                m_OldDb.SelectDetails(
                                                                                      new VoucherDetail { Remark = "4" }))
                                                        .Concat(
                                                                m_OldDb.SelectDetails(
                                                                                      new VoucherDetail { Remark = "5" }))
                                                        .Concat(
                                                                m_OldDb.SelectDetails(
                                                                                      new VoucherDetail { Remark = "6" }))
                                                        .Concat(
                                                                new[]
                                                                    {
                                                                        new VoucherDetail
                                                                            {
                                                                                Title = 4101,
                                                                                Fund = 33422.3724
                                                                            }
                                                                    })
                                                        .GroupBy(
                                                                 d =>
                                                                 new VoucherDetail
                                                                     {
                                                                         Title = d.Title,
                                                                         SubTitle = d.SubTitle,
                                                                         Content = d.Content
                                                                     },
                                                                 (vd, ds) =>
                                                                 new VoucherDetail
                                                                     {
                                                                         Title = vd.Title,
                                                                         SubTitle = vd.SubTitle,
                                                                         Content = vd.Content,
                                                                         Fund = ds.Sum(d => d.Fund)
                                                                     })
                                                        .ToArray()
                                   });
        }

        /// <summary>
        /// 返回编号对应的会计科目名称
        /// </summary>
        /// <param name="title">一级科目编号</param>
        /// <param name="subtitle">二级科目编号</param>
        /// <returns>名称</returns>
        public static string GetTitleName(int? title, int? subtitle = null)
        {
            var mgr = new ResourceManager("AccountingServer.BLL.AccountTitle", Assembly.GetExecutingAssembly());
            return mgr.GetString(String.Format("T{0:0000}{1:00}", title, subtitle));
        }

        /// <summary>
        /// 检查记账凭证借贷方数额是否相等
        /// </summary>
        /// <param name="entity">记账凭证</param>
        /// <returns>借方比贷方多出数</returns>
        public double IsBalanced(Voucher entity)
        {
            // ReSharper disable once PossibleInvalidOperationException
            return entity.Details.Sum(d => d.Fund.Value);
        }

        /// <summary>
        /// 按日期、科目、内容计算余额
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>余额，借方为正，贷方为负</returns>
        public double GetBalance(Balance filter)
        {
            var dFilter = new VoucherDetail
                              {
                                  Title = filter.Title,
                                  SubTitle = filter.SubTitle,
                                  Content = filter.Content
                              };
            var res = m_Db.SelectVouchersWithDetail(dFilter);
            if (filter.Date.HasValue)
                res = res.Where(v => v.Date <= filter.Date);
            filter.Fund = res.SelectMany(v => v.Details).Where(d => d.IsMatch(dFilter)).Sum(d => d.Fund.Value);
            return filter.Fund;
        }

        /// <summary>
        /// 按科目、内容计算余额
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>余额，借方为正，贷方为负</returns>
        public double GetFinalBalance(Balance filter)
        {
            var dFilter = new VoucherDetail
                              {
                                  Title = filter.Title,
                                  SubTitle = filter.SubTitle,
                                  Content = filter.Content
                              };
            filter.Fund = m_Db.SelectDetails(dFilter).Sum(d => d.Fund.Value);
            return filter.Fund;
        }

        /// <summary>
        /// 按科目、内容计算每日金额
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>每日金额，借方为正，贷方为负</returns>
        public IEnumerable<Balance> GetBalances(Balance filter)
        {
            var dFilter = new VoucherDetail
                              {
                                  Title = filter.Title,
                                  SubTitle = filter.SubTitle,
                                  Content = filter.Content
                              };
            return m_Db.SelectVouchersWithDetail(dFilter)
                       .GroupBy(
                                v => v.Date,
                                (dt, vs) =>
                                new Balance
                                    {
                                        Date = dt,
                                        Title = filter.Title,
                                        SubTitle = filter.SubTitle,
                                        Content = filter.Content,
                                        Fund =
                                            vs.SelectMany(v => v.Details)
                                              .Where(d => d.IsMatch(dFilter))
                                              .Sum(d => d.Fund)
                                              .Value
                                    });
        }

        /// <summary>
        /// 比较两日期（可以为无日期）的早晚
        /// </summary>
        /// <param name="b1Date">第一个日期</param>
        /// <param name="b2Date">第二个日期</param>
        /// <returns>相等为0，第一个早为-1，第二个早为1（无日期按无穷长时间以前考虑）</returns>
        private static int CompareDate(DateTime? b1Date, DateTime? b2Date)
        {
            if (b1Date.HasValue &&
                b2Date.HasValue)
                return b1Date.Value.CompareTo(b2Date.Value);
            if (b1Date.HasValue)
                return 1;
            if (b2Date.HasValue)
                return -1;
            return 0;
        }

        /// <summary>
        /// 累加每日金额得到每日余额
        /// </summary>
        /// <param name="startDate">开始累加的日期</param>
        /// <param name="endDate">停止累加的日期</param>
        /// <param name="resx">每日金额</param>
        /// <returns>每日余额</returns>
        private static IEnumerable<Balance> ProcessDailyBalance(DateTime startDate, DateTime endDate, IReadOnlyList<Balance> resx)
        {
            var id = 0;
            var fund = 0D;
            for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
            {
                while (id < resx.Count &&
                       CompareDate(resx[id].Date, dt) < 0)
                    fund += resx[id++].Fund;
                if (id < resx.Count)
                    fund += resx[id++].Fund;

                yield return
                    new Balance
                        {
                            Date = dt,
                            Fund = fund
                        };
            }
        }

        /// <summary>
        /// 按科目、内容计算每日余额
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">截止日期</param>
        /// <param name="dir">0表示同时考虑借贷方，大于0表示只考虑借方，小于0表示只考虑贷方</param>
        /// <returns>每日余额，借方为正，贷方为负</returns>
        public IEnumerable<Balance> GetDailyBalance(Balance filter, DateTime startDate, DateTime endDate, int dir = 0)
        {
            var dFilter = new VoucherDetail
                              {
                                  Title = filter.Title,
                                  SubTitle = filter.SubTitle,
                                  Content = filter.Content
                              };
            var res = m_Db.SelectVouchersWithDetail(dFilter).Where(v => CompareDate(v.Date, endDate) <= 0);
            var resx = res.GroupBy(
                                   v => v.Date,
                                   (dt, vs) =>
                                   new Balance
                                       {
                                           Date = dt,
                                           //Title = filter.Title,
                                           //SubTitle = filter.SubTitle,
                                           //Content = filter.Content,
                                           Fund =
                                               vs.SelectMany(v => v.Details)
                                                 .Where(d => d.IsMatch(dFilter))
                                                 .Where(d => dir == 0 || (dir > 0 ? d.Fund > 0 : d.Fund < 0))
                                                 .Sum(d => d.Fund)
                                                 .Value
                                       }).ToList();
            resx.Sort((b1, b2) => CompareDate(b1.Date, b2.Date));
            return ProcessDailyBalance(startDate, endDate, resx);
        }

        /// <summary>
        /// 按科目、内容计算每日总余额
        /// </summary>
        /// <param name="filters">过滤器</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">截止日期</param>
        /// <param name="dir">0表示同时考虑借贷方，大于0表示只考虑借方，小于0表示只考虑贷方</param>
        /// <returns>每日总余额，借方为正，贷方为负</returns>
        public IEnumerable<Balance> GetDailyBalance(IEnumerable<Balance> filters, DateTime startDate, DateTime endDate,
                                                    int dir = 0)
        {
            var dFilters =
                filters.Select(
                               filter =>
                               new VoucherDetail
                                   {
                                       Title = filter.Title,
                                       SubTitle = filter.SubTitle,
                                       Content = filter.Content
                                   });

            var res = m_Db.SelectVouchersWithDetail(dFilters).Where(v => CompareDate(v.Date, endDate) <= 0);
            var resx = res.GroupBy(
                                   v => v.Date,
                                   (dt, vs) =>
                                   new Balance
                                       {
                                           Date = dt,
                                           //Title = filter.Title,
                                           //SubTitle = filter.SubTitle,
                                           //Content = filter.Content,
                                           Fund =
                                               vs.SelectMany(v => v.Details)
                                                 .Where(d => dFilters.Any(d.IsMatch))
                                                 .Where(d => dir == 0 || (dir > 0 ? d.Fund > 0 : d.Fund < 0))
                                                 .Sum(d => d.Fund)
                                                 .Value
                                       }).ToList();
            resx.Sort((b1, b2) => CompareDate(b1.Date, b2.Date));
            return ProcessDailyBalance(startDate, endDate, resx);
        }

        /// <summary>
        /// 按日期、科目计算各内容金额
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>各内容每日金额，借方为正，贷方为负</returns>
        public IEnumerable<IEnumerable<Balance>> GetBalancesAcrossContent(Balance filter)
        {
            var dFilter = new VoucherDetail { Title = filter.Title, SubTitle = filter.SubTitle };
            return m_Db.SelectVouchersWithDetail(dFilter)
                       .GroupBy(
                                v => v.Date,
                                (dt, vs) =>
                                vs.SelectMany(v => v.Details)
                                  .Where(d => d.IsMatch(dFilter))
                                  .GroupBy(
                                           d => d.Content,
                                           (c, ds) =>
                                           new Balance
                                               {
                                                   Date = dt,
                                                   Title = filter.Title,
                                                   SubTitle = filter.SubTitle,
                                                   Content = c,
                                                   Fund = ds.Sum(d => d.Fund).Value
                                               }));
        }

        /// <summary>
        /// 按科目计算各内容余额
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>各内容余额，借方为正，贷方为负</returns>
        public IEnumerable<Balance> GetFinalBalancesAcrossContent(Balance filter)
        {
            var dFilter = new VoucherDetail { Title = filter.Title, SubTitle = filter.SubTitle };
            return m_Db.SelectDetails(dFilter)
                       .GroupBy(
                                d => d.Content,
                                (c, ds) =>
                                new Balance
                                    {
                                        Title = filter.Title,
                                        SubTitle = filter.SubTitle,
                                        Content = c,
                                        Fund = ds.Sum(d => d.Fund).Value
                                    });
        }

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

        /// <summary>
        /// 按过滤器查找记账凭证并记数
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>匹配过滤器的记账凭证总数</returns>
        public long SelectVouchersCount(Voucher filter) { return m_Db.SelectVouchersCount(filter); }
        /// <summary>
        /// 按编号查找记账凭证
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>记账凭证，如果没有则为null</returns>
        public Voucher SelectVoucher(string id) { return m_Db.SelectVoucher(id); }
        /// <summary>
        /// 按过滤器查找记账凭证
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>匹配过滤器的记账凭证</returns>
        public IEnumerable<Voucher> SelectVouchers(Voucher filter) { return m_Db.SelectVouchers(filter); }
        /// <summary>
        /// 按日期查找记账凭证
        /// <para>若<paramref name="startDate"/>和<paramref name="endDate"/>均为null，则返回所有无日期的记账凭证</para>
        /// </summary>
        /// <param name="startDate">开始日期，若为null表示不检查最小日期，无日期亦可</param>
        /// <param name="endDate">截止日期，若为null表示不检查最大日期</param>
        /// <returns>指定日期的记账凭证</returns>
        public IEnumerable<Voucher> SelectVouchers(DateTime? startDate, DateTime? endDate) { return m_Db.SelectVouchers(startDate, endDate); }

        /// <summary>
        /// 按细目过滤器查找记账凭证
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <returns>任一细目匹配过滤器的记账凭证</returns>
        public IEnumerable<Voucher> SelectVouchersWithDetail(VoucherDetail entity)
        {
            return m_Db.SelectVouchersWithDetail(entity);
        }

        /// <summary>
        /// 按细目过滤器查找细目
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <returns>匹配过滤器的细目</returns>
        public IEnumerable<VoucherDetail> SelectDetails(VoucherDetail filter) { return m_Db.SelectDetails(filter); }
        /// <summary>
        /// 按细目过滤器查找细目并记数
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <returns>匹配过滤器的细目总数</returns>
        public long SelectDetailsCount(VoucherDetail filter) { return m_Db.SelectDetailsCount(filter); }

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

        /// <summary>
        /// 添加记账凭证
        /// <para>若<paramref name="entity"/>没有指定编号，则添加成功后会自动给<paramref name="entity"/>添加编号</para>
        /// </summary>
        /// <param name="entity">记账凭证</param>
        /// <returns>是否成功</returns>
        public bool InsertVoucher(Voucher entity)
        {
            return m_Db.InsertVoucher(entity);
        }

        /// <summary>
        /// 按过滤器删除记账凭证
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>已删除的记账凭证总数</returns>
        public int DeleteVouchers(Voucher filter)
        {
            return m_Db.DeleteVouchers(filter);
        }

        /// <summary>
        /// 按过滤器删除细目
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <returns>已删除的细目总数</returns>
        public int DeleteDetails(VoucherDetail filter) { return m_Db.DeleteDetails(filter); }

        //public bool InsertFixedAsset(DbFixedAsset entity) { return m_Db.InsertFixedAsset(entity); }
        //public int DeleteFixedAssets(DbFixedAsset entity) { return m_Db.DeleteFixedAssets(entity); }

        //public bool InsertShortcut(DbShortcut entity) { return m_Db.InsertShortcuts(entity); }
        //public int DeleteShortcuts(DbShortcut entity) { return m_Db.DeleteShortcuts(new DbShortcut {ID = entity.ID}); }


        //public bool InsertTitle(DbTitle entity) { return m_Db.InsertTitle(entity); }
        //public int DeleteTitles(DbTitle entity) { return m_Db.DeleteTitles(entity); }

        /// <summary>
        /// 合并记账凭证上相同的细目
        /// </summary>
        /// <param name="voucher">记账凭证</param>
        public void Shrink(Voucher voucher)
        {
            voucher.Details = voucher.Details
                                     .GroupBy(
                                              d => new VoucherDetail { Title = d.Title, Content = d.Content },
                                              (key, grp) =>
                                              {
                                                  key.Item = voucher.ID;
                                                  key.Fund =
                                                      grp.Sum(
                                                              d =>
                                                              d.Fund);
                                                  return key;
                                              }).ToArray();
            m_Db.UpdateVoucher(voucher);
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

        /// <summary>
        /// 折旧
        /// </summary>
        public void Depreciate()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 期末结转
        /// </summary>
        public void Carry()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 摊销
        /// </summary>
        public void Amortization()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Balance> GetBalancesAcrossContent(Balance filter, DateTime? startDate, DateTime? endDate)
        {
            throw new NotImplementedException();
        }
    }
}
