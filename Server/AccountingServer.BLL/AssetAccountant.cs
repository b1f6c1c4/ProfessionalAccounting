﻿using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.DAL;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     资产会计业务处理类
    /// </summary>
    internal class AssetAccountant : DistributedAccountant
    {
        public AssetAccountant(IDbAdapter db) : base(db) { }

        /// <summary>
        ///     固定资产清理
        /// </summary>
        private const int DefaultDispositionTitle = 1606;

        /// <summary>
        ///     调整资产计算表
        /// </summary>
        /// <param name="asset">资产</param>
        public static void InternalRegular(Asset asset)
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
                        assetItem.Date = AccountantHelper.LastDayOfMonth(
                                                                         assetItem.Date.Value.Year,
                                                                         assetItem.Date.Value.Month);

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
                    item.Value = bookValue;
                }
                else if (item is DepreciateItem)
                {
                    bookValue -= (item as DepreciateItem).Amount;
                    item.Value = bookValue;
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
                    item.Value = (item as DevalueItem).FairValue;
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
                        Db.SelectVouchers(
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
                                              ((item as AcquisationItem).OrigValue - value).IsZero())
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
                foreach (var voucher in Db.SelectVouchers(new VoucherQueryAtomBase(filter: filter)))
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
                foreach (var voucher in Db.SelectVouchers(new VoucherQueryAtomBase(filter: filter)))
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
                bookValue = item.Value;
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
                                     VoucherType.Ordinary,
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
                                     VoucherType.Ordinary,
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
            var res = Db.Upsert(voucher);
            item.VoucherID = voucher.ID;
            return res;
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

            var voucher = Db.SelectVoucher(item.VoucherID);
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
                Db.Upsert(voucher);

            return true;
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
                                dt = AccountantHelper.LastDayOfMonth(dt.Year, dt.Month + 1);
                                if (i == items.Count)
                                    i--;
                                continue;
                            }

                            var amount = items[i - 1].Value - asset.Salvge.Value;
                            var monthes = 12 * (lastYear - dt.Year) + lastMonth - dt.Month;

                            if (amount.IsZero() ||
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
                                                 Value = items[i - 1].Value - amount / (monthes + 1)
                                             });

                            dt = AccountantHelper.LastDayOfMonth(dt.Year, dt.Month + 1);
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
                                                  Date = AccountantHelper.LastDayOfMonth(yr, mon),
                                                  Amount = a / (12 - mo)
                                              });
                        }
                        for (var year = 1; year < n; year++)
                            for (var mon = 1; mon <= 12; mon++)
                                items.Add(
                                          new DepreciateItem
                                              {
                                                  Date = AccountantHelper.LastDayOfMonth(yr + year, mon),
                                                  Amount = amount * (nstar - year + 1) / zstar / 12
                                              });
                        // if (mo > 0)
                        {
                            for (var mon = 1; mon <= mo; mon++)
                                items.Add(
                                          new DepreciateItem
                                              {
                                                  Date = AccountantHelper.LastDayOfMonth(yr + n, mon),
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
