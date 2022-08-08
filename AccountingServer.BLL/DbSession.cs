/* Copyright (C) 2020-2022 b1f6c1c4
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
using System.Linq;
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

    /// <summary>
    ///     数据库访问
    /// </summary>
    public AtomicReference<IDbAdapter> Db { private get; set; }

    /// <summary>
    ///     汇率日志
    /// </summary>
    public Action<string, bool> ExchangeLogger { get; set; }

    /// <inheritdoc />
    public ValueTask<double> Query(DateTime? date, string from, string to)
        => LookupExchange(date ?? DateTime.UtcNow, from, to);

    public VirtualizeLock Virtualize()
    {
        Db.Set(Facade.Virtualize(Db.Get()));
        return new(this);
    }

    /// <summary>
    ///     查询汇率
    /// </summary>
    /// <param name="date">日期</param>
    /// <param name="from">购汇币种</param>
    /// <param name="to">结汇币种</param>
    /// <returns>汇率</returns>
    private async ValueTask<double> LookupExchange(DateTime date, string from, string to)
    {
        if (from == to)
            return 1;

        var now = DateTime.UtcNow;
        if (date > now)
            date = now;

        var res = await Db.Get().SelectExchangeRecord(new() { Time = date, From = from, To = to });
        if (res != null)
            return res.Value;
        res = await Db.Get().SelectExchangeRecord(new() { Time = date, From = to, To = from });
        if (res != null)
            return 1D / res.Value;

        var log = $"{now:o} Query: {date:o} {from}/{to}";
        if (ExchangeLogger != null)
            ExchangeLogger(log, false);
        else
            Console.WriteLine(log);
        var value = await ExchangeFactory.Instance.Query(from, to);
        await Db.Get().Upsert(new ExchangeRecord
            {
                Time = now, From = from, To = to, Value = value,
            });
        return value;
    }

    public async ValueTask<double> SaveHistoricalRate(DateTime date, string from, string to)
    {
        var res = await Db.Get().SelectExchangeRecord(new() { Time = date, From = from, To = to });
        if (res != null && res.Time == date)
            return res.Value;
        res = await Db.Get().SelectExchangeRecord(new() { Time = date, From = to, To = from });
        if (res != null && res.Time == date)
            return 1D / res.Value;

        var value = await ExchangeFactory.HistoricalInstance.Query(date, from, to);
        await Db.Get().Upsert(new ExchangeRecord { Time = date, From = from, To = to, Value = value });
        return value;
    }

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

    public async ValueTask<Voucher> SelectVoucher(string id)
        => await Db.Get().SelectVoucher(id);

    public IAsyncEnumerable<Voucher> SelectVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => Db.Get().SelectVouchers(query);

    public IAsyncEnumerable<Voucher> SelectVouchersEmit(IVoucherDetailQuery query)
        => Db.Get().SelectVouchersEmit(query);

    public IAsyncEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query)
        => Db.Get().SelectVoucherDetails(query);

    public ValueTask<ISubtotalResult> SelectVouchersGrouped(IVoucherGroupedQuery query, int limit)
    {
        var res = Db.Get().SelectVouchersGrouped(query, limit);
        var conv = new SubtotalBuilder(query.Subtotal, this);
        return conv.Build(res);
    }

    public ValueTask<ISubtotalResult> SelectVoucherDetailsGrouped(IGroupedQuery query, int limit)
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

    public ValueTask<bool> DeleteVoucher(string id)
        => Db.Get().DeleteVoucher(id);

    public ValueTask<long> DeleteVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => Db.Get().DeleteVouchers(query);

    public ValueTask<bool> Upsert(Voucher entity)
        => Db.Get().Upsert(Regularize(entity));

    public ValueTask<long> Upsert(IEnumerable<Voucher> entities)
        => Db.Get().Upsert(entities.Select(Regularize));

    public static double? Regularize(double? fund)
    {
        const double coercionTolerance = 1e-12;

        if (!fund.HasValue)
            return null;

        var t = Math.Round(fund.Value, -(int)Math.Log10(VoucherDetail.Tolerance));
        var c = Math.Round(fund.Value, -(int)Math.Log10(coercionTolerance));
        return Math.Abs(t - c) < coercionTolerance / 10 ? t : fund;
    }

    public static Voucher Regularize(Voucher entity)
    {
        entity.Date = entity.Date?.Date;
        entity.Details ??= new();

        foreach (var d in entity.Details)
        {
            _ = d.User ?? throw new ApplicationException("User must be specified before passing to DbSession");
            d.Currency = d.Currency.ToUpperInvariant();
            d.Fund = Regularize(d.Fund);
        }

        entity.Details.Sort(TheComparison);
        return entity;
    }

    public ValueTask<Asset> SelectAsset(Guid id)
        => Db.Get().SelectAsset(id);

    public IAsyncEnumerable<Asset> SelectAssets(IQueryCompounded<IDistributedQueryAtom> filter)
        => Db.Get().SelectAssets(filter);

    public ValueTask<bool> DeleteAsset(Guid id)
        => Db.Get().DeleteAsset(id);

    public ValueTask<long> DeleteAssets(IQueryCompounded<IDistributedQueryAtom> filter)
        => Db.Get().DeleteAssets(filter);

    public ValueTask<bool> Upsert(Asset entity)
        => Db.Get().Upsert(entity);

    public ValueTask<Amortization> SelectAmortization(Guid id)
        => Db.Get().SelectAmortization(id);

    public IAsyncEnumerable<Amortization> SelectAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
        => Db.Get().SelectAmortizations(filter);

    public ValueTask<bool> DeleteAmortization(Guid id)
        => Db.Get().DeleteAmortization(id);

    public ValueTask<long> DeleteAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
        => Db.Get().DeleteAmortizations(filter);

    public ValueTask<bool> Upsert(Amortization entity)
    {
        Regularize(entity.Template);

        return Db.Get().Upsert(entity);
    }

    public class VirtualizeLock : IAsyncDisposable
    {
        public VirtualizeLock(DbSession db) => Db = db;

        private DbSession Db { get; }

        public int CachedVouchers => (Db.Db.Get() as Virtualizer)!.CachedVouchers;

        public async ValueTask DisposeAsync()
            => Db.Db.Set(await Facade.UnVirtualize(Db.Db.Get()));
    }
}
