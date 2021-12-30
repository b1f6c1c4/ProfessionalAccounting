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
using System.Text;
using System.Threading.Tasks;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell;

/// <summary>
///     资产表达式解释器
/// </summary>
internal class AssetShell : DistributedShell
{
    /// <inheritdoc />
    protected override string Initial => "a";

    /// <inheritdoc />
    protected override async IAsyncEnumerable<string> ExecuteList(IQueryCompounded<IDistributedQueryAtom> distQuery,
        DateTime? dt,
        bool showSchedule, Session session)
    {
        await foreach (var a in Sort(session.Accountant.SelectAssetsAsync(distQuery)))
            yield return await ListAsset(a, session, dt, showSchedule);
    }

    /// <inheritdoc />
    protected override IAsyncEnumerable<string> ExecuteQuery(IQueryCompounded<IDistributedQueryAtom> distQuery,
        Session session)
        => session.Serializer.PresentAssets(Sort(session.Accountant.SelectAssetsAsync(distQuery)));

    /// <inheritdoc />
    protected override async IAsyncEnumerable<string> ExecuteRegister(IQueryCompounded<IDistributedQueryAtom> distQuery,
        DateFilter rng,
        IQueryCompounded<IVoucherQueryAtom> query, Session session)
    {
        await foreach (var a in Sort(session.Accountant.SelectAssetsAsync(distQuery)))
        {
            await foreach (var s in session.Serializer.PresentVouchers(
                               session.Accountant.RegisterVouchers(a, rng, query).ToAsyncEnumerable()))
                yield return s;

            await session.Accountant.UpsertAsync(a);
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<string> ExecuteUnregister(IQueryCompounded<IDistributedQueryAtom> distQuery, DateFilter rng,
        IQueryCompounded<IVoucherQueryAtom> query, Session session)
    {
        await foreach (var a in Sort(session.Accountant.SelectAssetsAsync(distQuery)))
        {
            foreach (var item in a.Schedule.Where(item => item.Date.Within(rng)))
            {
                if (query != null)
                {
                    if (item.VoucherID == null)
                        continue;

                    var voucher = await session.Accountant.SelectVoucherAsync(item.VoucherID);
                    if (voucher != null)
                        if (!MatchHelper.IsMatch(query, voucher.IsMatch))
                            continue;
                }

                item.VoucherID = null;
            }

            yield return await ListAsset(a, session);
            await session.Accountant.UpsertAsync(a);
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<string> ExecuteRecal(IQueryCompounded<IDistributedQueryAtom> distQuery,
        Session session)
    {
        await foreach (var a in Sort(session.Accountant.SelectAssetsAsync(distQuery)))
        {
            Accountant.Depreciate(a);
            yield return session.Serializer.PresentAsset(a);
            await session.Accountant.UpsertAsync(a);
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<string> ExecuteResetSoft(IQueryCompounded<IDistributedQueryAtom> distQuery, DateFilter rng, Session session)
    {
        await foreach (var a in session.Accountant.SelectAssetsAsync(distQuery))
        {
            if (a.Schedule == null)
                continue;

            var flag = false;
            foreach (var item in a.Schedule.Where(item => item.Date.Within(rng))
                         .Where(static item => item.VoucherID != null))
            {
                if (await session.Accountant.SelectVoucherAsync(item.VoucherID) != null)
                    continue;

                item.VoucherID = null;
                flag = true;
            }

            if (!flag)
                continue;

            yield return session.Serializer.PresentAsset(a);
            await session.Accountant.UpsertAsync(a);
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<string> ExecuteResetMixed(IQueryCompounded<IDistributedQueryAtom> distQuery, DateFilter rng, Session session)
    {
        await foreach (var a in session.Accountant.SelectAssetsAsync(distQuery))
        {
            if (a.Schedule == null)
                continue;

            var flag = false;
            foreach (var item in a.Schedule.Where(item => item.Date.Within(rng))
                         .Where(static item => item.VoucherID != null))
            {
                var voucher = await session.Accountant.SelectVoucherAsync(item.VoucherID);
                if (voucher == null)
                {
                    item.VoucherID = null;
                    flag = true;
                }
                else if (await session.Accountant.DeleteVoucherAsync(voucher.ID))
                {
                    item.VoucherID = null;
                    flag = true;
                }
            }

            if (!flag)
                continue;
            yield return session.Serializer.PresentAsset(a);
            await session.Accountant.UpsertAsync(a);
        }
    }

    protected override async IAsyncEnumerable<string> ExecuteResetHard(IQueryCompounded<IDistributedQueryAtom> distQuery, IQueryCompounded<IVoucherQueryAtom> query, Session session)
    {
        await foreach (var a in session.Accountant.SelectAssetsAsync(distQuery))
        {
            var cnt = await session.Accountant.DeleteVouchersAsync(
                new IntersectQueries<IVoucherQueryAtom>(
                    query ?? VoucherQueryUnconstrained.Instance,
                    ParsingF.VoucherQuery(
                        $"{{ T{a.DepreciationTitle.AsTitle()} {a.StringID.Quotation('\'')} Depreciation }} + {{ T{a.DevaluationTitle.AsTitle()} {a.StringID.Quotation('\'')} Devalue }}",
                        session.Client)));
            yield return $"{a.StringID} {a.Name} => {cnt}";
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<string> ExecuteApply(IQueryCompounded<IDistributedQueryAtom> distQuery,
        DateFilter rng, bool isCollapsed, Session session)
    {
        await foreach (var a in Sort(session.Accountant.SelectAssetsAsync(distQuery)))
        {
            await foreach (var item in session.Accountant.Update(a, rng, isCollapsed))
                yield return ListAssetItem(item);

            await session.Accountant.UpsertAsync(a);
        }
    }

    /// <summary>
    ///     执行检查表达式
    /// </summary>
    /// <param name="distQuery">分期检索式</param>
    /// <param name="rng">日期过滤器</param>
    /// <param name="session">客户端会话</param>
    /// <returns>执行结果</returns>
    protected override async IAsyncEnumerable<string> ExecuteCheck(IQueryCompounded<IDistributedQueryAtom> distQuery,
        DateFilter rng, Session session)
    {
        await foreach (var a in Sort(session.Accountant.SelectAssetsAsync(distQuery)))
        {
            var sbi = new StringBuilder();
            await foreach (var item in session.Accountant.Update(a, rng, false, true))
                sbi.AppendLine(ListAssetItem(item));

            if (sbi.Length != 0)
            {
                yield return await ListAsset(a, session, null, false);
                yield return sbi.ToString();
            }

            await session.Accountant.UpsertAsync(a);
        }
    }

    /// <summary>
    ///     显示资产及其折旧计算表
    /// </summary>
    /// <param name="asset">资产</param>
    /// <param name="session">客户端会话</param>
    /// <param name="dt">计算账面价值的时间</param>
    /// <param name="showSchedule">是否显示折旧计算表</param>
    /// <returns>格式化的信息</returns>
    private async ValueTask<string> ListAsset(Asset asset, Session session, DateTime? dt = null,
        bool showSchedule = true)
    {
        var sb = new StringBuilder();

        var bookValue = Accountant.GetBookValueOn(asset, dt);
        if (dt.HasValue &&
            !bookValue?.IsZero() != true)
            return null;

        sb.AppendLine($"{asset.StringID} {asset.Name.CPadRight(35)}{asset.Date:yyyyMMdd}" +
            $"U{asset.User.AsUser().CPadRight(5)} " +
            asset.Value.AsCurrency(asset.Currency).CPadLeft(13) +
            (dt.HasValue ? bookValue.AsCurrency(asset.Currency).CPadLeft(13) : "-".CPadLeft(13)) +
            asset.Salvage.AsCurrency(asset.Currency).CPadLeft(13) +
            asset.Title.AsTitle().CPadLeft(5) +
            asset.DepreciationTitle.AsTitle().CPadLeft(5) +
            asset.DevaluationTitle.AsTitle().CPadLeft(5) +
            asset.DepreciationExpenseTitle.AsTitle().CPadLeft(5) +
            asset.DepreciationExpenseSubTitle.AsSubTitle() +
            asset.DevaluationExpenseTitle.AsTitle().CPadLeft(5) +
            asset.DevaluationExpenseSubTitle.AsSubTitle() +
            asset.Life.ToString().CPadLeft(4) +
            asset.Method.ToString().CPadLeft(20));

        if (showSchedule && asset.Schedule != null)
            foreach (var assetItem in asset.Schedule)
            {
                sb.AppendLine(ListAssetItem(assetItem));
                if (assetItem.VoucherID != null)
                    sb.AppendLine(session.Serializer
                        .PresentVoucher(await session.Accountant.SelectVoucherAsync(assetItem.VoucherID)).Wrap());
            }

        return sb.ToString();
    }

    /// <summary>
    ///     显示折旧计算表条目
    /// </summary>
    /// <param name="assetItem">折旧计算表条目</param>
    /// <returns>格式化的信息</returns>
    private static string ListAssetItem(IDistributedItem assetItem)
        => assetItem switch
            {
                AcquisitionItem acq => string.Format(
                    "   {0:yyyMMdd} ACQ:{1} ={3} ({2})",
                    assetItem.Date,
                    acq.OrigValue.AsCurrency().CPadLeft(13),
                    assetItem.VoucherID,
                    assetItem.Value.AsCurrency().CPadLeft(13)),
                DepreciateItem dep => string.Format(
                    "   {0:yyyMMdd} DEP:{1} ={3} ({2})",
                    assetItem.Date,
                    dep.Amount.AsCurrency().CPadLeft(13),
                    assetItem.VoucherID,
                    assetItem.Value.AsCurrency().CPadLeft(13)),
                DevalueItem dev => string.Format(
                    "   {0:yyyMMdd} DEV:{1} ={3} ({2})",
                    assetItem.Date,
                    dev.Amount.AsCurrency().CPadLeft(13),
                    assetItem.VoucherID,
                    assetItem.Value.AsCurrency().CPadLeft(13)),
                DispositionItem => string.Format(
                    "   {0:yyyMMdd} DSP:{1} ={3} ({2})",
                    assetItem.Date,
                    "ALL".CPadLeft(13),
                    assetItem.VoucherID,
                    assetItem.Value.AsCurrency().CPadLeft(13)),
                _ => null,
            };

    /// <summary>
    ///     对资产进行排序
    /// </summary>
    /// <param name="enumerable">资产</param>
    /// <returns>排序后的资产</returns>
    private static IAsyncEnumerable<Asset> Sort(IAsyncEnumerable<Asset> enumerable)
        => enumerable.OrderBy(static a => a.Date, new DateComparer()).ThenBy(static a => a.Name).ThenBy(static a => a.ID);
}
