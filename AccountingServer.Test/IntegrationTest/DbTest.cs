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
using AccountingServer.DAL;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using Xunit;

namespace AccountingServer.Test.IntegrationTest;

[Collection("DbTestCollection")]
public class DbTest : IDisposable
{
    private readonly IDbAdapter m_Adapter;

    public DbTest()
    {
        m_Adapter = Facade.Create(db: "accounting-test");

        m_Adapter.DeleteVouchers(VoucherQueryUnconstrained.Instance);
    }

    public void Dispose()
        => m_Adapter.DeleteVouchers(VoucherQueryUnconstrained.Instance);

    [Theory]
    [ClassData(typeof(VoucherDataProvider))]
    public void VoucherStoreTest(string dt, VoucherType type)
    {
        var voucher1 = VoucherDataProvider.Create(dt, type);

        Assert.True(m_Adapter.Upsert(voucher1).Result);
        Assert.NotNull(voucher1.ID);

        voucher1.Remark = "whatever";
        Assert.True(m_Adapter.Upsert(voucher1).Result);

        var voucher2 = m_Adapter.SelectVouchers(VoucherQueryUnconstrained.Instance).SingleAsync().Result;
        Assert.Equal(voucher1, voucher2, new VoucherEqualityComparer());

        var voucher3 = m_Adapter.SelectVoucher(voucher1.ID).Result;
        Assert.Equal(voucher1, voucher3, new VoucherEqualityComparer());

        Assert.True(m_Adapter.DeleteVoucher(voucher1.ID).Result);
        Assert.False(m_Adapter.DeleteVoucher(voucher1.ID).Result);

        Assert.False(m_Adapter.SelectVouchers(VoucherQueryUnconstrained.Instance).AnyAsync().Result);
    }

    [Fact]
    public void VoucherBulkStoreTest()
    {
        var vouchers = new VoucherDataProvider().Select(pars
            => VoucherDataProvider.Create((string)pars[0], (VoucherType)pars[1])).ToList();
        var cnt = vouchers.Count;

        Assert.Equal(cnt, m_Adapter.Upsert(vouchers).Result);
        foreach (var voucher in vouchers)
            Assert.NotNull(voucher.ID);

        vouchers.AddRange(new VoucherDataProvider().Select(pars
            => VoucherDataProvider.Create((string)pars[0], (VoucherType)pars[1])));
        Assert.Equal(cnt * 2, m_Adapter.Upsert(vouchers).Result);
        foreach (var voucher in vouchers)
            Assert.NotNull(voucher.ID);

        Assert.Equal(cnt * 2, m_Adapter.SelectVouchers(VoucherQueryUnconstrained.Instance).CountAsync().Result);

        Assert.Equal(cnt * 2, m_Adapter.DeleteVouchers(VoucherQueryUnconstrained.Instance).Result);

        Assert.False(m_Adapter.SelectVouchers(VoucherQueryUnconstrained.Instance).AnyAsync().Result);
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

        Assert.True(m_Adapter.Upsert(asset1).Result);
        Assert.NotNull(asset1.ID);

        asset1.Remark = "whatever";
        Assert.True(m_Adapter.Upsert(asset1).Result);

        var asset2 = m_Adapter.SelectAssets(DistributedQueryUnconstrained.Instance).SingleAsync().Result;
        Assert.Equal(asset1, asset2, new AssetEqualityComparer());

        var asset3 = m_Adapter.SelectAsset(asset1.ID.Value).Result;
        Assert.Equal(asset1, asset3, new AssetEqualityComparer());

        Assert.True(m_Adapter.DeleteAsset(asset1.ID.Value).Result);
        Assert.False(m_Adapter.DeleteAsset(asset1.ID.Value).Result);

        Assert.Equal(0, m_Adapter.DeleteAssets(DistributedQueryUnconstrained.Instance).Result);

        Assert.False(m_Adapter.SelectAssets(DistributedQueryUnconstrained.Instance).AnyAsync().Result);
    }

    [Theory]
    [ClassData(typeof(AmortDataProvider))]
    public void AmortStoreTest(string dt, AmortizeInterval type)
    {
        var amort1 = AmortDataProvider.Create(dt, type);
        foreach (var item in amort1.Schedule)
            item.Value = 0;

        Assert.True(m_Adapter.Upsert(amort1).Result);
        Assert.NotNull(amort1.ID);

        amort1.Remark = "whatever";
        Assert.True(m_Adapter.Upsert(amort1).Result);

        var amort2 = m_Adapter.SelectAmortizations(DistributedQueryUnconstrained.Instance).SingleAsync().Result;
        Assert.Equal(amort1, amort2, new AmortEqualityComparer());

        var amort3 = m_Adapter.SelectAmortization(amort1.ID.Value).Result;
        Assert.Equal(amort1, amort3, new AmortEqualityComparer());

        Assert.True(m_Adapter.DeleteAmortization(amort1.ID.Value).Result);
        Assert.False(m_Adapter.DeleteAmortization(amort1.ID.Value).Result);

        Assert.Equal(0, m_Adapter.DeleteAmortizations(DistributedQueryUnconstrained.Instance).Result);

        Assert.False(m_Adapter.SelectAmortizations(DistributedQueryUnconstrained.Instance).AnyAsync().Result);
    }

    [Fact]
    public void UriInvalidTest()
        => Assert.Throws<NotSupportedException>(() => Facade.Create("http:///"));
}
