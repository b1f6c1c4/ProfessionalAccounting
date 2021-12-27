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
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Serializer;

internal class AlternativeSerializer : IEntitySerializer
{
    /// <summary>
    ///     主要表示器
    /// </summary>
    private readonly IEntitySerializer m_Primary;

    /// <summary>
    ///     次要表示器
    /// </summary>
    private readonly IEntitySerializer m_Secondary;

    private AlternativeSerializer(IEntitySerializer primary, IEntitySerializer secondary)
    {
        m_Primary = primary;
        m_Secondary = secondary;
    }

    public string PresentVoucher(Voucher voucher, Client client) => Run(s => s.PresentVoucher(voucher, client));
    public Voucher ParseVoucher(string str, Client client) => Run(s => s.ParseVoucher(str, client));
    public string PresentVoucherDetail(VoucherDetail detail, Client client) => Run(s => s.PresentVoucherDetail(detail, client));
    public string PresentVoucherDetail(VoucherDetailR detail, Client client) => Run(s => s.PresentVoucherDetail(detail, client));
    public VoucherDetail ParseVoucherDetail(string str, Client client) => Run(s => s.ParseVoucherDetail(str, client));
    public string PresentAsset(Asset asset, Client client) => Run(s => s.PresentAsset(asset, client));
    public Asset ParseAsset(string str, Client client) => Run(s => s.ParseAsset(str, client));
    public string PresentAmort(Amortization amort, Client client) => Run(s => s.PresentAmort(amort, client));
    public Amortization ParseAmort(string str, Client client) => Run(s => s.ParseAmort(str, client));

    public static IEntitySerializer Compose(params IEntitySerializer[] serializers)
        => serializers.Aggregate((p, s) => new AlternativeSerializer(p, s));

    private TOut Run<TOut>(Func<IEntitySerializer, TOut> func)
    {
        try
        {
            return func(m_Primary);
        }
        catch (NotImplementedException)
        {
            return func(m_Secondary);
        }
    }
}
