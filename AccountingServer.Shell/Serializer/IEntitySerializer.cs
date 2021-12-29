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
using System.Text;
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
    string PresentVouchers(IEnumerable<Voucher> vouchers);

    /// <summary>
    ///     将多个细目表示
    /// </summary>
    /// <param name="details">细目</param>
    /// <returns>表示</returns>
    string PresentVoucherDetails(IEnumerable<VoucherDetail> details);

    /// <summary>
    ///     将多个带记账凭证的细目表示
    /// </summary>
    /// <param name="details">细目</param>
    /// <returns>表示</returns>
    string PresentVoucherDetails(IEnumerable<VoucherDetailR> details);

    /// <summary>
    ///     将多个资产表示
    /// </summary>
    /// <param name="assets">资产</param>
    /// <returns>表示</returns>
    string PresentAssets(IEnumerable<Asset> assets);

    /// <summary>
    ///     将多个摊销表示
    /// </summary>
    /// <param name="amorts">摊销</param>
    /// <returns>表示</returns>
    string PresentAmorts(IEnumerable<Amortization> amorts);
}

internal static class SerializerHelper
{
    public static string Wrap(this string str) => $"@{str}@" + Environment.NewLine;
}

internal class TrivialEntitiesSerializer : IEntitiesSerializer
{
    private readonly IEntitySerializer m_Serializer;
    public TrivialEntitiesSerializer(IEntitySerializer serializer) => m_Serializer = serializer;

    public string PresentVouchers(IEnumerable<Voucher> vouchers)
    {
        var sb = new StringBuilder();
        foreach (var voucher in vouchers)
            sb.Append(m_Serializer.PresentVoucher(voucher).Wrap());
        return sb.ToString();
    }

    public string PresentVoucherDetails(IEnumerable<VoucherDetail> details)
    {
        var sb = new StringBuilder();
        foreach (var detail in details)
            sb.Append(m_Serializer.PresentVoucherDetail(detail));
        return sb.ToString();
    }

    public string PresentVoucherDetails(IEnumerable<VoucherDetailR> details)
    {
        var sb = new StringBuilder();
        foreach (var detail in details)
            sb.Append(m_Serializer.PresentVoucherDetail(detail));
        return sb.ToString();
    }

    public string PresentAssets(IEnumerable<Asset> assets)
    {
        var sb = new StringBuilder();
        foreach (var asset in assets)
            sb.Append(m_Serializer.PresentAsset(asset).Wrap());
        return sb.ToString();
    }

    public string PresentAmorts(IEnumerable<Amortization> amorts)
    {
        var sb = new StringBuilder();
        foreach (var amort in amorts)
            sb.Append(m_Serializer.PresentAmort(amort).Wrap());
        return sb.ToString();
    }

    public string PresentVoucher(Voucher voucher) => m_Serializer.PresentVoucher(voucher);
    public Voucher ParseVoucher(string str) => m_Serializer.ParseVoucher(str);
    public string PresentVoucherDetail(VoucherDetail detail) => m_Serializer.PresentVoucherDetail(detail);
    public string PresentVoucherDetail(VoucherDetailR detail) => m_Serializer.PresentVoucherDetail(detail);
    public VoucherDetail ParseVoucherDetail(string str) => m_Serializer.ParseVoucherDetail(str);
    public string PresentAsset(Asset asset) => m_Serializer.PresentAsset(asset);
    public Asset ParseAsset(string str) => m_Serializer.ParseAsset(str);
    public string PresentAmort(Amortization amort) => m_Serializer.PresentAmort(amort);
    public Amortization ParseAmort(string str) => m_Serializer.ParseAmort(str);
}
