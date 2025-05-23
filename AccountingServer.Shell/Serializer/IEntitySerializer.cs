/* Copyright (C) 2020-2025 b1f6c1c4
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

using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Serializer;

/// <summary>
///     表示器
/// </summary>
public interface IEntitySerializer
{
    /// <summary>
    ///     将记账凭证表示
    /// </summary>
    /// <param name="voucher">记账凭证</param>
    /// <returns>表示</returns>
    string PresentVoucher(Voucher voucher);

    /// <summary>
    ///     将记账凭证表示，并嵌入任意字符串
    /// </summary>
    /// <param name="voucher">记账凭证</param>
    /// <param name="inject">嵌入字符串</param>
    /// <returns>表示</returns>
    string PresentVoucher(Voucher voucher, string inject);

    /// <summary>
    ///     从表示中取得记账凭证
    /// </summary>
    /// <param name="str">表示</param>
    /// <returns>记账凭证</returns>
    Voucher ParseVoucher(string str);

    /// <summary>
    ///     将细目表示
    /// </summary>
    /// <param name="detail">细目</param>
    /// <returns>表示</returns>
    string PresentVoucherDetail(VoucherDetail detail);

    /// <summary>
    ///     将带记账凭证的细目表示
    /// </summary>
    /// <param name="detail">细目</param>
    /// <returns>表示</returns>
    string PresentVoucherDetail(VoucherDetailR detail);

    /// <summary>
    ///     从表示中取得记账凭证细目
    /// </summary>
    /// <param name="str">表达</param>
    /// <returns>细目</returns>
    VoucherDetail ParseVoucherDetail(string str);

    /// <summary>
    ///     将资产表示
    /// </summary>
    /// <param name="asset">资产</param>
    /// <returns>表示</returns>
    string PresentAsset(Asset asset);

    /// <summary>
    ///     从表示中取得资产
    /// </summary>
    /// <param name="str">表示</param>
    /// <returns>资产</returns>
    Asset ParseAsset(string str);

    /// <summary>
    ///     将摊销表示
    /// </summary>
    /// <param name="amort">摊销</param>
    /// <returns>表示</returns>
    string PresentAmort(Amortization amort);

    /// <summary>
    ///     从表示中取得摊销
    /// </summary>
    /// <param name="str">表示</param>
    /// <returns>摊销</returns>
    Amortization ParseAmort(string str);
}

internal interface IEntitiesSerializer : IEntitySerializer
{
    /// <summary>
    ///     将多个记账凭证表示
    /// </summary>
    /// <param name="vouchers">记账凭证</param>
    /// <returns>表示</returns>
    IAsyncEnumerable<string> PresentVouchers(IAsyncEnumerable<Voucher> vouchers);

    /// <summary>
    ///     将多个细目表示
    /// </summary>
    /// <param name="details">细目</param>
    /// <returns>表示</returns>
    IAsyncEnumerable<string> PresentVoucherDetails(IAsyncEnumerable<VoucherDetail> details);

    /// <summary>
    ///     将多个带记账凭证的细目表示
    /// </summary>
    /// <param name="details">细目</param>
    /// <returns>表示</returns>
    IAsyncEnumerable<string> PresentVoucherDetails(IAsyncEnumerable<VoucherDetailR> details);

    /// <summary>
    ///     将多个资产表示
    /// </summary>
    /// <param name="assets">资产</param>
    /// <returns>表示</returns>
    IAsyncEnumerable<string> PresentAssets(IAsyncEnumerable<Asset> assets);

    /// <summary>
    ///     将多个摊销表示
    /// </summary>
    /// <param name="amorts">摊销</param>
    /// <returns>表示</returns>
    IAsyncEnumerable<string> PresentAmorts(IAsyncEnumerable<Amortization> amorts);
}

internal static class SerializerHelper
{
    public static string Wrap(this string str) => $"@{str}@\n";
}

internal class TrivialEntitiesSerializer : IEntitiesSerializer
{
    private readonly IEntitySerializer m_Serializer;
    public TrivialEntitiesSerializer(IEntitySerializer serializer) => m_Serializer = serializer;

    public IAsyncEnumerable<string> PresentVouchers(IAsyncEnumerable<Voucher> vouchers)
        => vouchers.Select(voucher => m_Serializer.PresentVoucher(voucher).Wrap());

    public IAsyncEnumerable<string> PresentVoucherDetails(IAsyncEnumerable<VoucherDetail> details)
        => details.Select(detail => m_Serializer.PresentVoucherDetail(detail));

    public IAsyncEnumerable<string> PresentVoucherDetails(IAsyncEnumerable<VoucherDetailR> details)
        => details.Select(detail => m_Serializer.PresentVoucherDetail(detail));

    public IAsyncEnumerable<string> PresentAssets(IAsyncEnumerable<Asset> assets)
        => assets.Select(asset => m_Serializer.PresentAsset(asset).Wrap());

    public IAsyncEnumerable<string> PresentAmorts(IAsyncEnumerable<Amortization> amorts)
        => amorts.Select(amort => m_Serializer.PresentAmort(amort).Wrap());

    public string PresentVoucher(Voucher voucher) => m_Serializer.PresentVoucher(voucher);
    public string PresentVoucher(Voucher voucher, string inject) => m_Serializer.PresentVoucher(voucher, inject);
    public Voucher ParseVoucher(string str) => m_Serializer.ParseVoucher(str);
    public string PresentVoucherDetail(VoucherDetail detail) => m_Serializer.PresentVoucherDetail(detail);
    public string PresentVoucherDetail(VoucherDetailR detail) => m_Serializer.PresentVoucherDetail(detail);
    public VoucherDetail ParseVoucherDetail(string str) => m_Serializer.ParseVoucherDetail(str);
    public string PresentAsset(Asset asset) => m_Serializer.PresentAsset(asset);
    public Asset ParseAsset(string str) => m_Serializer.ParseAsset(str);
    public string PresentAmort(Amortization amort) => m_Serializer.PresentAmort(amort);
    public Amortization ParseAmort(string str) => m_Serializer.ParseAmort(str);
}
