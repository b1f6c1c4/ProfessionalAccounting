/* Copyright (C) 2020-2024 b1f6c1c4
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

namespace AccountingServer.BLL;

// ReSharper disable MemberCanBePrivate.Global
public class DbSession : IHistoricalExchange
{
    internal DbSession(IDbAdapter db)
        => Db = db;

    public DbSession(string uri = null, string db = null)
        => Db = Facade.Create(uri, db);

    /// <summary>
    ///     数据库访问
    /// </summary>
    internal IDbAdapter Db { get; init; }

    #region exchange

    /// <summary>
    ///     汇率日志
    /// </summary>
    public Action<string, bool> ExchangeLogger { get; set; }

    /// <inheritdoc />
    public ValueTask<double> Query(DateTime? date, string from, string to)
        => LookupExchange(date ?? DateTime.UtcNow, from, to);

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

        var res = await Db.SelectExchangeRecord(new() { Time = date, From = from, To = to });
        if (res != null)
            return res.Value;
        res = await Db.SelectExchangeRecord(new() { Time = date, From = to, To = from });
        if (res != null)
            return 1D / res.Value;

        var log = $"{now:o} Query: {date:o} {from}/{to}";
        if (ExchangeLogger != null)
            ExchangeLogger(log, false);
        else
            Console.WriteLine(log);
        var value = await ExchangeFactory.Instance.Query(from, to);
        await Db.Upsert(new ExchangeRecord
            {
                Time = now, From = from, To = to, Value = value,
            });
        return value;
    }

    internal async ValueTask<double> SaveHistoricalRate(DateTime date, string from, string to)
    {
        var res = await Db.SelectExchangeRecord(new() { Time = date, From = from, To = to });
        if (res != null && res.Time == date)
            return res.Value;
        res = await Db.SelectExchangeRecord(new() { Time = date, From = to, To = from });
        if (res != null && res.Time == date)
            return 1D / res.Value;

        var value = await ExchangeFactory.HistoricalInstance.Query(date, from, to);
        await Db.Upsert(new ExchangeRecord { Time = date, From = from, To = to, Value = value });
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

    #endregion

    public virtual ValueTask<Voucher> SelectVoucher(string id)
        => Db.SelectVoucher(id);

    public virtual IAsyncEnumerable<Voucher> SelectVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => Db.SelectVouchers(query);

    public virtual IAsyncEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query)
        => Db.SelectVoucherDetails(query);

    public virtual IAsyncEnumerable<Balance> SelectVouchersGrouped(IVoucherGroupedQuery query, int limit)
        => Db.SelectVouchersGrouped(query, limit);

    public virtual IAsyncEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query, int limit)
        => Db.SelectVoucherDetailsGrouped(query, limit);

    public virtual IAsyncEnumerable<(Voucher, string, string, double)> SelectUnbalancedVouchers(
        IQueryCompounded<IVoucherQueryAtom> query)
        => Db.SelectUnbalancedVouchers(query);

    public virtual IAsyncEnumerable<(Voucher, List<string>)> SelectDuplicatedVouchers(
        IQueryCompounded<IVoucherQueryAtom> query)
        => Db.SelectDuplicatedVouchers(query);

    public virtual ValueTask<bool> DeleteVoucher(string id)
        => Db.DeleteVoucher(id);

    public virtual ValueTask<long> DeleteVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => Db.DeleteVouchers(query);

    public virtual ValueTask<bool> Upsert(Voucher entity)
        => Db.Upsert(Regularize(entity));

    public virtual ValueTask<long> Upsert(IEnumerable<Voucher> entities)
        => Db.Upsert(entities.Select(Regularize));

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
            _ = d.Currency ?? throw new ApplicationException("Currency must be specified before passing to DbSession");
            if (!d.Currency.EndsWith('#'))
                d.Currency = d.Currency.ToUpperInvariant();
            d.Fund = Regularize(d.Fund);
        }

        entity.Details.Sort(TheComparison);
        return entity;
    }

    public ValueTask<Asset> SelectAsset(Guid id)
        => Db.SelectAsset(id);

    public IAsyncEnumerable<Asset> SelectAssets(IQueryCompounded<IDistributedQueryAtom> filter)
        => Db.SelectAssets(filter);

    public ValueTask<bool> DeleteAsset(Guid id)
        => Db.DeleteAsset(id);

    public ValueTask<long> DeleteAssets(IQueryCompounded<IDistributedQueryAtom> filter)
        => Db.DeleteAssets(filter);

    public ValueTask<bool> Upsert(Asset entity)
        => Db.Upsert(entity);

    public ValueTask<Amortization> SelectAmortization(Guid id)
        => Db.SelectAmortization(id);

    public IAsyncEnumerable<Amortization> SelectAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
        => Db.SelectAmortizations(filter);

    public ValueTask<bool> DeleteAmortization(Guid id)
        => Db.DeleteAmortization(id);

    public ValueTask<long> DeleteAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
        => Db.DeleteAmortizations(filter);

    public ValueTask<bool> Upsert(Amortization entity)
    {
        Regularize(entity.Template);

        return Db.Upsert(entity);
    }

    public ValueTask<bool> StartProfiler(int slow)
        => Db.StartProfiler(slow);

    public IAsyncEnumerable<string> StopProfiler()
        => Db.StopProfiler();
}
