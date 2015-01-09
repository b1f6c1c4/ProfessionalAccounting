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

        /// <summary>
        ///     按编号查找记账凭证
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>记账凭证，如果没有则为<c>null</c></returns>
        Voucher SelectVoucher(string id);

        /// <summary>
        ///     按过滤器查找记账凭证
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>匹配过滤器的记账凭证</returns>
        IEnumerable<Voucher> FilteredSelect(Voucher filter, DateFilter rng);

        /// <summary>
        ///     按编号删除记账凭证
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>是否成功</returns>
        bool DeleteVoucher(string id);

        /// <summary>
        ///     按过滤器删除记账凭证
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>已删除的记账凭证总数</returns>
        long FilteredDelete(Voucher filter);

        /// <summary>
        ///     添加或替换记账凭证
        ///     <para>若无编号，则添加新编号</para>
        /// </summary>
        /// <param name="entity">新记账凭证</param>
        /// <returns>是否成功</returns>
        bool Upsert(Voucher entity);

        /// <summary>
        ///     按细目过滤器查找记账凭证
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>任一细目匹配过滤器的记账凭证</returns>
        IEnumerable<Voucher> FilteredSelect(VoucherDetail filter, DateFilter rng);

        /// <summary>
        ///     按过滤器和细目过滤器查找记账凭证
        /// </summary>
        /// <param name="vfilter">过滤器</param>
        /// <param name="filter">细目过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>任一细目匹配过滤器的记账凭证</returns>
        IEnumerable<Voucher> FilteredSelect(Voucher vfilter, VoucherDetail filter, DateFilter rng);

        /// <summary>
        ///     按细目过滤器查找记账凭证
        /// </summary>
        /// <param name="filters">细目过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>任一细目匹配任一过滤器的记账凭证</returns>
        IEnumerable<Voucher> FilteredSelect(IEnumerable<VoucherDetail> filters, DateFilter rng);

        /// <summary>
        ///     按细目过滤器查找细目
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>匹配过滤器的细目</returns>
        IEnumerable<VoucherDetail> FilteredSelectDetails(VoucherDetail filter, DateFilter rng);

        /// <summary>
        ///     按过滤器删除记账凭证
        /// </summary>
        /// <param name="vfilter">过滤器</param>
        /// <param name="filter">细目过滤器</param>
        /// <returns>已删除的细目总数</returns>
        long FilteredDelete(Voucher vfilter, VoucherDetail filter);

        /// <summary>
        ///     按过滤器删除细目
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <returns>已删除的细目总数</returns>
        long FilteredDelete(VoucherDetail filter);


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
    }
}
