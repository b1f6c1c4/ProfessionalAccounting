/* Copyright (C) 2020-2023 b1f6c1c4
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
using System.Collections;
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

    private class Cache : IEnumerable<Voucher>
    {
        private Dictionary<string, Voucher> m_Modified = new();
        private List<Voucher> m_Created = new();
        private readonly HashSet<string> m_Removed = new();
        public int Count => m_Modified.Count + m_Created.Count + m_Removed.Count;

        public Nothing Upsert(Voucher voucher)
        {
            if (voucher.ID == null)
                m_Created.Add(voucher);
            else
            {
                m_Removed.Remove(voucher.ID);
                m_Modified.Add(voucher.ID, voucher);
            }

            return Nothing.AtAll;
        }

        public bool Delete(string id)
        {
            if (id == null)
                throw new ApplicationException("Cannot delete null ID voucher.");

            var res = m_Modified.Remove(id);
            m_Removed.Add(id);
            return res;
        }

        public long Delete(IEnumerable<string> ids)
            => ids.LongCount(Delete);

        public long Delete(Predicate<Voucher> query)
        {
            var cnt = m_Created.LongCount() + m_Modified.LongCount();
            m_Created = m_Created.Where(v => query(v)).ToList();
            m_Modified = m_Modified.Where(kvp => query(kvp.Value))
                .ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value);
            return m_Created.LongCount() + m_Modified.LongCount() - cnt;
        }

        public IEnumerable<string> Ex => m_Modified.Keys.Concat(m_Removed);

        public (Voucher[], string[]) Materialize()
        {
            var upsert = m_Modified.Values.Concat(m_Created).ToArray();
            var remove = m_Removed.ToArray();
            m_Modified.Clear();
            m_Created.Clear();
            m_Removed.Clear();
            return (upsert, remove);
        }

        public Voucher this[string id]
            => m_Modified[id];

        public IEnumerator<Voucher> GetEnumerator()
            => m_Modified.Values.Concat(m_Created).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    private readonly Cache m_Cache = new();

    internal Virtualizer(IDbAdapter db) => Db = db;

    /// <summary>
    ///     数据库访问
    /// </summary>
    internal IDbAdapter Db { get; init; }

    public int CachedVouchers => ReadLocked(static _ => _.Count);

    public void Abort()
        => WriteLocked(static _ => _.Materialize());

    public async ValueTask DisposeAsync()
    {
        var (u, r) = WriteLocked(static _ => _.Materialize());

        if (u.Length != 0)
            if (await Db.Upsert(u) != u.Length)
                throw new ApplicationException("Cannot write-back voucher cache: upsert");
        if (r.Length != 0)
            if (await Db.DeleteVouchers(r) != r.Length)
                throw new ApplicationException("Cannot write-back voucher cache: remove");
    }

    public async ValueTask<Voucher> SelectVoucher(string id)
        => ReadLocked(_ => _[id]) ?? await Db.SelectVoucher(id);

    public IAsyncEnumerable<Voucher> SelectVouchers(IQueryCompounded<IVoucherQueryAtom> query,
        IEnumerable<string> exclude = null)
        => ReadLocked(_ => _.Where(v => v.IsMatch(query)).ToArray().ToAsyncEnumerable()
            .Concat(Db.SelectVouchers(query, _.Ex)));

    public IAsyncEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query,
        IEnumerable<string> exclude = null)
        => ReadLocked(_ => _.Where(v => v.IsMatch(query.VoucherQuery))
            .SelectMany(static v => v.Details).Where(d => d.IsMatch(query.ActualDetailFilter()))
            .ToArray().ToAsyncEnumerable()
            .Concat(Db.SelectVoucherDetails(query, _.Ex)));

    public IAsyncEnumerable<Balance> SelectVouchersGrouped(IVoucherGroupedQuery query, int limit,
        IEnumerable<string> exclude = null)
    {
        if (limit != 0)
            throw new NotSupportedException();

        var level = query.Subtotal.PreprocessVoucher();
        return ReadLocked(_ => Merge(_.Where(v => v.IsMatch(query.VoucherQuery))
            .Select(v => new Balance { Date = v.Date.Project(level), Fund = 1 })
            .ToArray().ToAsyncEnumerable()
            .Concat(Db.SelectVouchersGrouped(query, limit, _.Ex))));
    }

    public IAsyncEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query, int limit,
        IEnumerable<string> exclude = null)
    {
        if (limit != 0)
            throw new NotSupportedException();
        var level = query.Subtotal.PreprocessDetail();
        var fluent = ReadLocked(_ => Merge(_.Where(v => v.IsMatch(query.VoucherEmitQuery.VoucherQuery))
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
                    Value = level.HasFlag(SubtotalLevel.Value)
                        ? Math.Round(d.Fund!.Value, -(int)Math.Log10(VoucherDetail.Tolerance))
                        : null,
                    Fund = query.Subtotal.GatherType == GatheringType.Count ? 1 : d.Fund!.Value,
                })
            .ToArray().ToAsyncEnumerable()
            .Concat(Db.SelectVoucherDetailsGrouped(query))));
        return query.Subtotal.ShouldAvoidZero() ? fluent.Where(static b => !b.Fund.IsZero()) : fluent;
    }

    public IAsyncEnumerable<(Voucher, string, string, double)> SelectUnbalancedVouchers(
        IQueryCompounded<IVoucherQueryAtom> query)
        => throw new NotSupportedException();

    public IAsyncEnumerable<(Voucher, List<string>)> SelectDuplicatedVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => throw new NotSupportedException();

    public ValueTask<bool> DeleteVoucher(string id)
        => new(WriteLocked(_ => _.Delete(id)));

    public ValueTask<long> DeleteVouchers(IEnumerable<string> ids)
        => new( WriteLocked(_ => _.Delete(ids)));

    public ValueTask<long> DeleteVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => new(WriteLocked(
            _ => _.Delete(v => v.IsMatch(query))
                + _.Delete(Db.SelectVouchers(query, _.Ex).Select(static v => v.ID).ToEnumerable())));

    public ValueTask<bool> Upsert(Voucher entity)
    {
        WriteLocked(_ => _.Upsert(entity));
        return new(true);
    }

    public ValueTask<long> Upsert(IEnumerable<Voucher> entities)
    {
        var cnt = 0L;
        foreach (var voucher in entities)
        {
            WriteLocked(_ => _.Upsert(voucher));
            cnt++;
        }

        return new(cnt);
    }

    #region others

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

    #endregion

    private T ReadLocked<T>(Func<Cache, T> func)
    {
        m_Lock.EnterReadLock();
        try
        {
            return func(m_Cache);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    private T WriteLocked<T>(Func<Cache, T> func)
    {
        m_Lock.EnterWriteLock();
        try
        {
            return func(m_Cache);
        }
        finally
        {
            m_Lock.ExitWriteLock();
        }
    }

    private static IAsyncEnumerable<Balance> Merge(IAsyncEnumerable<Balance> balances)
        => balances.GroupByAwait(
            static b => new(b),
            static b => new ValueTask<double>(b.Fund),
            static async (b, fs) =>
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
                x.Content == y.Content && x.Remark == y.Remark && x.Currency == y.Currency && x.User == y.User &&
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                x.Value == y.Value;
        }

        public int GetHashCode(Balance obj)
            => HashCode.Combine(obj.Date, obj.Title, obj.SubTitle, obj.Content, obj.Remark, obj.Currency, obj.User);
    }
}
