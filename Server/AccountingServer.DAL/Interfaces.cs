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
        /// <returns>匹配过滤器的记账凭证</returns>
        IEnumerable<Voucher> SelectVouchers(Voucher filter);

        /// <summary>
        ///     按过滤器和日期查找记账凭证
        ///     <para>若<paramref name="startDate" />和<paramref name="endDate" />均为<c>null</c>，则返回所有无日期的记账凭证</para>
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <param name="startDate">开始日期，若为<c>null</c>表示不检查最小日期，无日期亦可</param>
        /// <param name="endDate">截止日期，若为<c>null</c>表示不检查最大日期</param>
        /// <returns>指定日期匹配过滤器的记账凭证</returns>
        IEnumerable<Voucher> SelectVouchers(Voucher filter, DateTime? startDate, DateTime? endDate);

        /// <summary>
        ///     按过滤器查找记账凭证并记数
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>匹配过滤器的记账凭证总数</returns>
        long SelectVouchersCount(Voucher filter);

        /// <summary>
        ///     添加记账凭证
        ///     <para>若<paramref name="entity" />没有指定编号，则添加成功后会自动给<paramref name="entity" />添加编号</para>
        /// </summary>
        /// <param name="entity">记账凭证</param>
        /// <returns>是否成功</returns>
        bool InsertVoucher(Voucher entity);

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
        int DeleteVouchers(Voucher filter);

        /// <summary>
        ///     添加或替换记账凭证
        ///     <para>不能改变记账凭证的编号</para>
        /// </summary>
        /// <param name="entity">新记账凭证</param>
        /// <returns>是否成功</returns>
        bool UpdateVoucher(Voucher entity);

        /// <summary>
        ///     按细目过滤器查找记账凭证
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <returns>任一细目匹配过滤器的记账凭证</returns>
        IEnumerable<Voucher> SelectVouchersWithDetail(VoucherDetail filter);

        /// <summary>
        ///     按过滤器和细目过滤器查找记账凭证
        ///     <para>若<paramref name="startDate" />和<paramref name="endDate" />均为<c>null</c>，则返回所有无日期的记账凭证</para>
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <param name="startDate">开始日期，若为<c>null</c>表示不检查最小日期，无日期亦可</param>
        /// <param name="endDate">截止日期，若为<c>null</c>表示不检查最大日期</param>
        /// <returns>指定日期任一细目匹配过滤器的记账凭证</returns>
        IEnumerable<Voucher> SelectVouchersWithDetail(VoucherDetail filter, DateTime? startDate, DateTime? endDate);

        /// <summary>
        ///     按细目过滤器查找记账凭证
        /// </summary>
        /// <param name="filters">细目过滤器</param>
        /// <returns>任一细目匹配任一过滤器的记账凭证</returns>
        IEnumerable<Voucher> SelectVouchersWithDetail(IEnumerable<VoucherDetail> filters);

        /// <summary>
        ///     按过滤器和细目过滤器查找记账凭证
        ///     <para>若<paramref name="startDate" />和<paramref name="endDate" />均为<c>null</c>，则返回所有无日期的记账凭证</para>
        /// </summary>
        /// <param name="filters">细目过滤器</param>
        /// <param name="startDate">开始日期，若为<c>null</c>表示不检查最小日期，无日期亦可</param>
        /// <param name="endDate">截止日期，若为<c>null</c>表示不检查最大日期</param>
        /// <returns>指定日期任一细目匹配任一过滤器的记账凭证</returns>
        IEnumerable<Voucher> SelectVouchersWithDetail(IEnumerable<VoucherDetail> filters, DateTime? startDate,
                                                      DateTime? endDate);

        /// <summary>
        ///     按细目过滤器查找细目
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <returns>匹配过滤器的细目</returns>
        IEnumerable<VoucherDetail> SelectDetails(VoucherDetail filter);

        /// <summary>
        ///     按过滤器和细目过滤器查找细目
        ///     <para>若<paramref name="startDate" />和<paramref name="endDate" />均为<c>null</c>，则返回所有无日期的记账凭证</para>
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <param name="startDate">开始日期，若为<c>null</c>表示不检查最小日期，无日期亦可</param>
        /// <param name="endDate">截止日期，若为<c>null</c>表示不检查最大日期</param>
        /// <returns>指定日期匹配过滤器的细目</returns>
        IEnumerable<VoucherDetail> SelectDetails(VoucherDetail filter, DateTime? startDate, DateTime? endDate);

        /// <summary>
        ///     按细目过滤器查找细目并记数
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <returns>匹配过滤器的细目总数</returns>
        long SelectDetailsCount(VoucherDetail filter);

        /// <summary>
        ///     添加细目
        /// </summary>
        /// <param name="entity">细目</param>
        /// <returns>是否成功</returns>
        bool InsertDetail(VoucherDetail entity);

        /// <summary>
        ///     按过滤器删除细目
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <returns>已删除的细目总数</returns>
        int DeleteDetails(VoucherDetail filter);


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
        IEnumerable<Asset> SelectAssets(Asset filter);

        /// <summary>
        ///     添加资产
        ///     <para>若<paramref name="entity" />没有指定编号，则添加成功后会自动给<paramref name="entity" />添加编号</para>
        /// </summary>
        /// <param name="entity">资产</param>
        /// <returns>是否成功</returns>
        bool InsertAsset(Asset entity);

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
        int DeleteAssets(Asset filter);

        /// <summary>
        ///     添加或替换资产
        ///     <para>不能改变资产的编号</para>
        /// </summary>
        /// <param name="entity">新资产</param>
        /// <returns>是否成功</returns>
        bool UpdateAsset(Asset entity);

        //void Depreciate();
        //void Carry();
    }

    /// <summary>
    ///     数据库服务器接口
    /// </summary>
    public interface IDbServer
    {
        /// <summary>
        ///     启动服务器
        /// </summary>
        void Launch();

        /// <summary>
        ///     关闭服务器
        /// </summary>
        void Shutdown();
    }
}
