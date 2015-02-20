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
        #region server

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

        #region voucher

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
        IEnumerable<Voucher> FilteredSelect(IVoucherQueryCompounded query);

        /// <summary>
        ///     按检索式查找细目
        /// </summary>
        /// <param name="query">检索式</param>
        /// <returns>匹配检索式的细目</returns>
        IEnumerable<VoucherDetail> FilteredSelect(IVoucherDetailQuery query);

        /// <summary>
        ///     按检索式执行分类汇总
        /// </summary>
        /// <param name="query">检索式</param>
        /// <returns>分类汇总结果</returns>
        IEnumerable<Balance> FilteredSelect(IGroupedQuery query);

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
        long FilteredDelete(IVoucherQueryCompounded query);

        /// <summary>
        ///     添加或替换记账凭证
        ///     <para>若无编号，则添加新编号</para>
        /// </summary>
        /// <param name="entity">新记账凭证</param>
        /// <returns>是否成功</returns>
        bool Upsert(Voucher entity);

        #endregion

        #region asset

        /// <summary>
        ///     按编号查找资产
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>资产，如果没有则为<c>null</c></returns>
        Asset SelectAsset(Guid id);

        /// <summary>
        ///     按过滤器查找资产
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>匹配过滤器的资产</returns>
        IEnumerable<Asset> FilteredSelect(Asset filter);

        /// <summary>
        ///     按编号删除资产
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>是否成功</returns>
        bool DeleteAsset(Guid id);

        /// <summary>
        ///     按过滤器删除资产
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>已删除的资产总数</returns>
        long FilteredDelete(Asset filter);

        /// <summary>
        ///     添加或替换资产
        ///     <para>若无编号，则添加新编号</para>
        /// </summary>
        /// <param name="entity">新资产</param>
        /// <returns>是否成功</returns>
        bool Upsert(Asset entity);

        #endregion

        #region amortization

        /// <summary>
        ///     按编号查找摊销
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>摊销，如果没有则为<c>null</c></returns>
        Amortization SelectAmortization(Guid id);

        /// <summary>
        ///     按过滤器查找摊销
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>匹配过滤器的摊销</returns>
        IEnumerable<Amortization> FilteredSelect(Amortization filter);

        /// <summary>
        ///     按编号删除摊销
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>是否成功</returns>
        bool DeleteAmortization(Guid id);

        /// <summary>
        ///     按过滤器删除摊销
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>已删除的摊销总数</returns>
        long FilteredDelete(Amortization filter);

        /// <summary>
        ///     添加或替换摊销
        ///     <para>若无编号，则添加新编号</para>
        /// </summary>
        /// <param name="entity">新摊销</param>
        /// <returns>是否成功</returns>
        bool Upsert(Amortization entity);

        #endregion
    }
}
