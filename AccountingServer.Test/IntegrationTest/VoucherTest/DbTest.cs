using System;
using System.Collections.Generic;
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
        private readonly IDbAdapter m_Adapter;

        public DbTest()
        {
            m_Adapter = Facade.Create("mongodb://localhost/accounting-test");

            m_Adapter.DeleteVouchers(VoucherQueryUnconstrained.Instance);

            ClientUser.Set("b1");
        }

        public void Dispose()
            => m_Adapter.DeleteVouchers(VoucherQueryUnconstrained.Instance);

        [Theory]
        [InlineData(null, VoucherType.Ordinary)]
        [InlineData("2017-01-01", VoucherType.Uncertain)]
        [InlineData(null, VoucherType.Carry)]
        [InlineData(null, VoucherType.AnnualCarry)]
        [InlineData("2017-01-01", VoucherType.Depreciation)]
        [InlineData(null, VoucherType.Devalue)]
        [InlineData("2017-01-01", VoucherType.Amortization)]
        public void VoucherStoreTest(string dt, VoucherType type)
        {
            var voucher1 = new Voucher
                {
                    Date = dt.ToDateTime(),
                    Type = type,
                    Remark = "tt",
                    Details = new List<VoucherDetail>
                        {
                            new VoucherDetail
                                {
                                    User = "b1",
                                    Currency = "CNY",
                                    Title = 1001,
                                    Content = "asdf",
                                    Fund = 123.45,
                                },
                            new VoucherDetail
                                {
                                    User = "b2",
                                    Currency = "USD",
                                    Title = 1002,
                                    SubTitle = 12,
                                    Fund = -123.45,
                                    Remark = "qwer",
                                },
                        },
                };

            m_Adapter.Upsert(voucher1);

            var voucher2 = m_Adapter.SelectVouchers(VoucherQueryUnconstrained.Instance).Single();
            Assert.Equal(voucher1, voucher2, new VoucherEqualityComparer());

            var voucher3 = m_Adapter.SelectVoucher(voucher1.ID);
            Assert.Equal(voucher1, voucher3, new VoucherEqualityComparer());

            Assert.True(m_Adapter.DeleteVoucher(voucher1.ID));
            Assert.False(m_Adapter.DeleteVoucher(voucher1.ID));

            Assert.False(m_Adapter.SelectVouchers(VoucherQueryUnconstrained.Instance).Any());
        }
    }
}
