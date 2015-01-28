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

        /// <summary>
        ///     资产类科目
        /// </summary>
        public const int Asset0 = 1000;

        /// <summary>
        ///     负债类科目
        /// </summary>
        public const int Debt0 = 2000;

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

        /// <summary>
        ///     是否为资产类科目
        /// </summary>
        /// <param name="id">科目编号</param>
        /// <returns>是否为资产</returns>
        public static bool IsAsset(int? id)
        {
            return id / 1000 == Asset0 / 1000;
        }

        /// <summary>
        ///     是否为负债类科目
        /// </summary>
        /// <param name="id">科目编号</param>
        /// <returns>是否为负债</returns>
        public static bool IsDebt(int? id)
        {
            return id / 1000 == Debt0 / 1000;
        }

        /// <summary>
        ///     检查记账凭证借贷方数额是否相等
        /// </summary>
        /// <param name="entity">记账凭证</param>
        /// <returns>借方比贷方多出数</returns>
        public static double IsBalanced(Voucher entity)
        {
            // ReSharper disable once PossibleInvalidOperationException
            return entity.Details.Sum(d => d.Fund.Value);
        }

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
        /// <returns>记账凭证，如果没有则为null</returns>
        public Voucher SelectVoucher(string id)
        {
            return m_Db.SelectVoucher(id);
        }

        /// <summary>
        ///     按过滤器查找记账凭证
        /// </summary>
        /// <param name="vfilter">过滤器</param>
        /// <param name="filter">细目过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>任一细目匹配过滤器的记账凭证</returns>
        public IEnumerable<Voucher> FilteredSelect(Voucher vfilter = null,
                                                   VoucherDetail filter = null,
                                                   DateFilter? rng = null)
        {
            return m_Db.FilteredSelect(vfilter, filter, rng);
        }

        /// <summary>
        ///     按过滤器查找记账凭证
        /// </summary>
        /// <param name="vfilter">过滤器</param>
        /// <param name="filters">细目过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="useAnd">各细目过滤器之间的关系为合取</param>
        /// <returns>任一细目匹配任一过滤器的记账凭证</returns>
        public IEnumerable<Voucher> FilteredSelect(Voucher vfilter = null,
                                                   IEnumerable<VoucherDetail> filters = null,
                                                   DateFilter? rng = null,
                                                   bool useAnd = false)
        {
            return m_Db.FilteredSelect(vfilter, filters, rng, useAnd);
        }


        /// <summary>
        ///     添加或替换记账凭证
        ///     <para>不能改变记账凭证的编号</para>
        /// </summary>
        /// <param name="entity">新记账凭证</param>
        /// <returns>是否成功</returns>
        public bool Update(Voucher entity)
        {
            return m_Db.Upsert(entity);
        }

        /// <summary>
        ///     按过滤器查找细目
        /// </summary>
        /// <param name="vfilter">过滤器</param>
        /// <param name="filter">细目过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>匹配过滤器的细目</returns>
        public IEnumerable<VoucherDetail> FilteredSelectDetails(Voucher vfilter = null,
                                                                VoucherDetail filter = null,
                                                                DateFilter? rng = null)
        {
            return m_Db.FilteredSelectDetails(vfilter, filter, rng);
        }

        /// <summary>
        ///     按过滤器查找细目
        /// </summary>
        /// <param name="vfilter">过滤器</param>
        /// <param name="filters">细目过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="useAnd">各细目过滤器之间的关系为合取</param>
        /// <returns>匹配过滤器的细目</returns>
        public IEnumerable<VoucherDetail> FilteredSelectDetails(Voucher vfilter = null,
                                                                IEnumerable<VoucherDetail> filters = null,
                                                                DateFilter? rng = null,
                                                                bool useAnd = false)
        {
            return m_Db.FilteredSelectDetails(vfilter, filters, rng, useAnd);
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
        ///     按过滤器删除记账凭证
        /// </summary>
        /// <param name="vfilter">过滤器</param>
        /// <param name="filter">细目过滤器</param>
        /// <returns>已删除的记账凭证总数</returns>
        public long FilteredDelete(Voucher vfilter, VoucherDetail filter)
        {
            return m_Db.FilteredDelete(vfilter, filter);
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
        public IEnumerable<Asset> FilteredSelect(Asset filter)
        {
            foreach (var asset in m_Db.FilteredSelect(filter))
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
        public long FilteredDelete(Asset filter)
        {
            return m_Db.FilteredDelete(filter);
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
        public IEnumerable<Amortization> FilteredSelect(Amortization filter)
        {
            foreach (var amort in m_Db.FilteredSelect(filter))
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
        public long FilteredDelete(Amortization filter)
        {
            return m_Db.FilteredDelete(filter);
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
