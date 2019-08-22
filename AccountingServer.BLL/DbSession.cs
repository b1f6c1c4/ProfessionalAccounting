using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AccountingServer.BLL.Util;
using AccountingServer.DAL;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    public class DbSession
    {
        public DbSession() => Db = Facade.Create();

        /// <summary>
        ///     数据库访问
        /// </summary>
        public IDbAdapter Db { private get; set; }

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

        public IEnumerable<Voucher> SelectVouchers(IQueryCompunded<IVoucherQueryAtom> query)
            => Db.SelectVouchers(query);

        public IEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query)
            => Db.SelectVoucherDetails(query);

        public ISubtotalResult SelectVouchersGrouped(IVoucherGroupedQuery query)
        {
            var res = Db.SelectVouchersGrouped(query);
            var conv = new SubtotalBuilder(query.Subtotal);
            return conv.Build(res);
        }

        public ISubtotalResult SelectVoucherDetailsGrouped(IGroupedQuery query)
        {
            var res = Db.SelectVoucherDetailsGrouped(query);
            var conv = new SubtotalBuilder(query.Subtotal);
            return conv.Build(res);
        }

        public bool DeleteVoucher(string id)
            => Db.DeleteVoucher(id);

        public long DeleteVouchers(IQueryCompunded<IVoucherQueryAtom> query)
            => Db.DeleteVouchers(query);

        public bool Upsert(Voucher entity)
        {
            Regularize(entity);

            return Db.Upsert(entity);
        }

        public static void Regularize(Voucher entity)
        {
            if (entity.Details == null)
                entity.Details = new List<VoucherDetail>();

            foreach (var d in entity.Details)
                d.Currency = d.Currency.ToUpperInvariant();

            entity.Details.Sort(TheComparison);
        }

        public Asset SelectAsset(Guid id)
            => Db.SelectAsset(id);

        public IEnumerable<Asset> SelectAssets(IQueryCompunded<IDistributedQueryAtom> filter)
            => Db.SelectAssets(filter);

        public bool DeleteAsset(Guid id)
            => Db.DeleteAsset(id);

        public long DeleteAssets(IQueryCompunded<IDistributedQueryAtom> filter)
            => Db.DeleteAssets(filter);

        public bool Upsert(Asset entity)
            => Db.Upsert(entity);

        public Amortization SelectAmortization(Guid id)
            => Db.SelectAmortization(id);

        public IEnumerable<Amortization> SelectAmortizations(IQueryCompunded<IDistributedQueryAtom> filter)
            => Db.SelectAmortizations(filter);

        public bool DeleteAmortization(Guid id)
            => Db.DeleteAmortization(id);

        public long DeleteAmortizations(IQueryCompunded<IDistributedQueryAtom> filter)
            => Db.DeleteAmortizations(filter);

        public bool Upsert(Amortization entity)
        {
            Regularize(entity.Template);

            return Db.Upsert(entity);
        }
    }
}
