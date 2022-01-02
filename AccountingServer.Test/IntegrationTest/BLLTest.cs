/* Copyright (C) 2020-2022 b1f6c1c4
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
using System.Threading.Tasks;
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

        m_Accountant.DeleteVouchersAsync(VoucherQueryUnconstrained.Instance).AsTask().Wait();
    }

    public void Dispose()
        => m_Accountant.DeleteVouchersAsync(VoucherQueryUnconstrained.Instance).AsTask().Wait();

    [Theory]
    [ClassData(typeof(VoucherDataProvider))]
    public async Task VoucherStoreTest(string dt, VoucherType type)
    {
        var voucher1 = VoucherDataProvider.Create(dt, type);

        Assert.True(await m_Accountant.UpsertAsync(voucher1));
        Assert.NotNull(voucher1.ID);

        voucher1.Remark = "whatever";
        Assert.True(await m_Accountant.UpsertAsync(voucher1));

        var voucher2 = await m_Accountant.SelectVouchersAsync(VoucherQueryUnconstrained.Instance).SingleAsync();
        Assert.Equal(voucher1, voucher2, new VoucherEqualityComparer());

        var voucher3 = await m_Accountant.SelectVoucherAsync(voucher1.ID);
        Assert.Equal(voucher1, voucher3, new VoucherEqualityComparer());

        Assert.True(await m_Accountant.DeleteVoucherAsync(voucher1.ID));
        Assert.False(await m_Accountant.DeleteVoucherAsync(voucher1.ID));

        Assert.False(await m_Accountant.SelectVouchersAsync(VoucherQueryUnconstrained.Instance).AnyAsync());
    }

    [Fact]
    public async Task VoucherBulkStoreTest()
    {
        var vouchers = new VoucherDataProvider().Select(static pars
            => VoucherDataProvider.Create((string)pars[0], (VoucherType)pars[1])).ToList();
        var cnt = vouchers.Count;

        Assert.Equal(cnt, await m_Accountant.UpsertAsync(vouchers));
        foreach (var voucher in vouchers)
            Assert.NotNull(voucher.ID);

        vouchers.AddRange(new VoucherDataProvider().Select(static pars
            => VoucherDataProvider.Create((string)pars[0], (VoucherType)pars[1])));
        Assert.Equal(cnt * 2, await m_Accountant.UpsertAsync(vouchers));
        foreach (var voucher in vouchers)
            Assert.NotNull(voucher.ID);

        Assert.Equal(cnt * 2, await m_Accountant.SelectVouchersAsync(VoucherQueryUnconstrained.Instance).CountAsync());

        Assert.Equal(cnt * 2, await m_Accountant.DeleteVouchersAsync(VoucherQueryUnconstrained.Instance));

        Assert.False(await m_Accountant.SelectVouchersAsync(VoucherQueryUnconstrained.Instance).AnyAsync());
    }

    [Theory]
    [ClassData(typeof(AssetDataProvider))]
    public async Task AssetStoreTest(string dt, DepreciationMethod type)
    {
        var asset1 = AssetDataProvider.Create(dt, type);
        foreach (var item in asset1.Schedule)
        {
            item.Value = 0;
            if (item is DevalueItem dev)
                dev.Amount = 0;
        }

        Assert.True(await m_Accountant.UpsertAsync(asset1));
        Assert.NotNull(asset1.ID);

        asset1.Remark = "whatever";
        Assert.True(await m_Accountant.UpsertAsync(asset1));

        if (asset1.Date.HasValue)
        {
            asset1 = AssetDataProvider.Create(dt, type);
            asset1.Remark = "whatever";
        }

        var asset2 = await m_Accountant.SelectAssetsAsync(DistributedQueryUnconstrained.Instance).SingleAsync();
        Assert.Equal(asset1, asset2, new AssetEqualityComparer());

        var asset3 = await m_Accountant.SelectAssetAsync(asset1.ID!.Value);
        Assert.Equal(asset1, asset3, new AssetEqualityComparer());

        Assert.True(await m_Accountant.DeleteAssetAsync(asset1.ID.Value));
        Assert.False(await m_Accountant.DeleteAssetAsync(asset1.ID.Value));

        Assert.Equal(0, await m_Accountant.DeleteAssetsAsync(DistributedQueryUnconstrained.Instance));

        Assert.False(await m_Accountant.SelectAssetsAsync(DistributedQueryUnconstrained.Instance).AnyAsync());
    }

    [Theory]
    [ClassData(typeof(AmortDataProvider))]
    public async Task AmortStoreTest(string dt, AmortizeInterval type)
    {
        var amort1 = AmortDataProvider.Create(dt, type);
        foreach (var item in amort1.Schedule)
            item.Value = 0;

        Assert.True(await m_Accountant.UpsertAsync(amort1));
        Assert.NotNull(amort1.ID);

        amort1.Remark = "whatever";
        Assert.True(await m_Accountant.UpsertAsync(amort1));

        if (amort1.Date.HasValue)
        {
            amort1 = AmortDataProvider.Create(dt, type);
            amort1.Remark = "whatever";
        }

        var amort2 = await m_Accountant.SelectAmortizationsAsync(DistributedQueryUnconstrained.Instance).SingleAsync();
        Assert.Equal(amort1, amort2, new AmortEqualityComparer());

        var amort3 = await m_Accountant.SelectAmortizationAsync(amort1.ID!.Value);
        Assert.Equal(amort1, amort3, new AmortEqualityComparer());

        Assert.True(await m_Accountant.DeleteAmortizationAsync(amort1.ID.Value));
        Assert.False(await m_Accountant.DeleteAmortizationAsync(amort1.ID.Value));

        Assert.Equal(0, await m_Accountant.DeleteAmortizationsAsync(DistributedQueryUnconstrained.Instance));

        Assert.False(await m_Accountant.SelectAmortizationsAsync(DistributedQueryUnconstrained.Instance).AnyAsync());
    }

    [Fact]
    public void UriInvalidTest()
        => Assert.Throws<NotSupportedException>(static () => Facade.Create("https:///"));
}
