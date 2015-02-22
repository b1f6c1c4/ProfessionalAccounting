using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    public partial class Accountant
    {
        /// <summary>
        ///     固定资产清理
        /// </summary>
        private const int DefaultDispositionTitle = 1606;

        /// <summary>
        ///     获取指定月的最后一天
        /// </summary>
        /// <param name="year">年</param>
        /// <param name="month">月</param>
        /// <returns>此月最后一天</returns>
        private static DateTime LastDayOfMonth(int year, int month)
        {
            while (month > 12)
            {
                month -= 12;
                year++;
            }
            while (month < 1)
            {
                month += 12;
                year--;
            }
            return new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
        }

        /// <summary>
        ///     获取资产的账面价值
        /// </summary>
        /// <param name="asset">资产</param>
        /// <param name="dt">日期，若为<c>null</c>则返回原值</param>
        /// <returns>指定日期的账面价值，若尚未购置则为<c>null</c></returns>
        public static double? GetBookValueOn(Asset asset, DateTime? dt)
        {
            if (!dt.HasValue ||
                asset.Schedule == null)
                return asset.Value;

            var last = asset.Schedule.LastOrDefault(item => DateHelper.CompareDate(item.Date, dt) <= 0);
            if (last != null)
                return last.BookValue;
            return null;
        }

        /// <summary>
        ///     获取摊销的待摊额
        /// </summary>
        /// <param name="amort">摊销</param>
        /// <param name="dt">日期，若为<c>null</c>则返回总额</param>
        /// <returns>指定日期的待摊额，若尚未开始则为<c>null</c></returns>
        public static double? GetBookValueOn(Amortization amort, DateTime? dt)
        {
            if (!dt.HasValue ||
                amort.Schedule == null)
                return amort.Value;

            var last = amort.Schedule.LastOrDefault(item => DateHelper.CompareDate(item.Date, dt) <= 0);
            if (last != null)
                return last.Residue;
            return null;
        }

        /// <summary>
        ///     调整资产计算表
        /// </summary>
        /// <param name="asset">资产</param>
        private static void InternalRegular(Asset asset)
        {
            if (asset.Remark == Asset.IgnoranceMark)
                return;
            if (!asset.Date.HasValue ||
                !asset.Value.HasValue)
                return;

            List<AssetItem> lst;
            if (asset.Schedule == null)
                lst = new List<AssetItem>();
            else if (asset.Schedule is List<AssetItem>)
                lst = asset.Schedule as List<AssetItem>;
            else
                lst = asset.Schedule.ToList();

            foreach (var assetItem in lst)
                if (assetItem is DepreciateItem ||
                    assetItem is DevalueItem)
                    if (assetItem.Date.HasValue)
                        assetItem.Date = LastDayOfMonth(assetItem.Date.Value.Year, assetItem.Date.Value.Month);

            lst.Sort(new AssetItemComparer());

            if (lst.Count == 0 ||
                !(lst[0] is AcquisationItem))
                lst.Insert(
                           0,
                           new AcquisationItem
                               {
                                   Date = asset.Date,
                                   OrigValue = asset.Value.Value
                               });
            else if (lst[0].Remark != AssetItem.IgnoranceMark)
            {
                (lst[0] as AcquisationItem).Date = asset.Date;
                (lst[0] as AcquisationItem).OrigValue = asset.Value.Value;
            }

            var bookValue = 0D;
            for (var i = 0; i < lst.Count; i++)
            {
                var item = lst[i];
                if (item is AcquisationItem)
                {
                    bookValue += (item as AcquisationItem).OrigValue;
                    item.BookValue = bookValue;
                }
                else if (item is DepreciateItem)
                {
                    bookValue -= (item as DepreciateItem).Amount;
                    item.BookValue = bookValue;
                }
                else if (item is DevalueItem)
                {
                    if (bookValue <= (item as DevalueItem).FairValue
                        &&
                        item.Remark != AssetItem.IgnoranceMark)
                    {
                        lst.RemoveAt(i--);
                        continue;
                    }
                    (item as DevalueItem).Amount = bookValue - (item as DevalueItem).FairValue;
                    bookValue = (item as DevalueItem).FairValue;
                    item.BookValue = (item as DevalueItem).FairValue;
                }
                else if (item is DispositionItem)
                    bookValue = 0;
            }

            asset.Schedule = lst;
        }

        /// <summary>
        ///     找出未在资产计算表中注册的记账凭证，并尝试建立引用
        /// </summary>
        /// <param name="asset">资产</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="query">检索式</param>
        /// <returns>未注册的记账凭证</returns>
        public IEnumerable<Voucher> RegisterVouchers(Asset asset, DateFilter rng,
                                                     IQueryCompunded<IVoucherQueryAtom> query)
        {
            if (asset.Remark == Asset.IgnoranceMark)
                yield break;

            {
                var filter = new VoucherDetail
                                 {
                                     Title = asset.Title,
                                     Content = asset.StringID
                                 };
                foreach (
                    var voucher in
                        m_Db.SelectVouchers(
                                            new VoucherQueryAryBase(
                                                OperatorType.Intersect,
                                                new[] { query, new VoucherQueryAtomBase(filter: filter) })))
                {
                    if (voucher.Remark == Asset.IgnoranceMark)
                        continue;

                    if (asset.Schedule.Any(item => item.VoucherID == voucher.ID))
                        continue;

                    // ReSharper disable once PossibleInvalidOperationException
                    var value = voucher.Details.Single(d => d.IsMatch(filter)).Fund.Value;

                    if (value > 0)
                    {
                        var lst = asset.Schedule.Where(item => item.Date.Within(rng))
                                       .Where(
                                              item =>
                                              item is AcquisationItem &&
                                              (!voucher.Date.HasValue || item.Date == voucher.Date) &&
                                              Math.Abs((item as AcquisationItem).OrigValue - value) < Tolerance)
                                       .ToList();

                        if (lst.Count == 1)
                            lst[0].VoucherID = voucher.ID;
                        else
                            yield return voucher;
                    }
                    else if (value < 0)
                    {
                        var lst = asset.Schedule.Where(item => item.Date.Within(rng))
                                       .Where(
                                              item =>
                                              item is DispositionItem &&
                                              (!voucher.Date.HasValue || item.Date == voucher.Date))
                                       .ToList();

                        if (lst.Count == 1)
                            lst[0].VoucherID = voucher.ID;
                        else
                            yield return voucher;
                    }
                    else
                        yield return voucher;
                }
            }
            {
                var filter = new VoucherDetail
                                 {
                                     Title = asset.DepreciationTitle,
                                     Content = asset.StringID
                                 };
                foreach (var voucher in m_Db.SelectVouchers(new VoucherQueryAtomBase(filter: filter)))
                {
                    if (voucher.Remark == Asset.IgnoranceMark)
                        continue;

                    if (asset.Schedule.Any(item => item.VoucherID == voucher.ID))
                        continue;

                    var lst = asset.Schedule.Where(
                                                   item =>
                                                   item is DepreciateItem &&
                                                   (!voucher.Date.HasValue || item.Date == voucher.Date))
                                   .ToList();

                    if (lst.Count == 1)
                        lst[0].VoucherID = voucher.ID;
                    else
                        yield return voucher;
                }
            }
            {
                var filter = new VoucherDetail
                                 {
                                     Title = asset.DevaluationTitle,
                                     Content = asset.StringID
                                 };
                foreach (var voucher in m_Db.SelectVouchers(new VoucherQueryAtomBase(filter: filter)))
                {
                    if (voucher.Remark == Asset.IgnoranceMark)
                        continue;

                    if (asset.Schedule.Any(item => item.VoucherID == voucher.ID))
                        continue;

                    var lst = asset.Schedule.Where(
                                                   item =>
                                                   item is DevalueItem &&
                                                   (!voucher.Date.HasValue || item.Date == voucher.Date))
                                   .ToList();

                    if (lst.Count == 1)
                        lst[0].VoucherID = voucher.ID;
                    else
                        yield return voucher;
                }
            }
        }

        /// <summary>
        ///     根据资产计算表更新账面
        /// </summary>
        /// <param name="asset">资产</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="isCollapsed">是否压缩</param>
        /// <param name="editOnly">是否只允许更新</param>
        /// <returns>无法更新的条目</returns>
        public IEnumerable<AssetItem> Update(Asset asset, DateFilter rng,
                                             bool isCollapsed = false, bool editOnly = false)
        {
            if (asset.Schedule == null)
                yield break;

            var bookValue = 0D;
            foreach (var item in asset.Schedule)
            {
                if (item.Date.Within(rng))
                    if (!UpdateItem(asset, item, bookValue, isCollapsed, editOnly))
                        yield return item;
                bookValue = item.BookValue;
            }
        }

        /// <summary>
        ///     根据资产计算表条目更新账面
        /// </summary>
        /// <param name="asset">资产</param>
        /// <param name="item">计算表条目</param>
        /// <param name="bookValue">此条目前账面价值</param>
        /// <param name="isCollapsed">是否压缩</param>
        /// <param name="editOnly">是否只允许更新</param>
        /// <returns>是否成功</returns>
        private bool UpdateItem(Asset asset, AssetItem item, double bookValue, bool isCollapsed = false,
                                bool editOnly = false)
        {
            if (item is AcquisationItem)
                return UpdateVoucher(
                                     item,
                                     isCollapsed,
                                     editOnly,
                                     VoucherType.Ordinal,
                                     new VoucherDetail
                                         {
                                             Title = asset.Title,
                                             Content = asset.StringID,
                                             Fund = (item as AcquisationItem).OrigValue
                                         });

            if (item is DepreciateItem)
                return UpdateVoucher(
                                     item,
                                     isCollapsed,
                                     editOnly,
                                     VoucherType.Depreciation,
                                     new VoucherDetail
                                         {
                                             Title = asset.DepreciationExpenseTitle,
                                             SubTitle = asset.DepreciationExpenseSubTitle,
                                             Content = asset.StringID,
                                             Fund = (item as DepreciateItem).Amount
                                         },
                                     new VoucherDetail
                                         {
                                             Title = asset.DepreciationTitle,
                                             Content = asset.StringID,
                                             Fund = -(item as DepreciateItem).Amount
                                         });

            if (item is DevalueItem)
                return UpdateVoucher(
                                     item,
                                     isCollapsed,
                                     editOnly,
                                     VoucherType.Devalue,
                                     new VoucherDetail
                                         {
                                             Title = asset.DevaluationExpenseTitle,
                                             SubTitle = asset.DevaluationExpenseSubTitle,
                                             Content = asset.StringID,
                                             Fund = (item as DevalueItem).Amount
                                         },
                                     new VoucherDetail
                                         {
                                             Title = asset.DevaluationTitle,
                                             Content = asset.StringID,
                                             Fund = -(item as DevalueItem).Amount
                                         });

            if (item is DispositionItem)
            {
                var totalDep = asset.Schedule.Where(
                                                    it =>
                                                    DateHelper.CompareDate(it.Date, item.Date) < 0 &&
                                                    it is DepreciateItem)
                                    .Cast<DepreciateItem>().Aggregate(0D, (td, it) => td + it.Amount);
                var totalDev = asset.Schedule.Where(
                                                    it =>
                                                    DateHelper.CompareDate(it.Date, item.Date) < 0 &&
                                                    it is DevalueItem)
                                    .Cast<DevalueItem>().Aggregate(0D, (td, it) => td + it.Amount);
                return UpdateVoucher(
                                     item,
                                     isCollapsed,
                                     editOnly,
                                     VoucherType.Ordinal,
                                     new VoucherDetail
                                         {
                                             Title = asset.Title,
                                             Content = asset.StringID,
                                             Fund = -asset.Value
                                         },
                                     new VoucherDetail
                                         {
                                             Title = asset.DepreciationTitle,
                                             Content = asset.StringID,
                                             Fund = totalDep
                                         },
                                     new VoucherDetail
                                         {
                                             Title = asset.DevaluationTitle,
                                             Content = asset.StringID,
                                             Fund = totalDev
                                         },
                                     new VoucherDetail
                                         {
                                             Title = DefaultDispositionTitle,
                                             Content = asset.StringID,
                                             Fund = bookValue,
                                             Remark = Asset.IgnoranceMark // 不用于检索，只用于添加
                                         }
                    );
            }

            return false;
        }

        /// <summary>
        ///     根据资产计算表条目更新账面
        /// </summary>
        /// <param name="item">计算表条目</param>
        /// <param name="isCollapsed">是否压缩</param>
        /// <param name="editOnly">是否只允许更新</param>
        /// <param name="voucherType">记账凭证类型</param>
        /// <param name="expectedDetails">应填细目</param>
        /// <returns>是否成功</returns>
        private bool UpdateVoucher(AssetItem item, bool isCollapsed, bool editOnly, VoucherType voucherType,
                                   params VoucherDetail[] expectedDetails)
        {
            if (item.VoucherID == null)
                return !editOnly && GenerateVoucher(item, isCollapsed, voucherType, expectedDetails);

            var voucher = m_Db.SelectVoucher(item.VoucherID);
            if (voucher == null)
                return !editOnly && GenerateVoucher(item, isCollapsed, voucherType, expectedDetails);

            if (voucher.Date != (isCollapsed ? null : item.Date) &&
                !editOnly)
                return false;


            var modified = false;

            if (voucher.Type != voucherType)
            {
                modified = true;
                voucher.Type = voucherType;
            }

            foreach (var d in expectedDetails)
            {
                if (d.Remark == Asset.IgnoranceMark)
                    continue;

                bool sucess;
                bool mo;
                UpdateDetail(d, voucher, out sucess, out mo, editOnly);
                if (!sucess)
                    return false;
                modified |= mo;
            }

            if (modified)
                m_Db.Upsert(voucher);

            return true;
        }

        /// <summary>
        ///     更新记账凭证的细目
        /// </summary>
        /// <param name="expected">应填细目</param>
        /// <param name="voucher">记账凭证</param>
        /// <param name="sucess">是否成功</param>
        /// <param name="modified">是否更改了记账凭证</param>
        /// <param name="editOnly">是否只允许更新</param>
        private static void UpdateDetail(VoucherDetail expected, Voucher voucher,
                                         out bool sucess, out bool modified, bool editOnly = false)
        {
            sucess = false;
            modified = false;

            if (!expected.Fund.HasValue)
                throw new InvalidOperationException("expected");
            var fund = expected.Fund.Value;
            expected.Fund = null;
            var isEliminated = Math.Abs(fund) < Tolerance;

            var ds = voucher.Details.Where(d => d.IsMatch(expected)).ToList();

            expected.Fund = fund;

            if (ds.Count == 0)
            {
                if (isEliminated)
                {
                    sucess = true;
                    return;
                }

                if (editOnly)
                    return;

                voucher.Details.Add(expected);
                sucess = true;
                modified = true;
                return;
            }
            if (ds.Count > 1)
                return;

            if (isEliminated)
            {
                if (editOnly)
                    return;

                voucher.Details.Remove(ds[0]);
                sucess = true;
                modified = true;
                return;
            }

            // ReSharper disable once PossibleInvalidOperationException
            if (!(Math.Abs(ds[0].Fund.Value - fund) > Tolerance))
            {
                sucess = true;
                return;
            }

            ds[0].Fund = fund;
            modified = true;
        }

        /// <summary>
        ///     生成记账凭证、插入数据库并注册
        /// </summary>
        /// <param name="item">计算表条目</param>
        /// <param name="isCollapsed">是否压缩</param>
        /// <param name="voucherType">记账凭证类型</param>
        /// <param name="details">细目</param>
        /// <returns>是否成功</returns>
        private bool GenerateVoucher(AssetItem item, bool isCollapsed, VoucherType voucherType,
                                     params VoucherDetail[] details)
        {
            var voucher = new Voucher
                              {
                                  Date = isCollapsed ? null : item.Date,
                                  Type = voucherType,
                                  Remark = "automatically generated",
                                  Details = details
                              };
            var res = m_Db.Upsert(voucher);
            item.VoucherID = voucher.ID;
            return res;
        }

        /// <summary>
        ///     折旧
        /// </summary>
        public static void Depreciate(Asset asset)
        {
            if (!asset.Date.HasValue ||
                !asset.Value.HasValue ||
                !asset.Salvge.HasValue ||
                !asset.Life.HasValue)
                return;

            var items =
                asset.Schedule.Where(
                                     assetItem =>
                                     !(assetItem is DepreciateItem) || assetItem.Remark == AssetItem.IgnoranceMark)
                     .ToList();

            switch (asset.Method)
            {
                case DepreciationMethod.None:
                    break;
                case DepreciationMethod.StraightLine:
                    {
                        var lastYear = asset.Date.Value.Year + asset.Life.Value;
                        var lastMonth = asset.Date.Value.Month;

                        var dt = asset.Date.Value;
                        var flag = false;
                        for (var i = 0;; i++)
                        {
                            if (i < items.Count)
                            {
                                if (items[i].Date > dt)
                                {
                                    dt = items[i].Date ?? dt;
                                    flag = false;
                                    continue;
                                }
                                if (flag)
                                    continue;
                                if (items[i] is AcquisationItem ||
                                    items[i] is DispositionItem)
                                    continue;
                                if (items[i] is DepreciateItem) // With IgnoranceMark
                                {
                                    flag = true;
                                    continue;
                                }
                            }

                            flag = true;

                            if (dt.Year == asset.Date.Value.Year
                                &&
                                dt.Month == asset.Date.Value.Month)
                            {
                                dt = LastDayOfMonth(dt.Year, dt.Month + 1);
                                if (i == items.Count)
                                    i--;
                                continue;
                            }

                            var amount = items[i - 1].BookValue - asset.Salvge.Value;
                            var monthes = 12 * (lastYear - dt.Year) + lastMonth - dt.Month;

                            if (amount <= Tolerance ||
                                monthes < 0) // Ended, Over-depreciated or Dispositoned
                            {
                                if (i < items.Count)
                                    continue; // If another AcquisationItem exists
                                break;
                            }

                            items.Insert(
                                         i,
                                         new DepreciateItem
                                             {
                                                 Date = dt,
                                                 Amount = amount / (monthes + 1),
                                                 BookValue = items[i - 1].BookValue - amount / (monthes + 1)
                                             });

                            dt = LastDayOfMonth(dt.Year, dt.Month + 1);
                        }
                    }
                    //if (mo < 12)
                    //    for (var mon = mo + 1; mon <= 12; mon++)
                    //        items.Add(
                    //                  new DepreciateItem
                    //                      {
                    //                          Date = LastDayOfMonth(yr, mon),
                    //                          Amount = amount / n / 12
                    //                      });
                    //for (var year = 1; year < n; year++)
                    //    for (var mon = 1; mon <= 12; mon++)
                    //        items.Add(
                    //                  new DepreciateItem
                    //                      {
                    //                          Date = LastDayOfMonth(yr + year, mon),
                    //                          Amount = amount / n / 12
                    //                      });
                    //// if (mo > 0)
                    //{
                    //    for (var mon = 1; mon <= mo; mon++)
                    //        items.Add(
                    //                  new DepreciateItem
                    //                      {
                    //                          Date = LastDayOfMonth(yr + n, mon),
                    //                          Amount = amount / n / 12
                    //                      });
                    //}
                    break;
                case DepreciationMethod.SumOfTheYear:
                    if (items.Any(a => a is DevalueItem || a.Remark == AssetItem.IgnoranceMark) ||
                        items.Count(a => a is AcquisationItem) != 1)
                        throw new NotImplementedException();
                    {
                        var n = asset.Life.Value;
                        var mo = asset.Date.Value.Month;
                        var yr = asset.Date.Value.Year;
                        var amount = asset.Value.Value - asset.Salvge.Value;
                        var z = n * (n + 1) / 2;
                        var nstar = n - mo / 12D;
                        var zstar = (Math.Floor(nstar) + 1) * (Math.Floor(nstar) + 2 * (nstar - Math.Floor(nstar))) / 2;
                        if (mo < 12)
                        {
                            var a = amount * n / z * (12 - mo) / z;
                            amount -= a;
                            for (var mon = mo + 1; mon <= 12; mon++)
                                items.Add(
                                          new DepreciateItem
                                              {
                                                  Date = LastDayOfMonth(yr, mon),
                                                  Amount = a / (12 - mo)
                                              });
                        }
                        for (var year = 1; year < n; year++)
                            for (var mon = 1; mon <= 12; mon++)
                                items.Add(
                                          new DepreciateItem
                                              {
                                                  Date = LastDayOfMonth(yr + year, mon),
                                                  Amount = amount * (nstar - year + 1) / zstar / 12
                                              });
                        // if (mo > 0)
                        {
                            for (var mon = 1; mon <= mo; mon++)
                                items.Add(
                                          new DepreciateItem
                                              {
                                                  Date = LastDayOfMonth(yr + n, mon),
                                                  Amount = amount * (nstar - (n + 1) + 2) / zstar / 12
                                              });
                        }
                    }
                    break;
                case DepreciationMethod.DoubleDeclineMethod:
                    throw new NotImplementedException();
            }

            asset.Schedule = items;
        }
    }
}
