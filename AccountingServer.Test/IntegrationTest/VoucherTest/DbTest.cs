using System;
using System.Linq;
using AccountingServer.DAL;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using Xunit;

namespace AccountingServer.Test.IntegrationTest.VoucherTest
{
    [Collection("DbTestCollection")]
    public class DbTest : IDisposable
    {
        public DbTest()
        {
            m_Adapter = Facade.Create(db: "accounting-test");

            m_Adapter.DeleteVouchers(VoucherQueryUnconstrained.Instance);

            ClientUser.Set("b1");
        }

        public void Dispose()
            => m_Adapter.DeleteVouchers(VoucherQueryUnconstrained.Instance);

        private readonly IDbAdapter m_Adapter;

        [Theory]
        [ClassData(typeof(VoucherDataProvider))]
        public void VoucherStoreTest(string dt, VoucherType type)
        {
            var voucher1 = VoucherDataProvider.Create(dt, type);

            m_Adapter.Upsert(voucher1);
            Assert.NotNull(voucher1.ID);

            var voucher2 = m_Adapter.SelectVouchers(VoucherQueryUnconstrained.Instance).Single();
            Assert.Equal(voucher1, voucher2, new VoucherEqualityComparer());

            var voucher3 = m_Adapter.SelectVoucher(voucher1.ID);
            Assert.Equal(voucher1, voucher3, new VoucherEqualityComparer());

            Assert.True(m_Adapter.DeleteVoucher(voucher1.ID));
            Assert.False(m_Adapter.DeleteVoucher(voucher1.ID));

            Assert.False(m_Adapter.SelectVouchers(VoucherQueryUnconstrained.Instance).Any());
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

            m_Adapter.Upsert(asset1);
            Assert.NotNull(asset1.ID);

            var asset2 = m_Adapter.SelectAssets(DistributedQueryUnconstrained.Instance).Single();
            Assert.Equal(asset1, asset2, new AssetEqualityComparer());

            var asset3 = m_Adapter.SelectAsset(asset1.ID.Value);
            Assert.Equal(asset1, asset3, new AssetEqualityComparer());

            Assert.True(m_Adapter.DeleteAsset(asset1.ID.Value));
            Assert.False(m_Adapter.DeleteAsset(asset1.ID.Value));

            Assert.False(m_Adapter.SelectAssets(DistributedQueryUnconstrained.Instance).Any());
        }

        [Theory]
        [ClassData(typeof(AmortDataProvider))]
        public void AmortStoreTest(string dt, AmortizeInterval type)
        {
            var amort1 = AmortDataProvider.Create(dt, type);
            foreach (var item in amort1.Schedule)
                item.Value = 0;

            m_Adapter.Upsert(amort1);
            Assert.NotNull(amort1.ID);

            var amort2 = m_Adapter.SelectAmortizations(DistributedQueryUnconstrained.Instance).Single();
            Assert.Equal(amort1, amort2, new AmortEqualityComparer());

            var amort3 = m_Adapter.SelectAmortization(amort1.ID.Value);
            Assert.Equal(amort1, amort3, new AmortEqualityComparer());

            Assert.True(m_Adapter.DeleteAmortization(amort1.ID.Value));
            Assert.False(m_Adapter.DeleteAmortization(amort1.ID.Value));

            Assert.False(m_Adapter.SelectAmortizations(DistributedQueryUnconstrained.Instance).Any());
        }

        [Fact]
        public void UriInvalidTest()
            => Assert.Throws<NotSupportedException>(() => Facade.Create("http:///"));
    }
}
