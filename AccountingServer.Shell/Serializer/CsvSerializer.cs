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
using System.Linq;
using System.Text;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Serializer;

/// <summary>
///     Csv表达式
/// </summary>
public class CsvSerializer : IEntitiesSerializer
{
    private readonly string m_Sep = "\t";
    private readonly List<ColumnSpec> m_Specs = new();

    public CsvSerializer(string spec)
    {
        {
            var sep = Parsing.Quoted(ref spec, '\'');
            if (sep != null)
                m_Sep = sep;
        }

        if (string.IsNullOrWhiteSpace(spec))
        {
            m_Specs.Add(ColumnSpec.VoucherID);
            m_Specs.Add(ColumnSpec.VoucherDate);
            m_Specs.Add(ColumnSpec.User);
            m_Specs.Add(ColumnSpec.Currency);
            m_Specs.Add(ColumnSpec.Title);
            m_Specs.Add(ColumnSpec.TitleComment);
            m_Specs.Add(ColumnSpec.SubTitle);
            m_Specs.Add(ColumnSpec.SubTitleComment);
            m_Specs.Add(ColumnSpec.Content);
            m_Specs.Add(ColumnSpec.Remark);
            m_Specs.Add(ColumnSpec.Fund);
            return;
        }

        while (!string.IsNullOrWhiteSpace(spec))
        {
            var sep = Parsing.Quoted(ref spec, '\'');
            if (sep != null)
            {
                m_Sep = sep;
                continue;
            }

            var s = Parsing.Token(ref spec, false);
            switch (s)
            {
                case "id":
                    m_Specs.Add(ColumnSpec.VoucherID);
                    break;
                case "d":
                case "date":
                    m_Specs.Add(ColumnSpec.VoucherDate);
                    break;
                case "type":
                    m_Specs.Add(ColumnSpec.VoucherType);
                    break;
                case "U":
                case "user":
                    m_Specs.Add(ColumnSpec.User);
                    break;
                case "C":
                case "currency":
                    m_Specs.Add(ColumnSpec.Currency);
                    break;
                case "t":
                case "title":
                    m_Specs.Add(ColumnSpec.Title);
                    break;
                case "t'":
                    m_Specs.Add(ColumnSpec.TitleComment);
                    break;
                case "s":
                case "subtitle":
                    m_Specs.Add(ColumnSpec.SubTitle);
                    break;
                case "s'":
                    m_Specs.Add(ColumnSpec.SubTitleComment);
                    break;
                case "c":
                case "content":
                    m_Specs.Add(ColumnSpec.Content);
                    break;
                case "r":
                case "remark":
                    m_Specs.Add(ColumnSpec.Remark);
                    break;
                case "v":
                case "fund":
                    m_Specs.Add(ColumnSpec.Fund);
                    break;
            }
        }
    }

    private IList<ColumnSpec> DetailSpec => m_Specs.Where(s => !s.HasFlag(ColumnSpec.Voucher)).ToList();

    /// <inheritdoc />
    public string PresentVoucher(Voucher voucher)
    {
        var sb = new StringBuilder();
        sb.AppendLine(PresentHeader(m_Specs));
        if (voucher == null)
            return "";
        foreach (var d in voucher.Details)
            sb.AppendLine(Present(new VoucherDetailR(voucher, d), m_Specs));
        return sb.ToString();
    }

    /// <inheritdoc />
    public string PresentVoucherDetail(VoucherDetail detail)
    {
        var sb = new StringBuilder();
        sb.AppendLine(PresentHeader(DetailSpec));
        sb.AppendLine(Present(new(null, detail), DetailSpec));
        return sb.ToString();
    }

    /// <inheritdoc />
    public string PresentVoucherDetail(VoucherDetailR detail)
    {
        var sb = new StringBuilder();
        sb.AppendLine(PresentHeader(m_Specs));
        sb.AppendLine(Present(detail, m_Specs));
        return sb.ToString();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> PresentVouchers(IAsyncEnumerable<Voucher> vouchers)
    {
        yield return PresentHeader(m_Specs);
        await foreach (var voucher in vouchers)
        foreach (var detail in voucher.Details)
            yield return Present(new(voucher, detail), m_Specs);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> PresentVoucherDetails(IAsyncEnumerable<VoucherDetail> details)
    {
        yield return PresentHeader(DetailSpec);
        await foreach (var detail in details)
            yield return Present(new(null, detail), DetailSpec);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> PresentVoucherDetails(IAsyncEnumerable<VoucherDetailR> details)
    {
        yield return PresentHeader(m_Specs);
        await foreach (var detail in details)
            yield return Present(detail, m_Specs);
    }

    public Voucher ParseVoucher(string str) => throw new NotImplementedException();
    public VoucherDetail ParseVoucherDetail(string str) => throw new NotImplementedException();
    public string PresentAsset(Asset asset) => throw new NotImplementedException();
    public Asset ParseAsset(string str) => throw new NotImplementedException();
    public string PresentAmort(Amortization amort) => throw new NotImplementedException();
    public Amortization ParseAmort(string str) => throw new NotImplementedException();

    public IAsyncEnumerable<string> PresentAssets(IAsyncEnumerable<Asset> assets)
        => throw new NotImplementedException();

    public IAsyncEnumerable<string> PresentAmorts(IAsyncEnumerable<Amortization> amorts)
        => throw new NotImplementedException();

    private string PresentHeader(IList<ColumnSpec> spec)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < spec.Count; i++)
        {
            var s = spec[i];
            if (i > 0)
                sb.Append(m_Sep);
            sb.Append(s);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     将带记账凭证的细目转换为Csv表示
    /// </summary>
    /// <param name="d">细目</param>
    /// <param name="spec">列</param>
    /// <returns>Csv表示</returns>
    private string Present(VoucherDetailR d, IList<ColumnSpec> spec)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < spec.Count; i++)
        {
            if (i > 0)
                sb.Append(m_Sep);

            sb.Append(spec[i] switch
                {
                    ColumnSpec.VoucherID => d.Voucher.ID,
                    ColumnSpec.VoucherDate => d.Voucher.Date.AsDate(),
                    ColumnSpec.VoucherType => d.Voucher.Type,
                    ColumnSpec.User => d.User,
                    ColumnSpec.Currency => d.Currency,
                    ColumnSpec.Title => d.Title.AsTitle(),
                    ColumnSpec.TitleComment => TitleManager.GetTitleName(d.Title),
                    ColumnSpec.SubTitle => d.SubTitle.AsSubTitle(),
                    ColumnSpec.SubTitleComment => TitleManager.GetTitleName(d.Title, d.SubTitle),
                    ColumnSpec.Content => d.Content.Quotation('\''),
                    ColumnSpec.Remark => d.Remark.Quotation('"'),
                    ColumnSpec.Fund => $"{d.Fund:R}",
                    _ => throw new ArgumentOutOfRangeException(),
                });
        }

        return sb.ToString();
    }

    [Flags]
    private enum ColumnSpec
    {
        Voucher = 0x1000,
        VoucherID = 0x1001,
        VoucherDate = 0x1002,
        VoucherType = 0x1003,
        User = 0xc,
        Currency = 0x4,
        Title = 0x5,
        TitleComment = 0x6,
        SubTitle = 0x7,
        SubTitleComment = 0x8,
        Content = 0x9,
        Remark = 0xa,
        Fund = 0xb,
    }
}
