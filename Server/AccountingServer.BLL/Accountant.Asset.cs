using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    public partial class Accountant
    {
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
            if (!dt.HasValue || asset.Schedule == null)
                return asset.Value;

            var last = asset.Schedule.LastOrDefault(item => DateHelper.CompareDate(item.Date, dt) <= 0);
            if (last != null)
                return last.BookValue;
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

            var lst = asset.Schedule == null ? new List<AssetItem>() : asset.Schedule.ToList();
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
                    bookValue = (item as DevalueItem).FairValue;
                    item.BookValue = (item as DevalueItem).FairValue;
                }
                else if (item is DispositionItem)
                    if (item.Remark != AssetItem.IgnoranceMark)
                    {
                        (item as DispositionItem).NetValue = bookValue;
                        bookValue = 0;
                    }
                    else
                        bookValue -= (item as DispositionItem).NetValue;
            }

            asset.Schedule = lst.ToArray();
        }

        /// <summary>
        ///     找出未在资产计算表中注册的凭证，并尝试建立引用
        /// </summary>
        /// <param name="asset">资产</param>
        /// <returns>未注册的凭证</returns>
        public IEnumerable<Voucher> RegisterVouchers(Asset asset)
        {
            if (asset.Remark == Asset.IgnoranceMark)
                yield break;

            {
                var filter = new VoucherDetail
                                 {
                                     Title = asset.Title,
                                     Content = asset.StringID
                                 };
                foreach (var voucher in m_Db.FilteredSelect(filter, DateFilter.Unconstrained))
                {
                    if (voucher.Remark == Asset.IgnoranceMark)
                        continue;

                    if (asset.Schedule.Any(item => item.VoucherID == voucher.ID))
                        continue;

                    // ReSharper disable once PossibleInvalidOperationException
                    var value = voucher.Details.Single(d => d.IsMatch(filter)).Fund.Value;

                    if (value > 0)
                    {
                        var lst = asset.Schedule.Where(
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
                        var lst = asset.Schedule.Where(
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
                foreach (var voucher in m_Db.FilteredSelect(filter, DateFilter.Unconstrained))
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
                foreach (var voucher in m_Db.FilteredSelect(filter, DateFilter.Unconstrained))
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
        /// <param name="disableCreation">是否要禁止自动生成凭证</param>
        /// <returns>无法更新的条目</returns>
        public IEnumerable<AssetItem> Update(Asset asset, DateFilter rng,
                                             bool isCollapsed = false, bool disableCreation = false)
        {
            if (asset.Schedule == null)
                yield break;

            var bookValue = 0D;
            foreach (var item in asset.Schedule)
            {
                if (item.Date.Within(rng))
                    if (!UpdateItem(asset, item, bookValue, isCollapsed, disableCreation))
                        yield return item;
                bookValue = item.BookValue;
            }
        }

        private bool UpdateItem(Asset asset, AssetItem item, double bookValue, bool isCollapsed = false,
                                bool disableCreation = false)
        {
            if (item is AcquisationItem)
                return UpdateItem(asset, item as AcquisationItem, isCollapsed, disableCreation);
            if (item is DepreciateItem)
                return UpdateItem(asset, item as DepreciateItem, isCollapsed, disableCreation);
            if (item is DevalueItem)
                return UpdateItem(asset, item as DevalueItem, bookValue, isCollapsed, disableCreation);
            //if (item is DispositionItem)
            //    return UpdateItem(asset, item as DispositionItem, bookValue, isCollapsed, disableCreation);

            return false;
        }

        private bool UpdateItem(Asset asset, AcquisationItem item, bool isCollapsed = false,
                                bool disableCreation = false)
        {
            if (item.VoucherID != null)
            {
                var voucher = m_Db.SelectVoucher(item.VoucherID);
                if (voucher != null)
                {
                    if (voucher.Date != (isCollapsed ? null : item.Date))
                        return false;

                    voucher.Type = VoucherType.Ordinal;

                    var flag = false;
                    {
                        var ds = voucher.Details.Where(
                                                       d => d.IsMatch(
                                                                      new VoucherDetail
                                                                          {
                                                                              Title = asset.Title,
                                                                              Content =
                                                                                  asset.StringID
                                                                          })).ToList();
                        if (ds.Count == 0)
                        {
                            if (disableCreation)
                                return false;

                            var l = voucher.Details.ToList();
                            l.Add(
                                  new VoucherDetail
                                      {
                                          Title = asset.Title,
                                          Content = asset.StringID,
                                          Fund = item.OrigValue
                                      });
                            voucher.Details = l.ToArray();
                            flag = true;
                        }
                        else if (ds.Count > 1)
                            return false;

                        if (Math.Abs(ds[0].Fund.Value - item.OrigValue) > Tolerance)
                        {
                            ds[0].Fund = item.OrigValue;
                            flag = true;
                        }
                    }

                    if (!flag)
                        return true;
                    return m_Db.Update(voucher);
                }
            }
            {
                if (disableCreation)
                    return false;

                var voucher = new Voucher
                                  {
                                      Date = isCollapsed ? null : item.Date,
                                      Type = VoucherType.Ordinal,
                                      Remark = "automatically generated",
                                      Details = new[]
                                                    {
                                                        new VoucherDetail
                                                            {
                                                                Title = asset.Title,
                                                                Content = asset.StringID,
                                                                Fund = item.OrigValue
                                                            }
                                                    }
                                  };
                var res = m_Db.Insert(voucher);
                item.VoucherID = voucher.ID;
                return res;
            }
        }

        private bool UpdateItem(Asset asset, DepreciateItem item, bool isCollapsed = false, bool disableCreation = false)
        {
            if (item.VoucherID != null)
            {
                var voucher = m_Db.SelectVoucher(item.VoucherID);
                if (voucher != null)
                {
                    if (voucher.Date != (isCollapsed ? null : item.Date))
                        return false;

                    voucher.Type = VoucherType.Depreciation;

                    var flag = false;
                    {
                        var ds = voucher.Details.Where(
                                                       d => d.IsMatch(
                                                                      new VoucherDetail
                                                                          {
                                                                              Title = asset.DepreciationTitle,
                                                                              Content =
                                                                                  asset.StringID
                                                                          })).ToList();
                        if (ds.Count == 0)
                        {
                            if (disableCreation)
                                return false;

                            var l = voucher.Details.ToList();
                            l.Add(
                                  new VoucherDetail
                                      {
                                          Title = asset.DepreciationTitle,
                                          Content = asset.StringID,
                                          Fund = item.Amount
                                      });
                            voucher.Details = l.ToArray();
                            flag = true;
                        }
                        else if (ds.Count > 1)
                            return false;

                        if (Math.Abs(ds[0].Fund.Value - (-item.Amount)) > Tolerance)
                        {
                            ds[0].Fund = -item.Amount;
                            flag = true;
                        }
                    }
                    {
                        var ds = voucher.Details.Where(
                                                       d => d.IsMatch(
                                                                      new VoucherDetail
                                                                          {
                                                                              Title = asset.DepreciationExpenseTitle,
                                                                              SubTitle =
                                                                                  asset.DepreciationExpenseSubTitle,
                                                                              Content =
                                                                                  asset.StringID
                                                                          })).ToList();
                        if (ds.Count == 0)
                        {
                            if (disableCreation)
                                return false;

                            var l = voucher.Details.ToList();
                            l.Add(
                                  new VoucherDetail
                                      {
                                          Title = asset.DepreciationExpenseTitle,
                                          SubTitle = asset.DepreciationExpenseSubTitle,
                                          Content = asset.StringID,
                                          Fund = item.Amount
                                      });
                            voucher.Details = l.ToArray();
                            flag = true;
                        }
                        else if (ds.Count > 1)
                            return false;

                        if (Math.Abs(ds[0].Fund.Value - item.Amount) > Tolerance)
                        {
                            ds[0].Fund = item.Amount;
                            flag = true;
                        }
                    }

                    if (!flag)
                        return true;
                    return m_Db.Update(voucher);
                }
            }
            {
                if (disableCreation)
                    return false;

                var voucher = new Voucher
                                  {
                                      Date = isCollapsed ? null : item.Date,
                                      Type = VoucherType.Depreciation,
                                      Remark = "automatically generated",
                                      Details = new[]
                                                    {
                                                        new VoucherDetail
                                                            {
                                                                Title = asset.DepreciationTitle,
                                                                Content = asset.StringID,
                                                                Fund = -item.Amount
                                                            },
                                                        new VoucherDetail
                                                            {
                                                                Title = asset.DepreciationExpenseTitle,
                                                                SubTitle = asset.DepreciationExpenseSubTitle,
                                                                Content = asset.StringID,
                                                                Fund = item.Amount
                                                            }
                                                    }
                                  };
                var res = m_Db.Insert(voucher);
                item.VoucherID = voucher.ID;
                return res;
            }
        }

        private bool UpdateItem(Asset asset, DevalueItem item, double bookValue, bool isCollapsed = false,
                                bool disableCreation = false)
        {
            if (item.VoucherID != null)
            {
                var voucher = m_Db.SelectVoucher(item.VoucherID);
                if (voucher != null)
                {
                    if (voucher.Date != (isCollapsed ? null : item.Date))
                        return false;

                    var fund = bookValue - item.FairValue;
                    voucher.Type = VoucherType.Devalue;

                    var flag = false;
                    {
                        var ds = voucher.Details.Where(
                                                       d => d.IsMatch(
                                                                      new VoucherDetail
                                                                          {
                                                                              Title = asset.DevaluationTitle,
                                                                              Content =
                                                                                  asset.StringID
                                                                          })).ToList();
                        if (ds.Count == 0)
                        {
                            if (disableCreation)
                                return false;

                            var l = voucher.Details.ToList();
                            l.Add(
                                  new VoucherDetail
                                      {
                                          Title = asset.DevaluationTitle,
                                          Content = asset.StringID,
                                          Fund = -fund
                                      });
                            flag = true;
                            voucher.Details = l.ToArray();
                        }
                        else if (ds.Count > 1)
                            return false;

                        if (Math.Abs(ds[0].Fund.Value - (-fund)) > Tolerance)
                        {
                            ds[0].Fund = -fund;
                            flag = true;
                        }
                    }
                    {
                        var ds = voucher.Details.Where(
                                                       d => d.IsMatch(
                                                                      new VoucherDetail
                                                                          {
                                                                              Title = asset.DevaluationExpenseTitle,
                                                                              SubTitle =
                                                                                  asset.DevaluationExpenseSubTitle,
                                                                              Content =
                                                                                  asset.StringID
                                                                          })).ToList();
                        if (ds.Count == 0)
                        {
                            if (disableCreation)
                                return false;

                            var l = voucher.Details.ToList();
                            l.Add(
                                  new VoucherDetail
                                      {
                                          Title = asset.DepreciationExpenseTitle,
                                          SubTitle = asset.DevaluationExpenseSubTitle,
                                          Content = asset.StringID,
                                          Fund = fund
                                      });
                            voucher.Details = l.ToArray();
                        }
                        else if (ds.Count > 1)
                            return false;

                        if (Math.Abs(ds[0].Fund.Value - fund) > Tolerance)
                        {
                            ds[0].Fund = fund;
                            flag = true;
                        }
                    }

                    if (!flag)
                        return true;
                    return m_Db.Update(voucher);
                }
            }
            {
                if (disableCreation)
                    return false;

                var fund = bookValue - item.FairValue;
                var voucher = new Voucher
                                  {
                                      Date = isCollapsed ? null : item.Date,
                                      Type = VoucherType.Devalue,
                                      Remark = "automatically generated",
                                      Details = new[]
                                                    {
                                                        new VoucherDetail
                                                            {
                                                                Title = asset.DevaluationTitle,
                                                                Content = asset.StringID,
                                                                Fund = -fund
                                                            },
                                                        new VoucherDetail
                                                            {
                                                                Title = asset.DevaluationExpenseTitle,
                                                                SubTitle = asset.DevaluationExpenseSubTitle,
                                                                Content = asset.StringID,
                                                                Fund = fund
                                                            }
                                                    }
                                  };
                var res = m_Db.Insert(voucher);
                item.VoucherID = voucher.ID;
                return res;
            }
        }

        private bool UpdateItem(Asset asset, DispositionItem item, double bookValue, bool isCollapsed = false,
                                bool disableCreation = false) { throw new NotImplementedException(); }

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

            var items = asset.Schedule.ToList();
            items.RemoveAll(a => a is DepreciateItem && a.Remark != AssetItem.IgnoranceMark);

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

            asset.Schedule = items.ToArray();
        }
    }
}
