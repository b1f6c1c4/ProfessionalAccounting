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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.DAL;

public class Virtualizer : IDbAdapter, IAsyncDisposable
{
    private readonly ReaderWriterLockSlim m_Lock = new();
    private readonly List<Voucher> m_Vouchers = new();

    internal Virtualizer(IDbAdapter db) => Db = db;

    /// <summary>
    ///     数据库访问
    /// </summary>
    internal IDbAdapter Db { get; init; }

    public int CachedVouchers => ReadLocked(static _ => _.Count);

    public async ValueTask DisposeAsync()
    {
        var a = WriteLocked(_ =>
            {
                var a = m_Vouchers.ToArray();
                _.Clear();
                return a;
            });

        if (a.Length == 0)
            return;

        if (await Db.Upsert(a) != a.Length)
            throw new ApplicationException("Cannot write-back voucher cache");
    }

    public ValueTask<Voucher> SelectVoucher(string id)
        => Db.SelectVoucher(id);

    public IAsyncEnumerable<Voucher> SelectVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => Db.SelectVouchers(query).Concat(ReadLocked(_ => _.Where(v => v.IsMatch(query))).ToAsyncEnumerable());

    public IAsyncEnumerable<Voucher> SelectVouchersEmit(IVoucherDetailQuery query)
        => Db.SelectVouchersEmit(query).Concat(ReadLocked(_ => _.Where(v => v.IsMatch(query.VoucherQuery)).Select(v =>
                {
                    var v2 = new Voucher(v);
                    v2.Details.RemoveAll(d => !d.IsMatch(query.ActualDetailFilter()));
                    return v2;
                })).ToAsyncEnumerable());

    public IAsyncEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query)
        => Db.SelectVoucherDetails(query).Concat(ReadLocked(_ => _.Where(v => v.IsMatch(query.VoucherQuery)))
            .SelectMany(static v => v.Details).Where(d => d.IsMatch(query.ActualDetailFilter())).ToAsyncEnumerable());

    public IAsyncEnumerable<Balance> SelectVouchersGrouped(IVoucherGroupedQuery query, int limit)
    {
        if (limit != 0)
            throw new NotSupportedException();
        var level = query.Subtotal.PreprocessVoucher();
        return Merge(Db.SelectVouchersGrouped(query).Concat(ReadLocked(_ => _.Where(v => v.IsMatch(query.VoucherQuery)))
            .Select(v => new Balance { Date = v.Date.Project(level), Fund = 1 }).ToAsyncEnumerable()));
    }

    public IAsyncEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query, int limit)
    {
        if (limit != 0)
            throw new NotSupportedException();
        var level = query.Subtotal.PreprocessDetail();
        var fluent = Merge(Db.SelectVoucherDetailsGrouped(query).Concat(
            ReadLocked(_ => _.Where(v => v.IsMatch(query.VoucherEmitQuery.VoucherQuery)))
                .SelectMany(static v => v.Details.Select(d => new VoucherDetailR(v, d)))
                .Where(d => d.IsMatch(query.VoucherEmitQuery.ActualDetailFilter()))
                .Select(d => new Balance
                    {
                        Date = d.Voucher.Date.Project(level),
                        User = level.HasFlag(SubtotalLevel.User) ? d.User : null,
                        Currency = level.HasFlag(SubtotalLevel.Currency) ? d.Currency : null,
                        Title = level.HasFlag(SubtotalLevel.Title) ? d.Title : null,
                        SubTitle = level.HasFlag(SubtotalLevel.SubTitle) ? d.SubTitle : null,
                        Content = level.HasFlag(SubtotalLevel.Content) ? d.Content : null,
                        Remark = level.HasFlag(SubtotalLevel.Remark) ? d.Remark : null,
                        Value = level.HasFlag(SubtotalLevel.Value) ? Math.Round(d.Fund!.Value, -(int)Math.Log10(VoucherDetail.Tolerance)) : null,
                        Fund = query.Subtotal.GatherType == GatheringType.Count ? 1 : d.Fund!.Value,
                    }).ToAsyncEnumerable()));
        return query.Subtotal.ShouldAvoidZero() ? fluent.Where(static b => !b.Fund.IsZero()) : fluent;
    }

    public IAsyncEnumerable<(Voucher, string, string, double)> SelectUnbalancedVouchers(
        IQueryCompounded<IVoucherQueryAtom> query)
        => throw new NotSupportedException();

    public IAsyncEnumerable<(Voucher, List<string>)> SelectDuplicatedVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => throw new NotSupportedException();

    public ValueTask<bool> DeleteVoucher(string id)
        => Db.DeleteVoucher(id);

    public async ValueTask<long> DeleteVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => await Db.DeleteVouchers(query) + WriteLocked(_ => _.RemoveAll(v => v.IsMatch(query)));

    public async ValueTask<bool> Upsert(Voucher entity)
    {
        if (entity.ID != null)
            return await Db.Upsert(entity);

        WriteLocked(_ =>
            {
                _.Add(entity);
                return Nothing.AtAll;
            });
        return true;
    }

    public async ValueTask<long> Upsert(IEnumerable<Voucher> entities)
    {
        var lst = new List<Voucher>();
        var cnt = 0L;
        foreach (var voucher in entities)
            if (voucher.ID != null)
            {
                lst.Add(voucher);
                cnt++;
            }
            else
                WriteLocked(_ =>
                    {
                        _.Add(voucher);
                        return Nothing.AtAll;
                    });

        return cnt + await Db.Upsert(lst);
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
        => Db.Upsert(entity);

    public ValueTask<ExchangeRecord> SelectExchangeRecord(ExchangeRecord record)
        => Db.SelectExchangeRecord(record);

    public ValueTask<bool> Upsert(ExchangeRecord record)
        => Db.Upsert(record);

    private T ReadLocked<T>(Func<List<Voucher>, T> func)
    {
        m_Lock.EnterReadLock();
        try
        {
            return func(m_Vouchers);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    private T WriteLocked<T>(Func<List<Voucher>, T> func)
    {
        m_Lock.EnterWriteLock();
        try
        {
            return func(m_Vouchers);
        }
        finally
        {
            m_Lock.ExitWriteLock();
        }
    }

    private static IAsyncEnumerable<Balance> Merge(IAsyncEnumerable<Balance> balances)
        => balances.GroupByAwait(static b => new(b), static b => new ValueTask<double>(b.Fund), static async (b, fs) =>
            {
                b.Fund = await fs.SumAsync(static f => f);
                return b;
            }, new BalanceComparer());

    private class BalanceComparer : IEqualityComparer<Balance>
    {
        public bool Equals(Balance x, Balance y)
        {
            if (x == y)
                return true;
            if (x == null || y == null)
                return false;
            return Nullable.Equals(x.Date, y.Date) && x.Title == y.Title && x.SubTitle == y.SubTitle &&
                x.Content == y.Content && x.Remark == y.Remark && x.Currency == y.Currency && x.User == y.User && x.Value == y.Value;
        }

        public int GetHashCode(Balance obj)
            => HashCode.Combine(obj.Date, obj.Title, obj.SubTitle, obj.Content, obj.Remark, obj.Currency, obj.User);
    }
}
