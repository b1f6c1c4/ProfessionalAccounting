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
using AccountingServer.BLL.Util;
using AccountingServer.DAL;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    public class DbSession : IHistoricalExchange
    {
        public DbSession(string uri = null, string db = null) => Db = Facade.Create(uri, db);

        /// <summary>
        ///     数据库访问
        /// </summary>
        public IDbAdapter Db { private get; init; }

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

            var res = Db.SelectExchangeRecord(new ExchangeRecord{ Time = date, From = from, To = to });
            if (res != null)
                return res.Value;

            Console.WriteLine($"{DateTime.UtcNow:o} Querying: {now:o} {from}/{to}");
            var value = ExchangeFactory.Instance.Query(from, to);
            Db.Upsert(new ExchangeRecord
                {
                    Time = now,
                    From = from,
                    To = to,
                    Value = value,
                });
            return value;
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
            => Db.SelectVoucher(id);

        public IEnumerable<Voucher> SelectVouchers(IQueryCompounded<IVoucherQueryAtom> query)
            => Db.SelectVouchers(query);

        public IEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query)
            => Db.SelectVoucherDetails(query);

        public ISubtotalResult SelectVouchersGrouped(IVoucherGroupedQuery query)
        {
            var res = Db.SelectVouchersGrouped(query, Limit);
            var conv = new SubtotalBuilder(query.Subtotal, this);
            return conv.Build(res);
        }

        public ISubtotalResult SelectVoucherDetailsGrouped(IGroupedQuery query)
        {
            var res = Db.SelectVoucherDetailsGrouped(query, Limit);
            var conv = new SubtotalBuilder(query.Subtotal, this);
            return conv.Build(res);
        }

        public bool DeleteVoucher(string id)
            => Db.DeleteVoucher(id);

        public long DeleteVouchers(IQueryCompounded<IVoucherQueryAtom> query)
            => Db.DeleteVouchers(query);

        public bool Upsert(Voucher entity)
        {
            Regularize(entity);

            return Db.Upsert(entity);
        }

        public long Upsert(IReadOnlyCollection<Voucher> entities)
        {
            foreach (var entity in entities)
                Regularize(entity);

            return Db.Upsert(entities);
        }

        public static void Regularize(Voucher entity)
        {
            entity.Details ??= new();

            foreach (var d in entity.Details)
            {
                d.User ??= ClientUser.Name;
                d.Currency = d.Currency.ToUpperInvariant();
            }

            entity.Details.Sort(TheComparison);
        }

        public Asset SelectAsset(Guid id)
            => Db.SelectAsset(id);

        public IEnumerable<Asset> SelectAssets(IQueryCompounded<IDistributedQueryAtom> filter)
            => Db.SelectAssets(filter);

        public bool DeleteAsset(Guid id)
            => Db.DeleteAsset(id);

        public long DeleteAssets(IQueryCompounded<IDistributedQueryAtom> filter)
            => Db.DeleteAssets(filter);

        public bool Upsert(Asset entity)
            => Db.Upsert(entity);

        public Amortization SelectAmortization(Guid id)
            => Db.SelectAmortization(id);

        public IEnumerable<Amortization> SelectAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
            => Db.SelectAmortizations(filter);

        public bool DeleteAmortization(Guid id)
            => Db.DeleteAmortization(id);

        public long DeleteAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
            => Db.DeleteAmortizations(filter);

        public bool Upsert(Amortization entity)
        {
            Regularize(entity.Template);

            return Db.Upsert(entity);
        }
    }
}
