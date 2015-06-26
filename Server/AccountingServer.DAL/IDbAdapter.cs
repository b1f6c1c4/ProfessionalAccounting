using System;
using System.Collections.Generic;
using AccountingServer.Entities;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     数据库访问接口
    /// </summary>
    public interface IDbAdapter
    {
        #region Server

        /// <summary>
        ///     是否已经连接到数据库
        /// </summary>
        bool Connected { get; }

        /// <summary>
        ///     连接服务器
        /// </summary>
        void Connect();

        /// <summary>
        ///     断开服务器
        /// </summary>
        void Disconnect();

        #endregion

        #region Voucher

        /// <summary>
        ///     按编号查找记账凭证
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>记账凭证，如果没有则为<c>null</c></returns>
        Voucher SelectVoucher(string id);

        /// <summary>
        ///     按检索式查找记账凭证
        /// </summary>
        /// <param name="query">检索式</param>
        /// <returns>任一细目匹配检索式的记账凭证</returns>
        IEnumerable<Voucher> SelectVouchers(IQueryCompunded<IVoucherQueryAtom> query);

        /// <summary>
        ///     按检索式执行分类汇总
        /// </summary>
        /// <param name="query">检索式</param>
        /// <returns>分类汇总结果</returns>
        IEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query);

        /// <summary>
        ///     按编号删除记账凭证
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>是否成功</returns>
        bool DeleteVoucher(string id);

        /// <summary>
        ///     按检索式删除记账凭证
        /// </summary>
        /// <param name="query">检索式</param>
        /// <returns>已删除的细目总数</returns>
        long DeleteVouchers(IQueryCompunded<IVoucherQueryAtom> query);

        /// <summary>
        ///     添加或替换记账凭证
        ///     <para>若无编号，则添加新编号</para>
        /// </summary>
        /// <param name="entity">新记账凭证</param>
        /// <returns>是否成功</returns>
        bool Upsert(Voucher entity);

        #endregion

        #region Asset

        /// <summary>
        ///     按编号查找资产
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>资产，如果没有则为<c>null</c></returns>
        Asset SelectAsset(Guid id);

        /// <summary>
        ///     按记账凭证过滤器查找资产
        /// </summary>
        /// <param name="filter">记账凭证过滤器</param>
        /// <returns>匹配记账凭证过滤器的资产</returns>
        IEnumerable<Asset> SelectAssets(IQueryCompunded<IDistributedQueryAtom> filter);

        /// <summary>
        ///     按编号删除资产
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>是否成功</returns>
        bool DeleteAsset(Guid id);

        /// <summary>
        ///     按记账凭证过滤器删除资产
        /// </summary>
        /// <param name="filter">记账凭证过滤器</param>
        /// <returns>已删除的资产总数</returns>
        long DeleteAssets(IQueryCompunded<IDistributedQueryAtom> filter);

        /// <summary>
        ///     添加或替换资产
        ///     <para>若无编号，则添加新编号</para>
        /// </summary>
        /// <param name="entity">新资产</param>
        /// <returns>是否成功</returns>
        bool Upsert(Asset entity);

        #endregion

        #region Amortization

        /// <summary>
        ///     按编号查找摊销
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>摊销，如果没有则为<c>null</c></returns>
        Amortization SelectAmortization(Guid id);

        /// <summary>
        ///     按记账凭证过滤器查找摊销
        /// </summary>
        /// <param name="filter">记账凭证过滤器</param>
        /// <returns>匹配记账凭证过滤器的摊销</returns>
        IEnumerable<Amortization> SelectAmortizations(IQueryCompunded<IDistributedQueryAtom> filter);

        /// <summary>
        ///     按编号删除摊销
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>是否成功</returns>
        bool DeleteAmortization(Guid id);

        /// <summary>
        ///     按记账凭证过滤器删除摊销
        /// </summary>
        /// <param name="filter">记账凭证过滤器</param>
        /// <returns>已删除的摊销总数</returns>
        long DeleteAmortizations(IQueryCompunded<IDistributedQueryAtom> filter);

        /// <summary>
        ///     添加或替换摊销
        ///     <para>若无编号，则添加新编号</para>
        /// </summary>
        /// <param name="entity">新摊销</param>
        /// <returns>是否成功</returns>
        bool Upsert(Amortization entity);

        #endregion

        #region NamedQurey

        /// <summary>
        ///     按名称查找命名查询模板
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>命名查询模板</returns>
        string SelectNamedQueryTemplate(string name);

        /// <summary>
        ///     查找全部命名查询模板
        /// </summary>
        /// <returns>命名查询模板</returns>
        IEnumerable<KeyValuePair<string, string>> SelectNamedQueryTemplates();

        /// <summary>
        ///     按名称删除命名查询模板
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>是否成功</returns>
        bool DeleteNamedQueryTemplate(string name);

        /// <summary>
        ///     添加或替换命名查询模板
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="value">值</param>
        /// <returns>是否成功</returns>
        bool Upsert(string name, string value);

        #endregion
    }
}
