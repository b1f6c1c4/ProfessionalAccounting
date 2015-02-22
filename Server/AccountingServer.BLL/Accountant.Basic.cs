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
        ///// <summary>
        /////     本年利润
        ///// </summary>
        //public const int FullYearProfit = 4103;

        ///// <summary>
        /////     费用
        ///// </summary>
        //public const int Cost = 5000;

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

        /// <summary>
        ///     启动数据库服务器
        /// </summary>
        public void Launch()
        {
            m_DbServer.Launch();
        }

        /// <summary>
        ///     连接数据库
        /// </summary>
        public void Connect()
        {
            m_Db.Connect();
        }

        /// <summary>
        ///     断开数据库连接
        /// </summary>
        public void Disconnect()
        {
            m_Db.Disconnect();
        }

        /// <summary>
        ///     备份数据库
        /// </summary>
        public void Backup()
        {
            m_DbServer.Backup();
        }

        #region voucher

        /// <summary>
        ///     按编号查找记账凭证
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>记账凭证，如果没有则为<c>null</c></returns>
        public Voucher SelectVoucher(string id)
        {
            return m_Db.SelectVoucher(id);
        }

        /// <summary>
        ///     按检索式查找记账凭证
        /// </summary>
        /// <param name="query">检索式</param>
        /// <returns>任一细目匹配检索式的记账凭证</returns>
        public IEnumerable<Voucher> SelectVouchers(IQueryCompunded<IVoucherQueryAtom> query)
        {
            return m_Db.SelectVouchers(query);
        }

        /// <summary>
        ///     按检索式查找细目
        /// </summary>
        /// <param name="query">检索式</param>
        /// <returns>匹配检索式的细目</returns>
        public IEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query)
        {
            return m_Db.SelectVoucherDetails(query);
        }

        /// <summary>
        ///     按检索式执行分类汇总
        /// </summary>
        /// <param name="query">检索式</param>
        /// <returns>分类汇总结果</returns>
        public IEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query)
        {
            var res = m_Db.SelectVoucherDetailsGrouped(query);
            if (query.Subtotal.AggrType != AggregationType.ChangedDay &&
                query.Subtotal.NonZero)
                return res.Where(b => Math.Abs(b.Fund) >= Tolerance);
            return res;
        }

        /// <summary>
        ///     按编号删除记账凭证
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>是否成功</returns>
        public bool DeleteVoucher(string id)
        {
            return m_Db.DeleteVoucher(id);
        }

        /// <summary>
        ///     按检索式删除记账凭证
        /// </summary>
        /// <param name="query">检索式</param>
        /// <returns>已删除的细目总数</returns>
        public long DeleteVouchers(IQueryCompunded<IVoucherQueryAtom> query)
        {
            return m_Db.DeleteVouchers(query);
        }

        /// <summary>
        ///     添加或替换记账凭证
        ///     <para>若无编号，则添加新编号</para>
        /// </summary>
        /// <param name="entity">新记账凭证</param>
        /// <returns>是否成功</returns>
        public bool Upsert(Voucher entity)
        {
            return m_Db.Upsert(entity);
        }

        #endregion

        #region asset

        /// <summary>
        ///     按编号查找资产
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>资产，如果没有则为<c>null</c></returns>
        public Asset SelectAsset(Guid id)
        {
            var result = m_Db.SelectAsset(id);
            InternalRegular(result);
            return result;
        }

        /// <summary>
        ///     按过滤器查找资产
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>匹配过滤器的资产</returns>
        public IEnumerable<Asset> SelectAssets(IQueryCompunded<IDistributedQueryAtom> filter)
        {
            foreach (var asset in m_Db.SelectAssets(filter))
            {
                InternalRegular(asset);
                yield return asset;
            }
        }

        /// <summary>
        ///     添加或替换资产
        ///     <para>若无编号，则添加新编号</para>
        /// </summary>
        /// <param name="entity">新资产</param>
        /// <returns>是否成功</returns>
        public bool Upsert(Asset entity)
        {
            return m_Db.Upsert(entity);
        }

        /// <summary>
        ///     按编号删除资产
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>是否成功</returns>
        public bool DeleteAsset(Guid id)
        {
            return m_Db.DeleteAsset(id);
        }

        /// <summary>
        ///     按过滤器删除资产
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>已删除的资产总数</returns>
        public long DeleteAssets(IQueryCompunded<IDistributedQueryAtom> filter)
        {
            return m_Db.DeleteAssets(filter);
        }

        /// <summary>
        ///     添加或替换资产
        ///     <para>不能改变资产的编号</para>
        /// </summary>
        /// <param name="entity">新资产</param>
        /// <returns>是否成功</returns>
        public bool Update(Asset entity)
        {
            return m_Db.Upsert(entity);
        }

        #endregion

        #region amort

        /// <summary>
        ///     按编号查找摊销
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>摊销，如果没有则为<c>null</c></returns>
        public Amortization SelectAmortization(Guid id)
        {
            var result = m_Db.SelectAmortization(id);
            InternalRegular(result);
            return result;
        }

        /// <summary>
        ///     按过滤器查找摊销
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>匹配过滤器的摊销</returns>
        public IEnumerable<Amortization> SelectAmortizations(IQueryCompunded<IDistributedQueryAtom> filter)
        {
            foreach (var amort in m_Db.SelectAmortizations(filter))
            {
                InternalRegular(amort);
                yield return amort;
            }
        }

        /// <summary>
        ///     添加或替换摊销
        ///     <para>若无编号，则添加新编号</para>
        /// </summary>
        /// <param name="entity">新摊销</param>
        /// <returns>是否成功</returns>
        public bool Upsert(Amortization entity)
        {
            return m_Db.Upsert(entity);
        }

        /// <summary>
        ///     按编号删除摊销
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>是否成功</returns>
        public bool DeleteAmortization(Guid id)
        {
            return m_Db.DeleteAmortization(id);
        }

        /// <summary>
        ///     按过滤器删除摊销
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>已删除的摊销总数</returns>
        public long DeleteAmortizations(IQueryCompunded<IDistributedQueryAtom> filter)
        {
            return m_Db.DeleteAmortizations(filter);
        }

        /// <summary>
        ///     添加或替换摊销
        ///     <para>不能改变摊销的编号</para>
        /// </summary>
        /// <param name="entity">新摊销</param>
        /// <returns>是否成功</returns>
        public bool Update(Amortization entity)
        {
            return m_Db.Upsert(entity);
        }

        #endregion

        #region namedqurey

        /// <summary>
        ///     按名称查找命名查询模板
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>命名查询模板</returns>
        public string SelectNamedQueryTemplate(string name) { return m_Db.SelectNamedQueryTemplate(name); }

        /// <summary>
        ///     查找全部命名查询模板
        /// </summary>
        /// <returns>命名查询模板</returns>
        public IEnumerable<KeyValuePair<string, string>> SelectNamedQueryTemplates()
        {
            return m_Db.SelectNamedQueryTemplates();
        }

        /// <summary>
        ///     按名称删除命名查询模板
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>是否成功</returns>
        public bool DeleteNamedQueryTemplate(string name) { return m_Db.DeleteNamedQueryTemplate(name); }

        /// <summary>
        ///     添加或替换命名查询模板
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="value">值</param>
        /// <returns>是否成功</returns>
        public bool Upsert(string name, string value) { return m_Db.Upsert(name, value); }

        #endregion

        /// <summary>
        ///     合并记账凭证上相同的细目
        /// </summary>
        /// <param name="voucher">记账凭证</param>
        public void Shrink(Voucher voucher)
        {
            voucher.Details = voucher.Details
                                     .GroupBy(
                                              d => new VoucherDetail { Title = d.Title, Content = d.Content },
                                              (key, grp) =>
                                              {
                                                  key.Item = voucher.ID;
                                                  key.Fund =
                                                      grp.Sum(
                                                              d =>
                                                              d.Fund);
                                                  return key;
                                              }).ToList();
            m_Db.Upsert(voucher);
        }

        /// <summary>
        ///     期末结转
        /// </summary>
        public void Carry()
        {
            throw new NotImplementedException();
        }
    }
}
