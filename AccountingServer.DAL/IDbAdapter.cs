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
using System.Threading.Tasks;
using AccountingServer.Entities;

namespace AccountingServer.DAL;

/// <summary>
///     数据库访问接口
/// </summary>
public interface IDbAdapter
{
    #region Voucher

    /// <summary>
    ///     按编号查找记账凭证
    /// </summary>
    /// <param name="id">编号</param>
    /// <returns>记账凭证，如果没有则为<c>null</c></returns>
    ValueTask<Voucher> SelectVoucher(string id);

    /// <summary>
    ///     按检索式查找记账凭证
    /// </summary>
    /// <param name="query">检索式</param>
    /// <returns>匹配检索式的记账凭证</returns>
    IAsyncEnumerable<Voucher> SelectVouchers(IQueryCompounded<IVoucherQueryAtom> query);

    /// <summary>
    ///     按检索式查找细目
    /// </summary>
    /// <param name="query">检索式</param>
    /// <returns>匹配检索式的细目</returns>
    IAsyncEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query);

    /// <summary>
    ///     按检索式执行记账凭证分类汇总
    /// </summary>
    /// <param name="query">检索式</param>
    /// <param name="limit">返回结果数量上限</param>
    /// <returns>匹配检索式的记账凭证数量</returns>
    IAsyncEnumerable<Balance> SelectVouchersGrouped(IVoucherGroupedQuery query, int limit = 0);

    /// <summary>
    ///     按检索式执行分类汇总
    /// </summary>
    /// <param name="query">检索式</param>
    /// <param name="limit">返回结果数量上限</param>
    /// <returns>分类汇总结果</returns>
    IAsyncEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query, int limit = 0);

    /// <summary>
    ///     查找借贷不平的记账凭证
    /// </summary>
    /// <param name="query">检索式</param>
    /// <returns>匹配检索式的记账凭证以及不匹配原因</returns>
    IAsyncEnumerable<(Voucher, string, string, double)> SelectUnbalancedVouchers(
        IQueryCompounded<IVoucherQueryAtom> query);

    /// <summary>
    ///     查找重复的记账凭证
    /// </summary>
    /// <param name="query">检索式</param>
    /// <returns>匹配检索式的记账凭证及其重复数量</returns>
    IAsyncEnumerable<(Voucher, List<string>)> SelectDuplicatedVouchers(IQueryCompounded<IVoucherQueryAtom> query);

    /// <summary>
    ///     按编号删除记账凭证
    /// </summary>
    /// <param name="id">编号</param>
    /// <returns>是否成功</returns>
    ValueTask<bool> DeleteVoucher(string id);

    /// <summary>
    ///     按检索式删除记账凭证
    /// </summary>
    /// <param name="query">检索式</param>
    /// <returns>已删除的细目总数</returns>
    ValueTask<long> DeleteVouchers(IQueryCompounded<IVoucherQueryAtom> query);

    /// <summary>
    ///     添加或替换记账凭证
    ///     <para>若无编号，则添加新编号</para>
    /// </summary>
    /// <param name="entity">新记账凭证</param>
    /// <returns>是否成功</returns>
    ValueTask<bool> Upsert(Voucher entity);

    /// <summary>
    ///     添加或替换多个记账凭证
    ///     <para>若无编号，则添加新编号</para>
    /// </summary>
    /// <param name="entities">新记账凭证</param>
    /// <returns>成功个数</returns>
    ValueTask<long> Upsert(IEnumerable<Voucher> entities);

    #endregion

    #region Asset

    /// <summary>
    ///     按编号查找资产
    /// </summary>
    /// <param name="id">编号</param>
    /// <returns>资产，如果没有则为<c>null</c></returns>
    ValueTask<Asset> SelectAsset(Guid id);

    /// <summary>
    ///     按记账凭证过滤器查找资产
    /// </summary>
    /// <param name="filter">记账凭证过滤器</param>
    /// <returns>匹配记账凭证过滤器的资产</returns>
    IAsyncEnumerable<Asset> SelectAssets(IQueryCompounded<IDistributedQueryAtom> filter);

    /// <summary>
    ///     按编号删除资产
    /// </summary>
    /// <param name="id">编号</param>
    /// <returns>是否成功</returns>
    ValueTask<bool> DeleteAsset(Guid id);

    /// <summary>
    ///     按记账凭证过滤器删除资产
    /// </summary>
    /// <param name="filter">记账凭证过滤器</param>
    /// <returns>已删除的资产总数</returns>
    ValueTask<long> DeleteAssets(IQueryCompounded<IDistributedQueryAtom> filter);

    /// <summary>
    ///     添加或替换资产
    ///     <para>若无编号，则添加新编号</para>
    /// </summary>
    /// <param name="entity">新资产</param>
    /// <returns>是否成功</returns>
    ValueTask<bool> Upsert(Asset entity);

    #endregion

    #region Amortization

    /// <summary>
    ///     按编号查找摊销
    /// </summary>
    /// <param name="id">编号</param>
    /// <returns>摊销，如果没有则为<c>null</c></returns>
    ValueTask<Amortization> SelectAmortization(Guid id);

    /// <summary>
    ///     按记账凭证过滤器查找摊销
    /// </summary>
    /// <param name="filter">记账凭证过滤器</param>
    /// <returns>匹配记账凭证过滤器的摊销</returns>
    IAsyncEnumerable<Amortization> SelectAmortizations(IQueryCompounded<IDistributedQueryAtom> filter);

    /// <summary>
    ///     按编号删除摊销
    /// </summary>
    /// <param name="id">编号</param>
    /// <returns>是否成功</returns>
    ValueTask<bool> DeleteAmortization(Guid id);

    /// <summary>
    ///     按记账凭证过滤器删除摊销
    /// </summary>
    /// <param name="filter">记账凭证过滤器</param>
    /// <returns>已删除的摊销总数</returns>
    ValueTask<long> DeleteAmortizations(IQueryCompounded<IDistributedQueryAtom> filter);

    /// <summary>
    ///     添加或替换摊销
    ///     <para>若无编号，则添加新编号</para>
    /// </summary>
    /// <param name="entity">新摊销</param>
    /// <returns>是否成功</returns>
    ValueTask<bool> Upsert(Amortization entity);

    #endregion

    #region ExchangeRecord

    /// <summary>
    ///     查找汇率
    /// </summary>
    /// <param name="record">过滤器</param>
    /// <returns>汇率，如果没有则为<c>null</c></returns>
    ValueTask<ExchangeRecord> SelectExchangeRecord(ExchangeRecord record);

    /// <summary>
    ///     添加或替换
    /// </summary>
    /// <param name="record">汇率</param>
    /// <returns>是否成功</returns>
    ValueTask<bool> Upsert(ExchangeRecord record);

    #endregion
}
