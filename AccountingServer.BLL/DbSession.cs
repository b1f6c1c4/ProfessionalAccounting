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
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AccountingServer.BLL.Util;
using AccountingServer.DAL;
using AccountingServer.Entities;
using Antlr4.Runtime.Sharpen;

// ReSharper disable MemberCanBePrivate.Global

namespace AccountingServer.BLL;

public class DbSession : IHistoricalExchange
{
    public DbSession(string uri = null, string db = null) => Db = new(Facade.Create(uri, db));

    public VirtualizeLock Virtualize()
    {
        Db.Set(Facade.Virtualize(Db.Get()));
        return new VirtualizeLock(this);
    }

    public class VirtualizeLock : IDisposable
    {
        public VirtualizeLock(DbSession db) => Db = db;

        private DbSession Db { get; }

        public int CachedVouchers => (Db.Db.Get() as Virtualizer)!.CachedVouchers;

        public void Dispose()
            => Db.Db.Set(Facade.UnVirtualize(Db.Db.Get()));
    }

    /// <summary>
    ///     数据库访问
    /// </summary>
    public AtomicReference<IDbAdapter> Db { private get; set; }

    /// <summary>
    ///     查询汇率
    /// </summary>
    /// <param name="date">日期</param>
    /// <param name="from">购汇币种</param>
    /// <param name="to">结汇币种</param>
    /// <returns>汇率</returns>
    private async Task<double> LookupExchange(DateTime date, string from, string to)
    {
        if (from == to)
            return 1;

        var now = DateTime.UtcNow;
        if (date > now)
            date = now;

        var res = await Db.Get().SelectExchangeRecord(new ExchangeRecord { Time = date, From = from, To = to });
        if (res != null)
            return res.Value;
        res = await Db.Get().SelectExchangeRecord(new ExchangeRecord { Time = date, From = to, To = from });
        if (res != null)
            return 1D / res.Value;

        Console.WriteLine($"{now:o} Query: {date:o} {from}/{to}");
        var value = ExchangeFactory.Instance.Query(from, to).Result;
        await Db.Get().Upsert(new ExchangeRecord
            {
                Time = now, From = from, To = to, Value = value,
            });
        return value;
    }

    public async Task<double> SaveHistoricalRate(DateTime date, string from, string to)
    {
        var res = await Db.Get().SelectExchangeRecord(new ExchangeRecord { Time = date, From = from, To = to });
        if (res != null && res.Time == date)
            return res.Value;
        res = await Db.Get().SelectExchangeRecord(new ExchangeRecord { Time = date, From = to, To = from });
        if (res != null && res.Time == date)
            return 1D / res.Value;

        var value = await ExchangeFactory.HistoricalInstance.Query(date, from, to);
        await Db.Get().Upsert(new ExchangeRecord { Time = date, From = from, To = to, Value = value });
        return value;
    }

    /// <inheritdoc />
    public Task<double> Query(DateTime? date, string from, string to)
        => LookupExchange(date ?? DateTime.UtcNow, from, to);

    private static int Compare<T>(T? lhs, T? rhs)
        where T : struct, IComparable<T>
    {
        if (lhs.HasValue &&
            rhs.HasValue)
            return lhs.Value.CompareTo(rhs.Value);

        if (lhs.HasValue)
            return 1;

        if (rhs.HasValue)
            return -1;

        return 0;
    }

    [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
    public static int TheComparison(VoucherDetail lhs, VoucherDetail rhs)
    {
        // ReSharper disable once JoinDeclarationAndInitializer
        int res;

        res = string.Compare(lhs.User, rhs.User, StringComparison.Ordinal);
        if (res != 0)
            return res;

        res = string.Compare(lhs.Currency, rhs.Currency, StringComparison.Ordinal);
        if (res != 0)
            return res;

        res = Compare(lhs.Title, rhs.Title);
        if (res != 0)
            return res;

        res = Compare(lhs.SubTitle, rhs.SubTitle);
        if (res != 0)
            return res;

        res = string.Compare(lhs.Content, rhs.Content, StringComparison.Ordinal);
        if (res != 0)
            return res;

        res = string.Compare(lhs.Remark, rhs.Remark, StringComparison.Ordinal);
        if (res != 0)
            return res;

        res = lhs.Fund.Value.CompareTo(rhs.Fund.Value);
        return res != 0 ? res : 0;
    }

    public async Task<Voucher> SelectVoucher(string id)
        => await Db.Get().SelectVoucher(id);

    public IAsyncEnumerable<Voucher> SelectVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => Db.Get().SelectVouchers(query);

    public IAsyncEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query)
        => Db.Get().SelectVoucherDetails(query);

    public Task<ISubtotalResult> SelectVouchersGrouped(IVoucherGroupedQuery query, int limit)
    {
        var res = Db.Get().SelectVouchersGrouped(query, limit);
        var conv = new SubtotalBuilder(query.Subtotal, this);
        return conv.Build(res);
    }

    public Task<ISubtotalResult> SelectVoucherDetailsGrouped(IGroupedQuery query, int limit)
    {
        var res = Db.Get().SelectVoucherDetailsGrouped(query, limit);
        var conv = new SubtotalBuilder(query.Subtotal, this);
        return conv.Build(res);
    }

    public IAsyncEnumerable<(Voucher, string, string, double)> SelectUnbalancedVouchers(
        IQueryCompounded<IVoucherQueryAtom> query)
        => Db.Get().SelectUnbalancedVouchers(query);

    public IAsyncEnumerable<(Voucher, List<string>)> SelectDuplicatedVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => Db.Get().SelectDuplicatedVouchers(query);

    public Task<bool> DeleteVoucher(string id)
        => Db.Get().DeleteVoucher(id);

    public Task<long> DeleteVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => Db.Get().DeleteVouchers(query);

    public Task<bool> Upsert(Voucher entity)
    {
        Regularize(entity);

        return Db.Get().Upsert(entity);
    }

    public Task<long> Upsert(IReadOnlyCollection<Voucher> entities)
    {
        foreach (var entity in entities)
            Regularize(entity);

        return Db.Get().Upsert(entities);
    }

    public static double? Regularize(double? fund)
    {
        const double coercionTolerance = 1e-12;

        if (!fund.HasValue)
            return null;

        var t = Math.Round(fund.Value, -(int)Math.Log10(VoucherDetail.Tolerance));
        var c = Math.Round(fund.Value, -(int)Math.Log10(coercionTolerance));
        return Math.Abs(t - c) < coercionTolerance / 10 ? t : fund;
    }

    public static void Regularize(Voucher entity)
    {
        entity.Details ??= new();

        foreach (var d in entity.Details)
        {
            d.User ??= "anonymous";
            d.Currency = d.Currency.ToUpperInvariant();
            d.Fund = Regularize(d.Fund);
        }

        entity.Details.Sort(TheComparison);
    }

    public Task<Asset> SelectAsset(Guid id)
        => Db.Get().SelectAsset(id);

    public IAsyncEnumerable<Asset> SelectAssets(IQueryCompounded<IDistributedQueryAtom> filter)
        => Db.Get().SelectAssets(filter);

    public Task<bool> DeleteAsset(Guid id)
        => Db.Get().DeleteAsset(id);

    public Task<long> DeleteAssets(IQueryCompounded<IDistributedQueryAtom> filter)
        => Db.Get().DeleteAssets(filter);

    public Task<bool> Upsert(Asset entity)
        => Db.Get().Upsert(entity);

    public Task<Amortization> SelectAmortization(Guid id)
        => Db.Get().SelectAmortization(id);

    public IAsyncEnumerable<Amortization> SelectAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
        => Db.Get().SelectAmortizations(filter);

    public Task<bool> DeleteAmortization(Guid id)
        => Db.Get().DeleteAmortization(id);

    public Task<long> DeleteAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
        => Db.Get().DeleteAmortizations(filter);

    public Task<bool> Upsert(Amortization entity)
    {
        Regularize(entity.Template);

        return Db.Get().Upsert(entity);
    }
}
