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

    private static async IAsyncEnumerable<T> E<T>()
    {
        yield break;
    }

    public virtual async ValueTask<Voucher> SelectVoucher(string id)
    {
        if (id == null)
            return null;

        return await Db.SelectVoucher(id);
    }

    public virtual IAsyncEnumerable<Voucher> SelectVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => query == null ? E<Voucher>() : Db.SelectVouchers(query);

    public virtual IAsyncEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query)
        => query == null ? E<VoucherDetail>() : Db.SelectVoucherDetails(query);

    public virtual IAsyncEnumerable<Balance> SelectVouchersGrouped(IVoucherGroupedQuery query, int limit)
        => query == null || query.VoucherQuery == null ? E<Balance>() : Db.SelectVouchersGrouped(query, limit);

    public virtual IAsyncEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query, int limit)
        => query == null || query.VoucherEmitQuery == null || query.VoucherEmitQuery.VoucherQuery == null ? E<Balance>()
            : Db.SelectVoucherDetailsGrouped(query, limit);

    public virtual IAsyncEnumerable<(Voucher, string, string, double)> SelectUnbalancedVouchers(
        IQueryCompounded<IVoucherQueryAtom> query)
        => query == null ? E<(Voucher, string, string, double)>() : Db.SelectUnbalancedVouchers(query);

    public virtual IAsyncEnumerable<(Voucher, List<string>)> SelectDuplicatedVouchers(
        IQueryCompounded<IVoucherQueryAtom> query)
        => query == null ? E<(Voucher, List<string>)>() : Db.SelectDuplicatedVouchers(query);

    public virtual ValueTask<bool> DeleteVoucher(string id)
        => Db.DeleteVoucher(id);

    public virtual async ValueTask<long> DeleteVouchers(IQueryCompounded<IVoucherQueryAtom> query)
    {
        if (query == null)
            return 0;

        return await Db.DeleteVouchers(query);
    }

    public virtual ValueTask<bool> Upsert(Voucher entity)
        => Db.Upsert(Regularize(entity));

    public virtual ValueTask<long> Upsert(IEnumerable<Voucher> entities)
        => Db.Upsert(entities.Select(Regularize));

    public static double? Regularize(double? fund)
    {
        const double coercionTolerance4 = 1e-12;
        const double coercionTolerance5 = 1e-9;

        if (!fund.HasValue)
            return null;

        var t4 = Math.Round(fund.Value, -(int)Math.Log10(VoucherDetail.Tolerance));
        var c4 = Math.Round(fund.Value, -(int)Math.Log10(coercionTolerance4));
        var t5 = Math.Round(fund.Value, -(int)Math.Log10(1e-4));
        var c5 = Math.Round(fund.Value, -(int)Math.Log10(coercionTolerance5));
        return Math.Abs(t4 - c4) < coercionTolerance4 / 10 ? t4 :
            Math.Abs(t5 - c5) < coercionTolerance5 / 10 ? t5 : fund;
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

    public async ValueTask<Asset> SelectAsset(Guid? id)
        => id.HasValue ? await Db.SelectAsset(id.Value) : null;

    public IAsyncEnumerable<Asset> SelectAssets(IQueryCompounded<IDistributedQueryAtom> filter)
        => filter == null ? E<Asset>() : Db.SelectAssets(filter);

    public ValueTask<bool> DeleteAsset(Guid id)
        => Db.DeleteAsset(id);

    public async ValueTask<long> DeleteAssets(IQueryCompounded<IDistributedQueryAtom> filter)
    {
        if (filter == null)
            return 0;

        return await Db.DeleteAssets(filter);
    }

    public ValueTask<bool> Upsert(Asset entity)
        => Db.Upsert(entity);

    public ValueTask<long> Upsert(IEnumerable<Asset> entities)
        => Db.Upsert(entities);

    public async ValueTask<Amortization> SelectAmortization(Guid? id)
        => id.HasValue ? await Db.SelectAmortization(id.Value) : null;

    public IAsyncEnumerable<Amortization> SelectAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
        => filter == null ? E<Amortization>() : Db.SelectAmortizations(filter);

    public ValueTask<bool> DeleteAmortization(Guid id)
        => Db.DeleteAmortization(id);

    public async ValueTask<long> DeleteAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
    {
        if (filter == null)
            return 0;

        return await Db.DeleteAmortizations(filter);
    }

    public ValueTask<bool> Upsert(Amortization entity)
    {
        Regularize(entity.Template);

        return Db.Upsert(entity);
    }

    public ValueTask<long> Upsert(IEnumerable<Amortization> entities)
        => Db.Upsert(entities.Select(static (entity) => {
                    Regularize(entity.Template);
                    return entity;
                }));

    public ValueTask<Authn> SelectAuth(byte[] id)
        => Db.SelectAuth(id);

    public IAsyncEnumerable<Authn> SelectAuths(string identityName)
        => Db.SelectAuths(identityName);

    public ValueTask<bool> DeleteAuth(byte[] id)
        => Db.DeleteAuth(id);

    public ValueTask<WebAuthn> SelectWebAuthn(byte[] credentialId)
        => Db.SelectWebAuthn(credentialId);

    public ValueTask<CertAuthn> SelectCertAuthn(string fingerprint)
        => Db.SelectCertAuthn(fingerprint);

    public ValueTask Insert(Authn aid)
        => Db.Insert(aid);

    public ValueTask<bool> Update(Authn aid)
        => Db.Update(aid);

    public ValueTask<bool> StartProfiler(int slow)
        => Db.StartProfiler(slow);

    public IAsyncEnumerable<string> StopProfiler()
        => Db.StopProfiler();
}
