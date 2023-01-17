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
using System.Threading.Tasks;
using AccountingServer.DAL;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL;

internal class Virtualizer : DbSession, IAsyncDisposable
{
    internal Virtualizer(IDbAdapter db) : base(db) { }

    private class Cache : IEnumerable<Voucher>
    {
        private Dictionary<string, Voucher> m_Modified = new();
        private List<Voucher> m_Created = new();
        private readonly HashSet<string> m_Removed = new();
        public int Count => m_Modified.Count + m_Created.Count + m_Removed.Count;

        public bool Upsert(Voucher voucher)
        {
            if (voucher.ID == null)
                m_Created.Add(new(voucher));
            else
            {
                m_Removed.Remove(voucher.ID);
                m_Modified.Add(voucher.ID, new(voucher));
            }

            return true;
        }

        public bool Delete(string id, bool hasExternal)
        {
            if (id == null)
                throw new ApplicationException("Cannot delete null ID voucher.");

            var res = m_Modified.Remove(id);
            if (!res && !hasExternal)
                return false;

            m_Removed.Add(id);
            return true;
        }

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
            => m_Modified.ContainsKey(id) ? m_Modified[id] : null;

        public IEnumerator<Voucher> GetEnumerator()
            => m_Modified.Values.Concat(m_Created).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    private readonly Cache m_Cache = new();

    public int CachedVouchers => m_Cache.Count;

    public void Abort()
        => m_Cache.Materialize();

    public async ValueTask DisposeAsync()
    {
        var (u, r) = m_Cache.Materialize();

        if (u.Length != 0)
            if (await Db.Upsert(u) != u.Length)
                throw new ApplicationException("Cannot write-back voucher cache: upsert");
        if (r.Length != 0)
            if (await Db.DeleteVouchers(r) != r.Length)
                throw new ApplicationException("Cannot write-back voucher cache: remove");
    }

    private static async IAsyncEnumerable<T> J<T>(IEnumerable<T> lhs, IAsyncEnumerable<T> rhs)
    {
        var lst = await rhs.ToListAsync();
        lst.AddRange(lhs);
        foreach (var item in lst)
            yield return item;
    }

    public override async ValueTask<Voucher> SelectVoucher(string id)
        => m_Cache[id] ?? (m_Cache.Ex.Contains(id) ? null : await Db.SelectVoucher(id));

    public override IAsyncEnumerable<Voucher> SelectVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => J(m_Cache.Where(v => v.IsMatch(query)),
            Db.SelectVouchers(query, m_Cache.Ex));

    public override IAsyncEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query)
        => J(m_Cache.Where(v => v.IsMatch(query.VoucherQuery))
                .SelectMany(static v => v.Details).Where(d => d.IsMatch(query.ActualDetailFilter())),
            Db.SelectVoucherDetails(query, m_Cache.Ex));

    public override IAsyncEnumerable<Balance> SelectVouchersGrouped(IVoucherGroupedQuery query, int limit)
    {
        if (limit != 0)
            throw new NotSupportedException();

        var level = query.Subtotal.PreprocessVoucher();
        return Merge(J(m_Cache.Where(v => v.IsMatch(query.VoucherQuery))
                .Select(v => new Balance { Date = v.Date.Project(level), Fund = 1 }),
            Db.SelectVouchersGrouped(query, limit, m_Cache.Ex)));
    }

    public override IAsyncEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query, int limit)
    {
        if (limit != 0)
            throw new NotSupportedException();
        var level = query.Subtotal.PreprocessDetail();
        var fluent = Merge(J(m_Cache.Where(v => v.IsMatch(query.VoucherEmitQuery.VoucherQuery))
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
                    }),
            Db.SelectVoucherDetailsGrouped(query)));
        return query.Subtotal.ShouldAvoidZero() ? fluent.Where(static b => !b.Fund.IsZero()) : fluent;
    }

    public override IAsyncEnumerable<(Voucher, string, string, double)> SelectUnbalancedVouchers(
        IQueryCompounded<IVoucherQueryAtom> query)
        => throw new NotSupportedException();

    public override IAsyncEnumerable<(Voucher, List<string>)> SelectDuplicatedVouchers(
        IQueryCompounded<IVoucherQueryAtom> query)
        => throw new NotSupportedException();

    public override async ValueTask<bool> DeleteVoucher(string id)
        => m_Cache.Delete(id, await Db.SelectVoucher(id) != null);

    public override async ValueTask<long> DeleteVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => m_Cache.Delete(v => v.IsMatch(query))
            + await Db.SelectVouchers(query, m_Cache.Ex).LongCountAsync(v => m_Cache.Delete(v.ID, true));

    public override ValueTask<bool> Upsert(Voucher entity)
        => new(m_Cache.Upsert(Regularize(entity)));

    public override ValueTask<long> Upsert(IEnumerable<Voucher> entities)
    {
        var cnt = 0L;
        foreach (var voucher in entities)
        {
            m_Cache.Upsert(Regularize(voucher));
            cnt++;
        }

        return new(cnt);
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
