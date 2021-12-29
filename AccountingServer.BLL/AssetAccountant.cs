/* Copyright (C) 2020-2021 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.BLL;

/// <summary>
///     资产会计业务处理类
/// </summary>
internal class AssetAccountant : DistributedAccountant
{
    /// <summary>
    ///     固定资产清理
    /// </summary>
    private const int DefaultDispositionTitle = 1606;

    public AssetAccountant(DbSession db, Client client) : base(db, client) { }

    /// <summary>
    ///     调整资产计算表
    /// </summary>
    /// <param name="asset">资产</param>
    /// <returns>资产</returns>
    public static Asset InternalRegular(Asset asset)
    {
        if (asset.Remark == Asset.IgnoranceMark)
            return asset;
        if (!asset.Date.HasValue ||
            !asset.Value.HasValue)
            return asset;

        var lst = asset.Schedule?.ToList() ?? new();

        foreach (var assetItem in lst)
            if (assetItem is DepreciateItem ||
                assetItem is DevalueItem)
                if (assetItem.Date.HasValue)
                    assetItem.Date = DateHelper.LastDayOfMonth(
                        assetItem.Date.Value.Year,
                        assetItem.Date.Value.Month);

        lst.Sort(new AssetItemComparer());

        if (lst.Count == 0 ||
            lst[0] is not AcquisitionItem acq0)
            lst.Insert(
                0,
                new AcquisitionItem { Date = asset.Date, OrigValue = asset.Value.Value });
        else if (lst[0].Remark != AssetItem.IgnoranceMark)
        {
            acq0.Date = asset.Date;
            acq0.OrigValue = asset.Value.Value;
        }

        var bookValue = 0D;
        for (var i = 0; i < lst.Count; i++)
        {
            var item = lst[i];
            switch (item)
            {
                case AcquisitionItem acq:
                    bookValue += acq.OrigValue;
                    item.Value = bookValue;
                    break;
                case DepreciateItem dep:
                    bookValue -= dep.Amount;
                    item.Value = bookValue;
                    break;
                case DevalueItem dev when bookValue <= dev.FairValue &&
                    item.Remark != AssetItem.IgnoranceMark:
                    lst.RemoveAt(i--);
                    continue;
                case DevalueItem dev:
                    dev.Amount = bookValue - dev.FairValue;
                    bookValue = dev.FairValue;
                    item.Value = dev.FairValue;
                    break;
                case DispositionItem:
                    bookValue = 0;
                    break;
            }
        }

        asset.Schedule = lst;
        return asset;
    }

    /// <summary>
    ///     找出未在资产计算表中注册的记账凭证，并尝试建立引用
    /// </summary>
    /// <param name="asset">资产</param>
    /// <param name="rng">日期过滤器</param>
    /// <param name="query">检索式</param>
    /// <returns>未注册的记账凭证</returns>
    public IEnumerable<Voucher> RegisterVouchers(Asset asset, DateFilter rng,
        IQueryCompounded<IVoucherQueryAtom> query)
    {
        if (asset.Remark == Asset.IgnoranceMark)
            yield break;

        foreach (
            var voucher in
            Db.SelectVouchers(ParsingF.VoucherQuery(
                    $"U{asset.User.AsUser()} T{asset.Title.AsTitle()} {asset.StringID.Quotation('\'')}", Client))
                .Where(v => v.IsMatch(query)).ToEnumerable()) // TODO
        {
            if (voucher.Remark == Asset.IgnoranceMark)
                continue;

            if (asset.Schedule.Any(item => item.VoucherID == voucher.ID))
                continue;

            var value =
                voucher.Details.Single(d => d.Title == asset.Title && d.Content == asset.StringID).Fund!.Value;

            switch (value)
            {
                case > 0:
                    {
                        var lst = asset.Schedule.Where(item => item.Date.Within(rng))
                            .Where(
                                item =>
                                    item is AcquisitionItem acq &&
                                    (!voucher.Date.HasValue || acq.Date == voucher.Date) &&
                                    (acq.OrigValue - value).IsZero())
                            .ToList();

                        if (lst.Count == 1)
                            lst[0].VoucherID = voucher.ID;
                        else
                            yield return voucher;
                        break;
                    }
                case < 0:
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
                        break;
                    }
                default:
                    yield return voucher;
                    break;
            }
        }

        foreach (
            var voucher in
            Db.SelectVouchers(
                ParsingF.VoucherQuery(
                    $"U{asset.User.AsUser()} T{asset.DepreciationTitle.AsTitle()} {asset.StringID.Quotation('\'')}",
                    Client)).ToEnumerable() // TODO
        )
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

        foreach (
            var voucher in
            Db.SelectVouchers(
                ParsingF.VoucherQuery(
                    $"U{asset.User.AsUser()} T{asset.DevaluationTitle.AsTitle()} {asset.StringID.Quotation('\'')}",
                    Client)).ToEnumerable() // TODO
        )
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

    /// <summary>
    ///     根据资产计算表更新账面
    /// </summary>
    /// <param name="asset">资产</param>
    /// <param name="rng">日期过滤器</param>
    /// <param name="isCollapsed">是否压缩</param>
    /// <param name="editOnly">是否只允许更新</param>
    /// <returns>无法更新的条目</returns>
    public async IAsyncEnumerable<AssetItem> Update(Asset asset, DateFilter rng,
        bool isCollapsed = false, bool editOnly = false)
    {
        if (asset.Schedule == null)
            yield break;

        var bookValue = 0D;
        foreach (var item in asset.Schedule)
        {
            if (item.Date.Within(rng))
                if (!await UpdateItem(asset, item, bookValue, isCollapsed, editOnly))
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
    private async ValueTask<bool> UpdateItem(Asset asset, AssetItem item, double bookValue, bool isCollapsed = false,
        bool editOnly = false)
        => item switch
            {
                AcquisitionItem acq =>
                    await UpdateVoucher(item, isCollapsed, editOnly, VoucherType.Ordinary,
                        new VoucherDetail
                            {
                                User = asset.User,
                                Currency = asset.Currency,
                                Title = asset.Title,
                                Content = asset.StringID,
                                Fund = acq.OrigValue,
                            }),
                DepreciateItem dep =>
                    await UpdateVoucher(item, isCollapsed, editOnly, VoucherType.Depreciation,
                        new()
                            {
                                User = asset.User,
                                Currency = asset.Currency,
                                Title = asset.DepreciationExpenseTitle,
                                SubTitle = asset.DepreciationExpenseSubTitle,
                                Content = asset.StringID,
                                Fund = dep.Amount,
                            },
                        new()
                            {
                                User = asset.User,
                                Currency = asset.Currency,
                                Title = asset.DepreciationTitle,
                                Content = asset.StringID,
                                Fund = -dep.Amount,
                            }),
                DevalueItem dev =>
                    await UpdateVoucher(item, isCollapsed, editOnly, VoucherType.Devalue,
                        new()
                            {
                                User = asset.User,
                                Currency = asset.Currency,
                                Title = asset.DevaluationExpenseTitle,
                                SubTitle = asset.DevaluationExpenseSubTitle,
                                Content = asset.StringID,
                                Fund = dev.Amount,
                            },
                        new()
                            {
                                User = asset.User,
                                Currency = asset.Currency,
                                Title = asset.DevaluationTitle,
                                Content = asset.StringID,
                                Fund = -dev.Amount,
                            }),
                DispositionItem =>
                    await UpdateVoucher(item, isCollapsed, editOnly, VoucherType.Ordinary,
                        new()
                            {
                                User = asset.User,
                                Currency = asset.Currency,
                                Title = asset.Title,
                                Content = asset.StringID,
                                Fund = -asset.Value,
                            },
                        new()
                            {
                                User = asset.User,
                                Currency = asset.Currency,
                                Title = asset.DepreciationTitle,
                                Content = asset.StringID,
                                Fund = asset.Schedule.Where(it
                                        => DateHelper.CompareDate(it.Date, item.Date) < 0 && it is DepreciateItem)
                                    .Cast<DepreciateItem>()
                                    .Aggregate(0D, (td, it) => td + it.Amount),
                            },
                        new()
                            {
                                User = asset.User,
                                Currency = asset.Currency,
                                Title = asset.DevaluationTitle,
                                Content = asset.StringID,
                                Fund = asset.Schedule.Where(it
                                        => DateHelper.CompareDate(it.Date, item.Date) < 0 && it is DevalueItem)
                                    .Cast<DevalueItem>()
                                    .Aggregate(0D, (td, it) => td + it.Amount),
                            }, new()
                            {
                                User = asset.User,
                                Currency = asset.Currency,
                                Title = DefaultDispositionTitle,
                                Content = asset.StringID,
                                Fund = bookValue,
                                Remark = Asset.IgnoranceMark, // 不用于检索，只用于添加
                            }),
                _ => false,
            };

    /// <summary>
    ///     生成记账凭证、插入数据库并注册
    /// </summary>
    /// <param name="item">计算表条目</param>
    /// <param name="isCollapsed">是否压缩</param>
    /// <param name="voucherType">记账凭证类型</param>
    /// <param name="details">细目</param>
    /// <returns>是否成功</returns>
    private async ValueTask<bool> GenerateVoucher(AssetItem item, bool isCollapsed, VoucherType voucherType,
        params VoucherDetail[] details)
    {
        var voucher = new Voucher
            {
                Date = isCollapsed ? null : item.Date,
                Type = voucherType,
                Remark = "automatically generated",
                Details = details.ToList(),
            };
        var res = await Db.Upsert(voucher);
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
    private async ValueTask<bool> UpdateVoucher(AssetItem item, bool isCollapsed, bool editOnly,
        VoucherType voucherType,
        params VoucherDetail[] expectedDetails)
    {
        if (item.VoucherID == null)
            return !editOnly && await GenerateVoucher(item, isCollapsed, voucherType, expectedDetails);

        var voucher = await Db.SelectVoucher(item.VoucherID);
        if (voucher == null)
            return !editOnly && await GenerateVoucher(item, isCollapsed, voucherType, expectedDetails);

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

            UpdateDetail(d, voucher, out var success, out var mo, editOnly);
            if (!success)
                return false;

            modified |= mo;
        }

        if (modified)
            await Db.Upsert(voucher);

        return true;
    }

    /// <summary>
    ///     折旧
    /// </summary>
    public static void Depreciate(Asset asset)
    {
        if (!asset.Date.HasValue ||
            !asset.Value.HasValue ||
            !asset.Salvage.HasValue ||
            !asset.Life.HasValue)
            return;

        var items =
            asset.Schedule.Where(
                    assetItem =>
                        assetItem is not DepreciateItem || assetItem.Remark == AssetItem.IgnoranceMark)
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
                            switch (items[i])
                            {
                                case AcquisitionItem:
                                case DispositionItem:
                                    continue;
                                // With IgnoranceMark
                                case DepreciateItem:
                                    flag = true;
                                    continue;
                            }
                        }

                        flag = true;

                        if (dt.Year == asset.Date.Value.Year &&
                            dt.Month == asset.Date.Value.Month)
                        {
                            dt = DateHelper.LastDayOfMonth(dt.Year, dt.Month + 1);
                            if (i == items.Count)
                                i--;
                            continue;
                        }

                        var amount = items[i - 1].Value - asset.Salvage.Value;
                        var months = 12 * (lastYear - dt.Year) + lastMonth - dt.Month;

                        if (amount.IsZero() ||
                            months < 0) // Ended, Over-depreciated or Disposed
                        {
                            if (i < items.Count)
                                continue; // If another AcquisitionItem exists

                            break;
                        }

                        items.Insert(
                            i,
                            new DepreciateItem
                                {
                                    Date = dt,
                                    Amount = amount / (months + 1),
                                    Value = items[i - 1].Value - amount / (months + 1),
                                });

                        dt = DateHelper.LastDayOfMonth(dt.Year, dt.Month + 1);
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
                    items.Count(a => a is AcquisitionItem) != 1)
                    throw new NotImplementedException();

                {
                    var n = asset.Life.Value;
                    var mo = asset.Date.Value.Month;
                    var yr = asset.Date.Value.Year;
                    var amount = asset.Value.Value - asset.Salvage.Value;
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
                                        Date = DateHelper.LastDayOfMonth(yr, mon), Amount = a / (12 - mo),
                                    });
                    }

                    for (var year = 1; year < n; year++)
                    for (var mon = 1; mon <= 12; mon++)
                        items.Add(
                            new DepreciateItem
                                {
                                    Date = DateHelper.LastDayOfMonth(yr + year, mon),
                                    Amount = amount * (nstar - year + 1) / zstar / 12,
                                });
                    // if (mo > 0)
                    {
                        for (var mon = 1; mon <= mo; mon++)
                            items.Add(
                                new DepreciateItem
                                    {
                                        Date = DateHelper.LastDayOfMonth(yr + n, mon),
                                        Amount = amount * (nstar - (n + 1) + 2) / zstar / 12,
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
