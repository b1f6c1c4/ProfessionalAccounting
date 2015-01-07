using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    public partial class Accountant
    {
        /// <summary>
        ///     按日期、科目、内容计算余额
        /// </summary>
        /// <example>
        ///     <list type="bullet">
        ///         <item>2014-01-01 借：餐费-A餐厅 100元</item>
        ///         <item>2014-01-02 借：餐费-B餐厅 200元</item>
        ///         <item>2014-01-05 借：餐费 500元</item>
        ///     </list>
        ///     <code>
        ///         GetBalance(new Balance { Title = 6602, SubTitle = 03, Content = "A餐厅", Date = DateTime.Parse("2014-01-05") }); // 100.0
        ///         GetBalance(new Balance { Title = 6602, SubTitle = 03, Content = "", Date = DateTime.Parse("2014-01-05") }); // 500.0
        ///         GetBalance(new Balance { Title = 6602, SubTitle = 03, Content = null, Date = DateTime.Parse("2014-01-02") }); // 300.0
        ///     </code>
        /// </example>
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
            var res = m_Db.FilteredSelect(dFilter, DateFilter.Unconstrained);
            if (filter.Date.HasValue)
                res = res.Where(v => v.Date <= filter.Date);
            filter.Fund = res.SelectMany(v => v.Details).Where(d => d.IsMatch(dFilter)).Sum(d => d.Fund.Value);
            return filter.Fund;
        }

        /// <summary>
        ///     按科目、内容计算余额
        /// </summary>
        /// <example>
        ///     <list type="bullet">
        ///         <item>2014-01-01 借：餐费-A餐厅 100元</item>
        ///         <item>2014-01-02 借：餐费-B餐厅 200元</item>
        ///         <item>2014-01-05 借：餐费 500元</item>
        ///     </list>
        ///     <code>
        ///         GetFinalBalance(new Balance { Title = 6602, SubTitle = 03, Content = "A餐厅" }); // 100.0
        ///         GetFinalBalance(new Balance { Title = 6602, SubTitle = 03, Content = "" }); // 500.0
        ///         GetFinalBalance(new Balance { Title = 6602, SubTitle = 03, Content = null }); // 800.0
        ///     </code>
        /// </example>
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
            filter.Fund = m_Db.FilteredSelectDetails(dFilter, DateFilter.Unconstrained).Sum(d => d.Fund.Value);
            return filter.Fund;
        }

        /// <summary>
        ///     按科目、内容计算每日金额
        /// </summary>
        /// <example>
        ///     <list type="bullet">
        ///         <item>2014-01-01 借：餐费-A餐厅 100元</item>
        ///         <item>2014-01-02 借：餐费-B餐厅 200元</item>
        ///         <item>2014-01-05 借：餐费 500元</item>
        ///     </list>
        ///     <code>
        ///         GetBalances(new Balance { Title = 6602, SubTitle = 03, Content = "A餐厅" });
        ///         // 2014-01-01 : 100
        ///         GetBalances(new Balance { Title = 6602, SubTitle = 03, Content = null });
        ///         // 2014-01-01 : 100
        ///         // 2014-01-02 : 200
        ///         // 2014-01-05 : 500
        ///     </code>
        /// </example>
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
            return m_Db.FilteredSelect(dFilter, DateFilter.Unconstrained)
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
                                              .Sum(d => d.Fund.Value)
                                    });
        }


        /// <summary>
        ///     累加每日金额得到每日余额
        /// </summary>
        /// <param name="resx">每日金额</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>每日余额</returns>
        public static IEnumerable<Balance> ProcessDailyBalance(IReadOnlyList<Balance> resx, DateFilter rng)
        {
            var id = 0;
            DateTime dt;
            if (rng.StartDate.HasValue)
                dt = rng.StartDate.Value;
            else if (resx.Any(b => b.Date.HasValue))
                // ReSharper disable once PossibleInvalidOperationException
                dt = resx.First(b => b.Date.HasValue).Date.Value;
            else
            {
                if (resx.Any())
                    yield return new Balance { Fund = resx.Sum(b => b.Fund) };
                yield break;
            }

            // ReSharper disable once PossibleInvalidOperationException
            var last = rng.EndDate ?? resx.Last().Date.Value;

            var fund = 0D;
            for (; dt <= last; dt = dt.AddDays(1))
            {
                while (id < resx.Count &&
                       DateHelper.CompareDate(resx[id].Date, dt) <= 0)
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
        ///     有多个栏目的情况下，累加每日金额得到每日余额
        /// </summary>
        /// <param name="resx">日期分离的每日金额</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>每日余额</returns>
        public static IEnumerable<IEnumerable<Balance>> ProcessDailyBalance(
            IReadOnlyList<Tuple<DateTime?, IEnumerable<Balance>>> resx,
            DateFilter rng)
        {
            var id = 0;
            DateTime dt;
            if (rng.StartDate.HasValue)
                dt = rng.StartDate.Value;
            else if (resx.Any(t => t.Item1.HasValue))
                // ReSharper disable once PossibleInvalidOperationException
                dt = resx.First(t => t.Item1.HasValue).Item1.Value;
            else
            {
                if (resx.Any())
                    yield return resx[0].Item2;
                yield break;
            }

            // ReSharper disable once PossibleInvalidOperationException
            var last = rng.EndDate ?? resx.Last().Item1.Value;

            var fund = new Dictionary<Balance, double>(new BalanceEqualityComparer());
            for (; dt <= last; dt = dt.AddDays(1))
            {
                while (id < resx.Count &&
                       DateHelper.CompareDate(resx[id].Item1, dt) <= 0)
                {
                    foreach (var balance in resx[id].Item2)
                        if (fund.ContainsKey(balance))
                            fund[balance] += balance.Fund;
                        else
                            fund[balance] = balance.Fund;
                    id++;
                }

                var copiedDt = dt;
                yield return
                    fund.AsEnumerable()
                        .Select(
                                kvp =>
                                new Balance
                                    {
                                        Date = copiedDt,
                                        Title = kvp.Key.Title,
                                        SubTitle = kvp.Key.SubTitle,
                                        Content = kvp.Key.Content,
                                        Fund = kvp.Value
                                    });
            }
        }

        /// <summary>
        ///     累加每日金额得到变动日余额
        /// </summary>
        /// <param name="resx">每日金额</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>变动日余额</returns>
        public static IEnumerable<Balance> ProcessAccumulatedBalance(IReadOnlyList<Balance> resx, DateFilter rng)
        {
            var id = 0;
            var fund = 0D;
            DateTime dt;
            if (rng.StartDate.HasValue)
                dt = rng.StartDate.Value;
            else if (resx.Any(b => b.Date.HasValue))
                // ReSharper disable once PossibleInvalidOperationException
                dt = resx.First(b => b.Date.HasValue).Date.Value;
            else
            {
                if (resx.Any())
                    yield return new Balance { Fund = resx.Sum(b => b.Fund) };
                yield break;
            }

            // ReSharper disable once PossibleInvalidOperationException
            var last = rng.EndDate ?? resx.Last().Date.Value;

            while (id < resx.Count &&
                   DateHelper.CompareDate(resx[id].Date, dt) < 0)
                fund += resx[id++].Fund;

            for (; id < resx.Count && resx[id].Date <= last; id++)
            {
                fund += resx[id].Fund;
                yield return
                    new Balance
                        {
                            Date = resx[id].Date,
                            Fund = fund
                        };
            }
        }

        /// <summary>
        ///     按科目、内容计算每日余额
        /// </summary>
        /// <example>
        ///     <list type="bullet">
        ///         <item>2014-01-01 借：餐费-A餐厅 100元</item>
        ///         <item>2014-01-02 借：餐费-B餐厅 200元</item>
        ///         <item>2014-01-05 借：餐费 500元</item>
        ///     </list>
        ///     <code>
        ///         GetDailyBalance(new Balance { Title = 6602, SubTitle = 03, Content = "A餐厅" }, null, null);
        ///         // 2014-01-01 : 100
        ///         GetDailyBalance(new Balance { Title = 6602, SubTitle = 03, Content = null }, null, null);
        ///         // 2014-01-01 : 100
        ///         // 2014-01-02 : 300
        ///         // 2014-01-03 : 300
        ///         // 2014-01-04 : 300
        ///         // 2014-01-05 : 800
        ///         GetDailyBalance(new Balance { Title = 6602, SubTitle = 03, Content = null }, null, null, true);
        ///         // 2014-01-01 : 100
        ///         // 2014-01-02 : 300
        ///         // 2014-01-05 : 800
        ///     </code>
        /// </example>
        /// <param name="filter">过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="dir">0表示同时考虑借贷方，大于0表示只考虑借方，小于0表示只考虑贷方</param>
        /// <param name="aggr">是否每变动日累加而非每日求余额</param>
        /// <returns>每日余额，借方为正，贷方为负</returns>
        public IEnumerable<Balance> GetDailyBalance(Balance filter, DateFilter rng, int dir = 0, bool aggr = false)
        {
            var dFilter = new VoucherDetail
                              {
                                  Title = filter.Title,
                                  SubTitle = filter.SubTitle,
                                  Content = filter.Content
                              };

            var res = m_Db.FilteredSelect(dFilter, new DateFilter(null, rng.EndDate));
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
                                                 .Sum(d => d.Fund.Value)
                                       }).ToList();
            resx.Sort(new BalanceComparer());
            return aggr
                       ? ProcessAccumulatedBalance(resx, rng)
                       : ProcessDailyBalance(resx, rng);
        }

        /// <summary>
        ///     按科目、内容计算每日总余额
        /// </summary>
        /// <example>
        ///     <list type="bullet">
        ///         <item>2014-01-01 借：餐费-A餐厅 100元</item>
        ///         <item>2014-01-02 借：餐费-B餐厅 200元</item>
        ///         <item>2014-01-05 借：餐费 500元</item>
        ///     </list>
        ///     <code>
        ///         GetDailyBalance(new [] { new Balance { Title = 6602, SubTitle = 03, Content = "A餐厅" }
        ///                                  new Balance { Title = 6602, SubTitle = 03, Content = "" } }, null, null);
        ///         // 2014-01-01 : 100
        ///         // 2014-01-02 : 100
        ///         // 2014-01-03 : 100
        ///         // 2014-01-04 : 100
        ///         // 2014-01-05 : 600
        ///     </code>
        /// </example>
        /// <param name="filters">过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="dir">0表示同时考虑借贷方，大于0表示只考虑借方，小于0表示只考虑贷方</param>
        /// <returns>每日总余额，借方为正，贷方为负</returns>
        public IEnumerable<Balance> GetDailyBalance(IEnumerable<Balance> filters, DateFilter rng, int dir = 0)
        {
            var dFilters =
                filters.Select(
                               filter =>
                               new VoucherDetail
                                   {
                                       Title = filter.Title,
                                       SubTitle = filter.SubTitle,
                                       Content = filter.Content
                                   }).ToArray();

            var res = m_Db.FilteredSelect(dFilters, new DateFilter(null, rng.EndDate));
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
                                                 .Sum(d => d.Fund.Value)
                                       }).ToList();
            resx.Sort(new BalanceComparer());
            return ProcessDailyBalance(resx, rng);
        }

        /// <summary>
        ///     按日期、科目计算各内容金额
        /// </summary>
        /// <example>
        ///     <list type="bullet">
        ///         <item>2014-01-01 借：餐费-A餐厅 100元</item>
        ///         <item>2014-01-02 借：餐费-B餐厅 200元</item>
        ///         <item>2014-01-05 借：餐费 500元</item>
        ///     </list>
        ///     <code>
        ///         GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 03 });
        ///         // 2014-01-01 : 
        ///         //              A餐厅 : 100
        ///         // 2014-01-02 : 
        ///         //              B餐厅 : 100
        ///         // 2014-01-05 : 
        ///         //              null : 500
        ///     </code>
        /// </example>
        /// <param name="filter">过滤器</param>
        /// <returns>各内容每日金额，借方为正，贷方为负</returns>
        public IEnumerable<IEnumerable<Balance>> GetBalancesAcrossContent(Balance filter)
        {
            var dFilter = new VoucherDetail { Title = filter.Title, SubTitle = filter.SubTitle };
            return m_Db.FilteredSelect(dFilter, DateFilter.Unconstrained)
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
                                                   Fund = ds.Sum(d => d.Fund.Value)
                                               }));
        }

        /// <summary>
        ///     按日期、过滤器计算各内容金额
        /// </summary>
        /// <example>
        ///     <list type="bullet">
        ///         <item>2014-01-01 借：餐费-A餐厅 100元</item>
        ///         <item>2014-01-02 借：餐费-B餐厅 200元</item>
        ///         <item>2014-01-05 借：餐费 500元</item>
        ///     </list>
        ///     <code>
        ///         GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 03 }, "2014-01-02", "2014-01-05");
        ///         // B餐厅 : 200
        ///         // null : 500
        ///     </code>
        /// </example>
        /// <param name="filter">过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>各内容金额</returns>
        public IEnumerable<Balance> GetBalancesAcrossContent(Balance filter, DateFilter rng)
        {
            var dFilter = new VoucherDetail { Title = filter.Title, SubTitle = filter.SubTitle };
            return m_Db.FilteredSelectDetails(dFilter, rng)
                       .GroupBy(
                                d => d.Content,
                                (c, ds) =>
                                new Balance
                                    {
                                        Title = filter.Title,
                                        SubTitle = filter.SubTitle,
                                        Content = c,
                                        Fund = ds.Sum(d => d.Fund.Value)
                                    });
        }

        /// <summary>
        ///     按科目计算各内容余额
        /// </summary>
        /// <example>
        ///     <list type="bullet">
        ///         <item>2014-01-01 借：餐费-A餐厅 100元</item>
        ///         <item>2014-01-02 借：餐费-B餐厅 200元</item>
        ///         <item>2014-01-05 借：餐费 500元</item>
        ///     </list>
        ///     <code>
        ///         GetFinalBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 03 });
        ///         // A餐厅 : 100
        ///         // B餐厅 : 200
        ///         // null : 500
        ///     </code>
        /// </example>
        /// <param name="filter">过滤器</param>
        /// <returns>各内容余额，借方为正，贷方为负</returns>
        public IEnumerable<Balance> GetFinalBalancesAcrossContent(Balance filter)
        {
            var dFilter = new VoucherDetail { Title = filter.Title, SubTitle = filter.SubTitle };
            return m_Db.FilteredSelectDetails(dFilter, DateFilter.Unconstrained)
                       .GroupBy(
                                d => d.Content,
                                (c, ds) =>
                                new Balance
                                    {
                                        Title = filter.Title,
                                        SubTitle = filter.SubTitle,
                                        Content = c,
                                        Fund = ds.Sum(d => d.Fund.Value)
                                    });
        }
    }
}
