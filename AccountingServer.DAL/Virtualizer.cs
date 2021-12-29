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
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.DAL;

/// <summary>
///
/// </summary>
public class Virtualizer : IDbAdapter, IDisposable
{
    /// <summary>
    ///     数据库访问
    /// </summary>
    internal IDbAdapter Db { get; init; }

    private readonly List<Voucher> m_Vouchers = new();

    internal Virtualizer(IDbAdapter db) => Db = db;

    public int CachedVouchers => m_Vouchers.Count;

    public void Dispose()
    {
        if (m_Vouchers.Count == 0)
            return;

        if (Db.Upsert(m_Vouchers).Result != m_Vouchers.Count)
            throw new ApplicationException("Cannot write-back voucher cache");

        m_Vouchers.Clear();
    }

    public ValueTask<Voucher> SelectVoucher(string id)
        => Db.SelectVoucher(id);

    public IAsyncEnumerable<Voucher> SelectVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => Db.SelectVouchers(query).Concat(m_Vouchers.Where(v => v.IsMatch(query)).ToAsyncEnumerable());

    public IAsyncEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query)
        => Db.SelectVoucherDetails(query).Concat(m_Vouchers.Where(v => v.IsMatch(query.VoucherQuery))
            .SelectMany(v => v.Details).Where(d => d.IsMatch(query.ActualDetailFilter())).ToAsyncEnumerable());

    private class BalanceComparer : IEqualityComparer<Balance>
    {
        public bool Equals(Balance x, Balance y)
        {
            if (x == y)
                return true;
            if (x == null || y == null)
                return false;
            return Nullable.Equals(x.Date, y.Date) && x.Title == y.Title && x.SubTitle == y.SubTitle &&
                x.Content == y.Content && x.Remark == y.Remark && x.Currency == y.Currency && x.User == y.User;
        }

        public int GetHashCode(Balance obj)
            => HashCode.Combine(obj.Date, obj.Title, obj.SubTitle, obj.Content, obj.Remark, obj.Currency, obj.User);
    }

    private static IAsyncEnumerable<Balance> Merge(IAsyncEnumerable<Balance> balances)
        => balances.GroupByAwait(b => new ValueTask<Balance>(b), b => new ValueTask<double>(b.Fund),
            async (b, fs) =>
                {
                    b.Fund = await fs.SumAsync(f => f);
                    return b;
                }, new BalanceComparer());

    private static DateTime? ProjectDate(DateTime? dt, SubtotalLevel level)
    {
        if (!dt.HasValue)
            return null;
        if (!level.HasFlag(SubtotalLevel.Day))
            return null;
        if (!level.HasFlag(SubtotalLevel.Week))
            return dt;
        if (level.HasFlag(SubtotalLevel.Year))
            return new(dt!.Value.Year, 1, 1);
        if (level.HasFlag(SubtotalLevel.Month))
            return new(dt!.Value.Year, dt!.Value.Month, 1);
        // if (level.HasFlag(SubtotalLevel.Week))
        return dt.Value.DayOfWeek switch
            {
                DayOfWeek.Monday => dt.Value.AddDays(-0),
                DayOfWeek.Tuesday => dt.Value.AddDays(-1),
                DayOfWeek.Wednesday => dt.Value.AddDays(-2),
                DayOfWeek.Thursday => dt.Value.AddDays(-3),
                DayOfWeek.Friday => dt.Value.AddDays(-4),
                DayOfWeek.Saturday => dt.Value.AddDays(-5),
                DayOfWeek.Sunday => dt.Value.AddDays(-6),
                _ => throw new ArgumentOutOfRangeException(),
            };
    }

    public IAsyncEnumerable<Balance> SelectVouchersGrouped(IVoucherGroupedQuery query, int limit)
    {
        if (limit != 0)
            throw new NotSupportedException();
        var level = query.Preprocess();
        return Merge(Db.SelectVouchersGrouped(query).Concat(
            m_Vouchers.Where(v => v.IsMatch(query.VoucherQuery))
                .Select(v => new Balance { Date = ProjectDate(v.Date, level), Fund = 1 }).ToAsyncEnumerable()));
    }

    public IAsyncEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query, int limit)
    {
        if (limit != 0)
            throw new NotSupportedException();
        var level = query.Preprocess();
        var fluent = Merge(Db.SelectVoucherDetailsGrouped(query).Concat(
            m_Vouchers.Where(v => v.IsMatch(query.VoucherEmitQuery.VoucherQuery))
                .SelectMany(v => v.Details.Select(d => new VoucherDetailR(v, d)))
                .Where(d => d.IsMatch(query.VoucherEmitQuery.ActualDetailFilter()))
                .Select(d => new Balance
                    {
                        Date = ProjectDate(d.Voucher.Date, level),
                        User = level.HasFlag(SubtotalLevel.User) ? d.User : null,
                        Currency = level.HasFlag(SubtotalLevel.Currency) ? d.Currency : null,
                        Title = level.HasFlag(SubtotalLevel.Title) ? d.Title : null,
                        SubTitle = level.HasFlag(SubtotalLevel.SubTitle) ? d.SubTitle : null,
                        Content = level.HasFlag(SubtotalLevel.Content) ? d.Content : null,
                        Remark = level.HasFlag(SubtotalLevel.Remark) ? d.Remark : null,
                        Fund = query.Subtotal.GatherType == GatheringType.Count ? 1 : d.Fund!.Value,
                    }).ToAsyncEnumerable()));
        return query.ShouldAvoidZero() ? fluent.Where(b => !b.Fund.IsZero()) : fluent;
    }

    public IAsyncEnumerable<(Voucher, string, string, double)> SelectUnbalancedVouchers(
        IQueryCompounded<IVoucherQueryAtom> query)
        => throw new NotSupportedException();

    public IAsyncEnumerable<(Voucher, List<string>)> SelectDuplicatedVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => throw new NotSupportedException();

    public ValueTask<bool> DeleteVoucher(string id)
        => Db.DeleteVoucher(id);

    public async ValueTask<long> DeleteVouchers(IQueryCompounded<IVoucherQueryAtom> query)
        => await Db.DeleteVouchers(query) + m_Vouchers.RemoveAll(v => v.IsMatch(query));

    public async ValueTask<bool> Upsert(Voucher entity)
    {
        if (entity.ID != null)
            return await Db.Upsert(entity);

        m_Vouchers.Add(entity);
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
                m_Vouchers.Add(voucher);

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
}
