using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AccountingServer.DAL;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL
{
    internal class DbSession
    {
        /// <summary>
        ///     数据库访问
        /// </summary>
        private IDbAdapter Db { get; set; }

        /// <summary>
        ///     获取是否已经连接到数据库
        /// </summary>
        public bool Connected => Db != null;

        public void Connect(string uri) => Db = Facade.Create(uri);

        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        private static int TheComparison(VoucherDetail lhs, VoucherDetail rhs)
        {
            // ReSharper disable once JoinDeclarationAndInitializer
            int res;

            res = string.Compare(lhs.Currency, rhs.Currency, StringComparison.Ordinal);
            if (res != 0)
                return res;

            res = lhs.Title.Value.CompareTo(rhs.Title.Value);
            if (res != 0)
                return res;

            res = lhs.SubTitle.Value.CompareTo(rhs.SubTitle.Value);
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

        public IEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query)
        {
            var res = Db.SelectVoucherDetailsGrouped(query);
            if (query.Subtotal.AggrType != AggregationType.ChangedDay &&
                query.Subtotal.GatherType == GatheringType.NonZero)
                return res.Where(b => !b.Fund.IsZero());

            return res;
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

        private static void Regularize(Voucher entity)
        {
            if (entity.Details == null)
                entity.Details = new List<VoucherDetail>();

            foreach (var d in entity.Details)
            {
                if (d.Currency == null)
                    d.Currency = VoucherDetail.BaseCurrency;

                d.Currency = d.Currency.ToUpper();
            }

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
