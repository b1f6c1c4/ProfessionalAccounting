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
using System.Threading;
using AccountingServer.BLL.Util;
using AccountingServer.DAL;
using AccountingServer.Entities;

namespace AccountingServer.BLL;

public class DbSession : IHistoricalExchange
{
    public DbSession(string uri = null, string db = null) => Db = Facade.Create(uri, db);

    private readonly ReaderWriterLockSlim m_Lock = new(LockRecursionPolicy.SupportsRecursion);

    public VirtualizeLock Virtualize()
    {
        m_Lock.EnterWriteLock();
        Db = Facade.Virtualize(Db);
        return new VirtualizeLock(this);
    }

    public class VirtualizeLock : IDisposable
    {
        public VirtualizeLock(DbSession db) => Db = db;

        private DbSession Db { get; }

        public int CachedVouchers => (Db.Db as Virtualizer)!.CachedVouchers;

        public void Dispose()
        {
            Db.Db = Facade.UnVirtualize(Db.Db);
            Db.m_Lock.ExitWriteLock();
        }
    }

    /// <summary>
    ///     数据库访问
    /// </summary>
    public IDbAdapter Db { private get; set; }

    /// <summary>
    ///     返回结果数量上限
    /// </summary>
    public int Limit { private get; set; } = 0;

    /// <summary>
    ///     查询汇率
    /// </summary>
    /// <param name="date">日期</param>
    /// <param name="from">购汇币种</param>
    /// <param name="to">结汇币种</param>
    /// <returns>汇率</returns>
    private double LookupExchange(DateTime date, string from, string to)
    {
        if (from == to)
            return 1;

        var now = DateTime.UtcNow;
        if (date > now)
            date = now;

        m_Lock.EnterReadLock();
        try
        {
            var res = Db.SelectExchangeRecord(new ExchangeRecord { Time = date, From = from, To = to });
            if (res != null)
                return res.Value;
            res = Db.SelectExchangeRecord(new ExchangeRecord { Time = date, From = to, To = from });
            if (res != null)
                return 1D / res.Value;

            Console.WriteLine($"{now:o} Query: {date:o} {from}/{to}");
            var value = ExchangeFactory.Instance.Query(from, to);
            Db.Upsert(new ExchangeRecord
                {
                    Time = now, From = from, To = to, Value = value,
                });
            return value;
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public double SaveHistoricalRate(DateTime date, string from, string to)
    {
        m_Lock.EnterReadLock();
        try
        {
            var res = Db.SelectExchangeRecord(new ExchangeRecord { Time = date, From = from, To = to });
            if (res != null && res.Time == date)
                return res.Value;
            res = Db.SelectExchangeRecord(new ExchangeRecord { Time = date, From = to, To = from });
            if (res != null && res.Time == date)
                return 1D / res.Value;

            var value = ExchangeFactory.HistoricalInstance.Query(date, from, to);
            Db.Upsert(new ExchangeRecord { Time = date, From = from, To = to, Value = value });
            return value;
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public double Query(DateTime? date, string from, string to)
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

    public Voucher SelectVoucher(string id)
    {
        m_Lock.EnterReadLock();
        try
        {
            return Db.SelectVoucher(id);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public IEnumerable<Voucher> SelectVouchers(IQueryCompounded<IVoucherQueryAtom> query)
    {
        m_Lock.EnterReadLock();
        try
        {
            return Db.SelectVouchers(query);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public IEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query)
    {
        m_Lock.EnterReadLock();
        try
        {
            return Db.SelectVoucherDetails(query);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public ISubtotalResult SelectVouchersGrouped(IVoucherGroupedQuery query)
    {
        m_Lock.EnterReadLock();
        try
        {
            var res = Db.SelectVouchersGrouped(query, Limit);
            var conv = new SubtotalBuilder(query.Subtotal, this);
            return conv.Build(res);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public ISubtotalResult SelectVoucherDetailsGrouped(IGroupedQuery query)
    {
        m_Lock.EnterReadLock();
        try
        {
            var res = Db.SelectVoucherDetailsGrouped(query, Limit);
            var conv = new SubtotalBuilder(query.Subtotal, this);
            return conv.Build(res);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public IEnumerable<(Voucher, string, string, double)> SelectUnbalancedVouchers(
        IQueryCompounded<IVoucherQueryAtom> query)
    {
        m_Lock.EnterReadLock();
        try
        {
            return Db.SelectUnbalancedVouchers(query);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public IEnumerable<(Voucher, List<string>)> SelectDuplicatedVouchers(IQueryCompounded<IVoucherQueryAtom> query)
    {
        m_Lock.EnterReadLock();
        try
        {
            return Db.SelectDuplicatedVouchers(query);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public bool DeleteVoucher(string id)
    {
        m_Lock.EnterReadLock();
        try
        {
            return Db.DeleteVoucher(id);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public long DeleteVouchers(IQueryCompounded<IVoucherQueryAtom> query)
    {
        m_Lock.EnterReadLock();
        try
        {
            return Db.DeleteVouchers(query);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public bool Upsert(Voucher entity)
    {
        Regularize(entity);

        m_Lock.EnterReadLock();
        try
        {
            return Db.Upsert(entity);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public long Upsert(IReadOnlyCollection<Voucher> entities)
    {
        foreach (var entity in entities)
            Regularize(entity);

        m_Lock.EnterReadLock();
        try
        {
            return Db.Upsert(entities);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
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
            d.User ??= ClientUser.Name;
            d.Currency = d.Currency.ToUpperInvariant();
            d.Fund = Regularize(d.Fund);
        }

        entity.Details.Sort(TheComparison);
    }

    public Asset SelectAsset(Guid id)
    {
        m_Lock.EnterReadLock();
        try
        {
            return Db.SelectAsset(id);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public IEnumerable<Asset> SelectAssets(IQueryCompounded<IDistributedQueryAtom> filter)
    {
        m_Lock.EnterReadLock();
        try
        {
            return Db.SelectAssets(filter);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public bool DeleteAsset(Guid id)
    {
        m_Lock.EnterReadLock();
        try
        {
            return Db.DeleteAsset(id);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public long DeleteAssets(IQueryCompounded<IDistributedQueryAtom> filter)
    {
        m_Lock.EnterReadLock();
        try
        {
            return Db.DeleteAssets(filter);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public bool Upsert(Asset entity)
    {
        m_Lock.EnterReadLock();
        try
        {
            return Db.Upsert(entity);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public Amortization SelectAmortization(Guid id)
    {
        m_Lock.EnterReadLock();
        try
        {
            return Db.SelectAmortization(id);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public IEnumerable<Amortization> SelectAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
    {
        m_Lock.EnterReadLock();
        try
        {
            return Db.SelectAmortizations(filter);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public bool DeleteAmortization(Guid id)
    {
        m_Lock.EnterReadLock();
        try
        {
            return Db.DeleteAmortization(id);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public long DeleteAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
    {
        m_Lock.EnterReadLock();
        try
        {
            return Db.DeleteAmortizations(filter);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    public bool Upsert(Amortization entity)
    {
        Regularize(entity.Template);

        m_Lock.EnterReadLock();
        try
        {
            return Db.Upsert(entity);
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }
}
