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
using AccountingServer.DAL;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using Xunit;

namespace AccountingServer.Test.IntegrationTest;

[Collection("DbTestCollection")]
public class BLLTest : IDisposable
{
    private readonly Accountant m_Accountant;

    public BLLTest()
    {
        m_Accountant = new(new(db: "accounting-test"), "b1", DateTime.UtcNow.Date);

        m_Accountant.DeleteVouchers(VoucherQueryUnconstrained.Instance);
    }

    public void Dispose()
        => m_Accountant.DeleteVouchers(VoucherQueryUnconstrained.Instance);

    [Theory]
    [ClassData(typeof(VoucherDataProvider))]
    public void VoucherStoreTest(string dt, VoucherType type)
    {
        var voucher1 = VoucherDataProvider.Create(dt, type);

        Assert.True(m_Accountant.Upsert(voucher1));
        Assert.NotNull(voucher1.ID);

        voucher1.Remark = "whatever";
        Assert.True(m_Accountant.Upsert(voucher1));

        var voucher2 = m_Accountant.SelectVouchers(VoucherQueryUnconstrained.Instance).Single();
        Assert.Equal(voucher1, voucher2, new VoucherEqualityComparer());

        var voucher3 = m_Accountant.SelectVoucher(voucher1.ID);
        Assert.Equal(voucher1, voucher3, new VoucherEqualityComparer());

        Assert.True(m_Accountant.DeleteVoucher(voucher1.ID));
        Assert.False(m_Accountant.DeleteVoucher(voucher1.ID));

        Assert.False(m_Accountant.SelectVouchers(VoucherQueryUnconstrained.Instance).Any());
    }

    [Fact]
    public void VoucherBulkStoreTest()
    {
        var vouchers = new VoucherDataProvider().Select(pars
            => VoucherDataProvider.Create((string)pars[0], (VoucherType)pars[1])).ToList();
        var cnt = vouchers.Count;

        Assert.Equal(cnt, m_Accountant.Upsert(vouchers));
        foreach (var voucher in vouchers)
            Assert.NotNull(voucher.ID);

        vouchers.AddRange(new VoucherDataProvider().Select(pars
            => VoucherDataProvider.Create((string)pars[0], (VoucherType)pars[1])));
        Assert.Equal(cnt * 2, m_Accountant.Upsert(vouchers));
        foreach (var voucher in vouchers)
            Assert.NotNull(voucher.ID);

        Assert.Equal(cnt * 2, m_Accountant.SelectVouchers(VoucherQueryUnconstrained.Instance).Count());

        Assert.Equal(cnt * 2, m_Accountant.DeleteVouchers(VoucherQueryUnconstrained.Instance));

        Assert.False(m_Accountant.SelectVouchers(VoucherQueryUnconstrained.Instance).Any());
    }

    [Theory]
    [ClassData(typeof(AssetDataProvider))]
    public void AssetStoreTest(string dt, DepreciationMethod type)
    {
        var asset1 = AssetDataProvider.Create(dt, type);
        foreach (var item in asset1.Schedule)
        {
            item.Value = 0;
            if (item is DevalueItem dev)
                dev.Amount = 0;
        }

        Assert.True(m_Accountant.Upsert(asset1));
        Assert.NotNull(asset1.ID);

        asset1.Remark = "whatever";
        Assert.True(m_Accountant.Upsert(asset1));

        if (asset1.Date.HasValue)
        {
            asset1 = AssetDataProvider.Create(dt, type);
            asset1.Remark = "whatever";
        }

        var asset2 = m_Accountant.SelectAssets(DistributedQueryUnconstrained.Instance).Single();
        Assert.Equal(asset1, asset2, new AssetEqualityComparer());

        var asset3 = m_Accountant.SelectAsset(asset1.ID!.Value);
        Assert.Equal(asset1, asset3, new AssetEqualityComparer());

        Assert.True(m_Accountant.DeleteAsset(asset1.ID.Value));
        Assert.False(m_Accountant.DeleteAsset(asset1.ID.Value));

        Assert.Equal(0, m_Accountant.DeleteAssets(DistributedQueryUnconstrained.Instance));

        Assert.False(m_Accountant.SelectAssets(DistributedQueryUnconstrained.Instance).Any());
    }

    [Theory]
    [ClassData(typeof(AmortDataProvider))]
    public void AmortStoreTest(string dt, AmortizeInterval type)
    {
        var amort1 = AmortDataProvider.Create(dt, type);
        foreach (var item in amort1.Schedule)
            item.Value = 0;

        Assert.True(m_Accountant.Upsert(amort1));
        Assert.NotNull(amort1.ID);

        amort1.Remark = "whatever";
        Assert.True(m_Accountant.Upsert(amort1));

        if (amort1.Date.HasValue)
        {
            amort1 = AmortDataProvider.Create(dt, type);
            amort1.Remark = "whatever";
        }

        var amort2 = m_Accountant.SelectAmortizations(DistributedQueryUnconstrained.Instance).Single();
        Assert.Equal(amort1, amort2, new AmortEqualityComparer());

        var amort3 = m_Accountant.SelectAmortization(amort1.ID!.Value);
        Assert.Equal(amort1, amort3, new AmortEqualityComparer());

        Assert.True(m_Accountant.DeleteAmortization(amort1.ID.Value));
        Assert.False(m_Accountant.DeleteAmortization(amort1.ID.Value));

        Assert.Equal(0, m_Accountant.DeleteAmortizations(DistributedQueryUnconstrained.Instance));

        Assert.False(m_Accountant.SelectAmortizations(DistributedQueryUnconstrained.Instance).Any());
    }

    [Fact]
    public void UriInvalidTest()
        => Assert.Throws<NotSupportedException>(() => Facade.Create("https:///"));
}
