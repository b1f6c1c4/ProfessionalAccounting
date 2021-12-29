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

// ReSharper disable UnusedMember.Global

namespace AccountingServer.BLL;

/// <summary>
///     基本会计业务处理类
/// </summary>
public class Accountant : IHistoricalExchange
{
    private readonly AmortAccountant m_AmortAccountant;

    private readonly AssetAccountant m_AssetAccountant;
    private readonly DbSession m_Db;
    public readonly Client Client;

    public Accountant(DbSession db, string user, DateTime dt)
    {
        m_Db = db;
        Client = new() { User = user, Today = dt };

        m_AssetAccountant = new(m_Db, Client);
        m_AmortAccountant = new(m_Db, Client);
    }

    /// <summary>
    ///     返回结果数量上限
    /// </summary>
    public int Limit { private get; init; }

    public DbSession.VirtualizeLock Virtualize()
        => m_Db.Virtualize();

    #region Voucher

    [Obsolete]
    public Voucher SelectVoucher(string id)
        => m_Db.SelectVoucher(id).AsTask().Result;

    public ValueTask<Voucher> SelectVoucherAsync(string id)
        => m_Db.SelectVoucher(id);

    [Obsolete]
    public IEnumerable<Voucher> SelectVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => m_Db.SelectVouchers(query).ToEnumerable();

    public IAsyncEnumerable<Voucher> SelectVouchersAsync(IQueryCompounded<IVoucherQueryAtom> query)
        => m_Db.SelectVouchers(query);

    [Obsolete]
    public IEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query)
        => m_Db.SelectVoucherDetails(query).ToEnumerable();

    public IAsyncEnumerable<VoucherDetail> SelectVoucherDetailsAsync(IVoucherDetailQuery query)
        => m_Db.SelectVoucherDetails(query);

    [Obsolete]
    public ISubtotalResult SelectVoucherDetailsGrouped(IGroupedQuery query)
        => m_Db.SelectVoucherDetailsGrouped(query, Limit).AsTask().Result;

    public ValueTask<ISubtotalResult> SelectVoucherDetailsGroupedAsync(IGroupedQuery query)
        => m_Db.SelectVoucherDetailsGrouped(query, Limit);

    [Obsolete]
    public ISubtotalResult SelectVouchersGrouped(IVoucherGroupedQuery query)
        => m_Db.SelectVouchersGrouped(query, Limit).AsTask().Result;

    public ValueTask<ISubtotalResult> SelectVouchersGroupedAsync(IVoucherGroupedQuery query)
        => m_Db.SelectVouchersGrouped(query, Limit);

    [Obsolete]
    public IEnumerable<(Voucher, string, string, double)> SelectUnbalancedVouchers(
        IQueryCompounded<IVoucherQueryAtom> query)
        => m_Db.SelectUnbalancedVouchers(query).ToEnumerable();

    public IAsyncEnumerable<(Voucher, string, string, double)> SelectUnbalancedVouchersAsync(
        IQueryCompounded<IVoucherQueryAtom> query)
        => m_Db.SelectUnbalancedVouchers(query);

    [Obsolete]
    public IEnumerable<(Voucher, List<string>)> SelectDuplicatedVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => m_Db.SelectDuplicatedVouchers(query).ToEnumerable();

    public IAsyncEnumerable<(Voucher, List<string>)> SelectDuplicatedVouchersAsync(
        IQueryCompounded<IVoucherQueryAtom> query)
        => m_Db.SelectDuplicatedVouchers(query);

    [Obsolete]
    public bool DeleteVoucher(string id)
        => m_Db.DeleteVoucher(id).AsTask().Result;

    public ValueTask<bool> DeleteVoucherAsync(string id)
        => m_Db.DeleteVoucher(id);

    [Obsolete]
    public long DeleteVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => m_Db.DeleteVouchers(query).AsTask().Result;

    public ValueTask<long> DeleteVouchersAsync(IQueryCompounded<IVoucherQueryAtom> query)
        => m_Db.DeleteVouchers(query);

    [Obsolete]
    public bool Upsert(Voucher entity)
        => m_Db.Upsert(entity).AsTask().Result;

    public ValueTask<bool> UpsertAsync(Voucher entity)
        => m_Db.Upsert(entity);

    [Obsolete]
    public long Upsert(IReadOnlyCollection<Voucher> entities)
        => m_Db.Upsert(entities).AsTask().Result;

    public ValueTask<long> UpsertAsync(IReadOnlyCollection<Voucher> entities)
        => m_Db.Upsert(entities);

    #endregion

    #region Asset

    [Obsolete]
    public Asset SelectAsset(Guid id)
        => AssetAccountant.InternalRegular(m_Db.SelectAsset(id).AsTask().Result);

    public async ValueTask<Asset> SelectAssetAsync(Guid id)
        => AssetAccountant.InternalRegular(await m_Db.SelectAsset(id));

    [Obsolete]
    public IEnumerable<Asset> SelectAssets(IQueryCompounded<IDistributedQueryAtom> filter)
        => m_Db.SelectAssets(filter).Select(AssetAccountant.InternalRegular).ToEnumerable();

    public IAsyncEnumerable<Asset> SelectAssetsAsync(IQueryCompounded<IDistributedQueryAtom> filter)
        => m_Db.SelectAssets(filter).Select(AssetAccountant.InternalRegular);

    [Obsolete]
    public bool DeleteAsset(Guid id)
        => m_Db.DeleteAsset(id).AsTask().Result;

    public ValueTask<bool> DeleteAssetAsync(Guid id)
        => m_Db.DeleteAsset(id);

    [Obsolete]
    public long DeleteAssets(IQueryCompounded<IDistributedQueryAtom> filter)
        => m_Db.DeleteAssets(filter).AsTask().Result;

    public ValueTask<long> DeleteAssetsAsync(IQueryCompounded<IDistributedQueryAtom> filter)
        => m_Db.DeleteAssets(filter);

    [Obsolete]
    public bool Upsert(Asset entity)
        => m_Db.Upsert(entity).AsTask().Result;

    public ValueTask<bool> UpsertAsync(Asset entity)
        => m_Db.Upsert(entity);

    public IEnumerable<Voucher> RegisterVouchers(Asset asset, DateFilter rng,
        IQueryCompounded<IVoucherQueryAtom> query)
        => m_AssetAccountant.RegisterVouchers(asset, rng, query);

    public static void Depreciate(Asset asset) => AssetAccountant.Depreciate(asset);

    public IAsyncEnumerable<AssetItem> Update(Asset asset, DateFilter rng, bool isCollapsed = false,
        bool editOnly = false)
        => m_AssetAccountant.Update(asset, rng, isCollapsed, editOnly);

    #endregion

    #region Amort

    [Obsolete]
    public Amortization SelectAmortization(Guid id)
        => AmortAccountant.InternalRegular(m_Db.SelectAmortization(id).AsTask().Result);

    public async ValueTask<Amortization> SelectAmortizationAsync(Guid id)
        => AmortAccountant.InternalRegular(await m_Db.SelectAmortization(id));

    [Obsolete]
    public IEnumerable<Amortization> SelectAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
        => m_Db.SelectAmortizations(filter).Select(AmortAccountant.InternalRegular).ToEnumerable();

    public IAsyncEnumerable<Amortization> SelectAmortizationsAsync(IQueryCompounded<IDistributedQueryAtom> filter)
        => m_Db.SelectAmortizations(filter).Select(AmortAccountant.InternalRegular);

    [Obsolete]
    public bool DeleteAmortization(Guid id)
        => m_Db.DeleteAmortization(id).AsTask().Result;

    public ValueTask<bool> DeleteAmortizationAsync(Guid id)
        => m_Db.DeleteAmortization(id);

    [Obsolete]
    public long DeleteAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
        => m_Db.DeleteAmortizations(filter).AsTask().Result;

    public ValueTask<long> DeleteAmortizationsAsync(IQueryCompounded<IDistributedQueryAtom> filter)
        => m_Db.DeleteAmortizations(filter);

    [Obsolete]
    public bool Upsert(Amortization entity)
        => m_Db.Upsert(entity).AsTask().Result;

    public ValueTask<bool> UpsertAsync(Amortization entity)
        => m_Db.Upsert(entity);

    public IEnumerable<Voucher> RegisterVouchers(Amortization amort, DateFilter rng,
        IQueryCompounded<IVoucherQueryAtom> query)
        => m_AmortAccountant.RegisterVouchers(amort, rng, query);

    public static void Amortize(Amortization amort) => AmortAccountant.Amortize(amort);

    public IAsyncEnumerable<AmortItem> Update(Amortization amort, DateFilter rng, bool isCollapsed = false,
        bool editOnly = false)
        => m_AmortAccountant.Update(amort, rng, isCollapsed, editOnly);

    public static double? GetBookValueOn(IDistributed dist, DateTime? dt)
        => DistributedAccountant.GetBookValueOn(dist, dt);

    #endregion

    #region Exchange

    public ValueTask<double> Query(DateTime? date, string from, string to) => m_Db.Query(date, from, to);

    public ValueTask<double> SaveHistoricalRate(DateTime date, string from, string to)
        => m_Db.SaveHistoricalRate(date, from, to);

    #endregion
}
