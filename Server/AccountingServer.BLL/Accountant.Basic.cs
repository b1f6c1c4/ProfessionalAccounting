using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.DAL;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     基本会计业务处理类
    /// </summary>
    public partial class Accountant
    {
        /// <summary>
        ///     数据库访问
        /// </summary>
        private readonly IDbAdapter m_Db;

        private readonly IDbServer m_DbServer;

        /// <summary>
        ///     判断金额相等的误差
        /// </summary>
        public const double Tolerance = 1e-8;

        /// <summary>
        ///     获取是否已经连接到数据库
        /// </summary>
        public bool Connected { get { return m_Db.Connected; } }

        public Accountant()
        {
            var adapter = new MongoDbAdapter();

            m_Db = adapter;
            m_DbServer = adapter;
        }

        #region Server

        public void Launch() { m_DbServer.Launch(); }

        public void Connect() { m_Db.Connect(); }

        public void Disconnect() { m_Db.Disconnect(); }

        public void Backup() { m_DbServer.Backup(); }

        #endregion

        #region Voucher

        public Voucher SelectVoucher(string id) { return m_Db.SelectVoucher(id); }

        public IEnumerable<Voucher> SelectVouchers(IQueryCompunded<IVoucherQueryAtom> query)
        {
            return m_Db.SelectVouchers(query);
        }

        public IEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query)
        {
            return m_Db.SelectVoucherDetails(query);
        }

        public IEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query)
        {
            var res = m_Db.SelectVoucherDetailsGrouped(query);
            if (query.Subtotal.AggrType != AggregationType.ChangedDay &&
                query.Subtotal.NonZero)
                return res.Where(b => Math.Abs(b.Fund) >= Tolerance);
            return res;
        }

        public bool DeleteVoucher(string id) { return m_Db.DeleteVoucher(id); }

        public long DeleteVouchers(IQueryCompunded<IVoucherQueryAtom> query) { return m_Db.DeleteVouchers(query); }

        public bool Upsert(Voucher entity) { return m_Db.Upsert(entity); }

        #endregion

        #region Asset

        public Asset SelectAsset(Guid id)
        {
            var result = m_Db.SelectAsset(id);
            InternalRegular(result);
            return result;
        }

        public IEnumerable<Asset> SelectAssets(IQueryCompunded<IDistributedQueryAtom> filter)
        {
            foreach (var asset in m_Db.SelectAssets(filter))
            {
                InternalRegular(asset);
                yield return asset;
            }
        }

        public bool DeleteAsset(Guid id) { return m_Db.DeleteAsset(id); }

        public long DeleteAssets(IQueryCompunded<IDistributedQueryAtom> filter) { return m_Db.DeleteAssets(filter); }

        public bool Upsert(Asset entity) { return m_Db.Upsert(entity); }

        #endregion

        #region Amort

        public Amortization SelectAmortization(Guid id)
        {
            var result = m_Db.SelectAmortization(id);
            InternalRegular(result);
            return result;
        }

        public IEnumerable<Amortization> SelectAmortizations(IQueryCompunded<IDistributedQueryAtom> filter)
        {
            foreach (var amort in m_Db.SelectAmortizations(filter))
            {
                InternalRegular(amort);
                yield return amort;
            }
        }

        public bool DeleteAmortization(Guid id) { return m_Db.DeleteAmortization(id); }

        public long DeleteAmortizations(IQueryCompunded<IDistributedQueryAtom> filter)
        {
            return m_Db.DeleteAmortizations(filter);
        }

        public bool Upsert(Amortization entity) { return m_Db.Upsert(entity); }

        #endregion

        #region NamedQurey

        public string SelectNamedQueryTemplate(string name) { return m_Db.SelectNamedQueryTemplate(name); }

        public IEnumerable<KeyValuePair<string, string>> SelectNamedQueryTemplates()
        {
            return m_Db.SelectNamedQueryTemplates();
        }

        public bool DeleteNamedQueryTemplate(string name) { return m_Db.DeleteNamedQueryTemplate(name); }

        public bool Upsert(string name, string value) { return m_Db.Upsert(name, value); }

        #endregion
    }
}
