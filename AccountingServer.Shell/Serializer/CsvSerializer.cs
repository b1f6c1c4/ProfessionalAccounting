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
using AccountingServer.BLL;
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

    /// <inheritdoc />
    public string PresentVoucher(Voucher voucher, Client client)
        => voucher == null ? "" : PresentVouchers(new[] { voucher }, client);

    /// <inheritdoc />
    public string PresentVoucherDetail(VoucherDetail detail, Client client) => PresentVoucherDetails(new[] { detail }, client);

    /// <inheritdoc />
    public string PresentVoucherDetail(VoucherDetailR detail, Client client) => PresentVoucherDetails(new[] { detail }, client);

    /// <inheritdoc />
    public string PresentVouchers(IEnumerable<Voucher> vouchers, Client client)
        => PresentVoucherDetails(vouchers.SelectMany(v => v.Details.Select(d => new VoucherDetailR(v, d))), client);

    /// <inheritdoc />
    public string PresentVoucherDetails(IEnumerable<VoucherDetail> details, Client client)
        => Present(
            details.Select(d => new VoucherDetailR(null, d)),
            m_Specs.Where(s => !s.HasFlag(ColumnSpec.Voucher)).ToList());

    /// <inheritdoc />
    public string PresentVoucherDetails(IEnumerable<VoucherDetailR> details, Client client)
        => Present(details, m_Specs);

    public Voucher ParseVoucher(string str, Client client) => throw new NotImplementedException();
    public VoucherDetail ParseVoucherDetail(string str, Client client) => throw new NotImplementedException();
    public string PresentAsset(Asset asset, Client client) => throw new NotImplementedException();
    public Asset ParseAsset(string str, Client client) => throw new NotImplementedException();
    public string PresentAmort(Amortization amort, Client client) => throw new NotImplementedException();
    public Amortization ParseAmort(string str, Client client) => throw new NotImplementedException();

    public string PresentAssets(IEnumerable<Asset> assets, Client client) => throw new NotImplementedException();
    public string PresentAmorts(IEnumerable<Amortization> amorts, Client client) => throw new NotImplementedException();

    /// <summary>
    ///     将带记账凭证的细目转换为Csv表示
    /// </summary>
    /// <param name="details">细目</param>
    /// <param name="spec">列</param>
    /// <returns>Csv表示</returns>
    private string Present(IEnumerable<VoucherDetailR> details, IList<ColumnSpec> spec)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < spec.Count; i++)
        {
            var s = spec[i];
            if (i > 0)
                sb.Append(m_Sep);
            sb.Append(s);
        }

        sb.AppendLine();

        foreach (var d in details)
        {
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

            sb.AppendLine();
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
