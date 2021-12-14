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
using AccountingServer.BLL.Util;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     基本会计业务处理类
    /// </summary>
    public class Accountant : IHistoricalExchange
    {
        private readonly AmortAccountant m_AmortAccountant;

        private readonly AssetAccountant m_AssetAccountant;
        private readonly DbSession m_Db;

        public Accountant(string uri = null, string db = null)
        {
            m_Db = new(uri, db);
            m_AssetAccountant = new(m_Db);
            m_AmortAccountant = new(m_Db);
        }

        public void AdjustLimit(int limit)
            => m_Db.Limit = limit;

        #region Voucher

        public Voucher SelectVoucher(string id)
            => m_Db.SelectVoucher(id);

        public IEnumerable<Voucher> SelectVouchers(IQueryCompounded<IVoucherQueryAtom> query)
            => m_Db.SelectVouchers(query);

        public IEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query)
            => m_Db.SelectVoucherDetails(query);

        public ISubtotalResult SelectVoucherDetailsGrouped(IGroupedQuery query)
            => m_Db.SelectVoucherDetailsGrouped(query);

        public ISubtotalResult SelectVouchersGrouped(IVoucherGroupedQuery query)
            => m_Db.SelectVouchersGrouped(query);

        public IEnumerable<(Voucher, List<string>)> SelectDuplicatedVouchers(IQueryCompounded<IVoucherQueryAtom> query)
            => m_Db.SelectDuplicatedVouchers(query);

        public bool DeleteVoucher(string id)
            => m_Db.DeleteVoucher(id);

        public long DeleteVouchers(IQueryCompounded<IVoucherQueryAtom> query)
            => m_Db.DeleteVouchers(query);

        public bool Upsert(Voucher entity)
            => m_Db.Upsert(entity);

        public long Upsert(IReadOnlyCollection<Voucher> entities)
            => m_Db.Upsert(entities);

        #endregion

        #region Asset

        public Asset SelectAsset(Guid id) => AssetAccountant.InternalRegular(m_Db.SelectAsset(id));

        public IEnumerable<Asset> SelectAssets(IQueryCompounded<IDistributedQueryAtom> filter)
            => m_Db.SelectAssets(filter).Select(AssetAccountant.InternalRegular);

        public bool DeleteAsset(Guid id)
            => m_Db.DeleteAsset(id);

        public long DeleteAssets(IQueryCompounded<IDistributedQueryAtom> filter)
            => m_Db.DeleteAssets(filter);

        public bool Upsert(Asset entity)
            => m_Db.Upsert(entity);

        public IEnumerable<Voucher> RegisterVouchers(Asset asset, DateFilter rng,
            IQueryCompounded<IVoucherQueryAtom> query)
            => m_AssetAccountant.RegisterVouchers(asset, rng, query);

        public static void Depreciate(Asset asset) => AssetAccountant.Depreciate(asset);

        public IEnumerable<AssetItem> Update(Asset asset, DateFilter rng, bool isCollapsed = false,
            bool editOnly = false)
            => m_AssetAccountant.Update(asset, rng, isCollapsed, editOnly);

        #endregion

        #region Amort

        public Amortization SelectAmortization(Guid id)
        {
            var result = m_Db.SelectAmortization(id);
            AmortAccountant.InternalRegular(result);
            return result;
        }

        public IEnumerable<Amortization> SelectAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
        {
            foreach (var amort in m_Db.SelectAmortizations(filter))
            {
                AmortAccountant.InternalRegular(amort);
                yield return amort;
            }
        }

        public bool DeleteAmortization(Guid id)
            => m_Db.DeleteAmortization(id);

        public long DeleteAmortizations(IQueryCompounded<IDistributedQueryAtom> filter)
            => m_Db.DeleteAmortizations(filter);

        public bool Upsert(Amortization entity)
            => m_Db.Upsert(entity);

        public IEnumerable<Voucher> RegisterVouchers(Amortization amort, DateFilter rng,
            IQueryCompounded<IVoucherQueryAtom> query)
            => m_AmortAccountant.RegisterVouchers(amort, rng, query);

        public static void Amortize(Amortization amort) => AmortAccountant.Amortize(amort);

        public IEnumerable<AmortItem> Update(Amortization amort, DateFilter rng,
            bool isCollapsed = false, bool editOnly = false)
            => m_AmortAccountant.Update(amort, rng, isCollapsed, editOnly);

        public static double? GetBookValueOn(IDistributed dist, DateTime? dt)
            => DistributedAccountant.GetBookValueOn(dist, dt);

        #endregion

        #region Exchange

        public double Query(DateTime? date, string from, string to) => m_Db.Query(date, from, to);

        #endregion
    }
}
