/* Copyright (C) 2020-2025 b1f6c1c4
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
using System.Diagnostics;
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
    public readonly Client Client;
    private readonly AmortAccountant m_AmortAccountant;

    private readonly AssetAccountant m_AssetAccountant;
    private DbSession m_Db;

    public Accountant(DbSession db, string user, DateTime dt)
    {
        m_Db = db;
        Client = new() { User = user, Today = dt.Date };

        m_AssetAccountant = new(m_Db, Client);
        m_AmortAccountant = new(m_Db, Client);
    }

    /// <summary>
    ///     返回结果数量上限
    /// </summary>
    public int Limit { private get; init; }

    public VirtualizeLock Virtualize()
        => new(this);

    public class VirtualizeLock : IAsyncDisposable
    {
        internal VirtualizeLock(Accountant accountant)
        {
            m_Accountant = accountant;
            m_Db = m_Accountant.m_Db;
            m_Accountant.m_Db = new Virtualizer(m_Db.Db);
        }

        private readonly Accountant m_Accountant;
        private readonly DbSession m_Db;

        public int CachedVouchers
            => m_Accountant.m_Db is Virtualizer v
                ? v.CachedVouchers()
                : throw new InvalidOperationException("The virtualizer has been aborted");

        public override string ToString()
            => m_Accountant.m_Db is Virtualizer v
                ? v.ToString()
                : throw new InvalidOperationException("The virtualizer has been aborted");

        public async ValueTask DisposeAsync()
        {
            if (m_Accountant.m_Db is Virtualizer v)
            {
                await v.DisposeAsync();
                m_Accountant.m_Db = m_Db;
            }
        }

        public async ValueTask Abort()
        {
            if (m_Accountant.m_Db is Virtualizer v)
            {
                v.Abort();
                await v!.DisposeAsync();
                m_Accountant.m_Db = m_Db;
            }
        }
    }

    #region Voucher

    public ValueTask<Voucher> SelectVoucherAsync(string id)
        => m_Db.SelectVoucher(id);

    public IAsyncEnumerable<Voucher> SelectVouchersAsync(IQueryCompounded<IVoucherQueryAtom> query)
        => m_Db.SelectVouchers(query);

    public IAsyncEnumerable<VoucherDetail> SelectVoucherDetailsAsync(IVoucherDetailQuery query)
        => m_Db.SelectVoucherDetails(query);

    public IAsyncEnumerable<Balance> SelectVoucherDetailsGroupedDirectAsync(IGroupedQuery query)
        => m_Db.SelectVoucherDetailsGrouped(query, Limit);

    public ValueTask<ISubtotalResult> SelectVoucherDetailsGroupedAsync(IGroupedQuery query)
    {
        var res = m_Db.SelectVoucherDetailsGrouped(query, Limit);
        var conv = new SubtotalBuilder(query.Subtotal, this);
        return conv.Build(res);
    }

    public IAsyncEnumerable<Balance> SelectVouchersGroupedDirectAsync(IVoucherGroupedQuery query)
        => m_Db.SelectVouchersGrouped(query, Limit);

    public ValueTask<ISubtotalResult> SelectVouchersGroupedAsync(IVoucherGroupedQuery query)
    {
        var res = m_Db.SelectVouchersGrouped(query, Limit);
        var conv = new SubtotalBuilder(query.Subtotal, this);
        return conv.Build(res);
    }

    public IAsyncEnumerable<(Voucher, string, string, double)> SelectUnbalancedVouchersAsync(
        IQueryCompounded<IVoucherQueryAtom> query)
        => m_Db.SelectUnbalancedVouchers(query);

    public IAsyncEnumerable<(Voucher, List<string>)> SelectDuplicatedVouchersAsync(
        IQueryCompounded<IVoucherQueryAtom> query)
        => m_Db.SelectDuplicatedVouchers(query);

    public ValueTask<bool> DeleteVoucherAsync(string id)
        => m_Db.DeleteVoucher(id);

    public ValueTask<long> DeleteVouchersAsync(IQueryCompounded<IVoucherQueryAtom> query)
        => m_Db.DeleteVouchers(query);

    private Voucher Regularize(Voucher entity)
    {
        entity.Details?.ForEach(d => d.User ??= Client.User);
        return entity;
    }

    public ValueTask<bool> UpsertAsync(Voucher entity)
        => m_Db.Upsert(Regularize(entity));

    public ValueTask<long> UpsertAsync(IEnumerable<Voucher> entities)
        => m_Db.Upsert(entities.Select(Regularize));

    #endregion

    #region Asset

    public async ValueTask<Asset> SelectAssetAsync(Guid? id)
        => AssetAccountant.InternalRegular(await m_Db.SelectAsset(id));

    public IAsyncEnumerable<Asset> SelectAssetsAsync(IQueryCompounded<IDistributedQueryAtom> filter)
        => m_Db.SelectAssets(filter).Select(AssetAccountant.InternalRegular);

    public ValueTask<bool> DeleteAssetAsync(Guid id)
        => m_Db.DeleteAsset(id);

    public ValueTask<long> DeleteAssetsAsync(IQueryCompounded<IDistributedQueryAtom> filter)
        => m_Db.DeleteAssets(filter);

    public ValueTask<bool> UpsertAsync(Asset entity)
        => m_Db.Upsert(entity);

    public ValueTask<long> UpsertAsync(IEnumerable<Asset> entities)
        => m_Db.Upsert(entities);

    public IEnumerable<Voucher> RegisterVouchers(Asset asset, DateFilter rng,
        IQueryCompounded<IVoucherQueryAtom> query)
        => m_AssetAccountant.RegisterVouchers(asset, rng, query);

    public static void Depreciate(Asset asset) => AssetAccountant.Depreciate(asset);

    public IAsyncEnumerable<AssetItem> Update(Asset asset, DateFilter rng, bool isCollapsed = false,
        bool editOnly = false)
        => m_AssetAccountant.Update(asset, rng, isCollapsed, editOnly);

    #endregion

    #region Amort

    public async ValueTask<Amortization> SelectAmortizationAsync(Guid? id)
        => AmortAccountant.InternalRegular(await m_Db.SelectAmortization(id));

    public IAsyncEnumerable<Amortization> SelectAmortizationsAsync(IQueryCompounded<IDistributedQueryAtom> filter)
        => m_Db.SelectAmortizations(filter).Select(AmortAccountant.InternalRegular);

    public ValueTask<bool> DeleteAmortizationAsync(Guid id)
        => m_Db.DeleteAmortization(id);

    public ValueTask<long> DeleteAmortizationsAsync(IQueryCompounded<IDistributedQueryAtom> filter)
        => m_Db.DeleteAmortizations(filter);

    public ValueTask<bool> UpsertAsync(Amortization entity)
        => m_Db.Upsert(entity);

    public ValueTask<long> UpsertAsync(IEnumerable<Amortization> entities)
        => m_Db.Upsert(entities);

    public IAsyncEnumerable<Voucher> RegisterVouchers(Amortization amort, DateFilter rng,
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
