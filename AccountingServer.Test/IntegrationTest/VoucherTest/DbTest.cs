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
            m_Adapter.DeleteAssets(DistributedQueryUnconstrained.Instance);
            m_Adapter.DeleteAmortizations(DistributedQueryUnconstrained.Instance);
        }

        public void Dispose()
        {
            m_Adapter.DeleteVouchers(VoucherQueryUnconstrained.Instance);
            m_Adapter.DeleteAssets(DistributedQueryUnconstrained.Instance);
            m_Adapter.DeleteAmortizations(DistributedQueryUnconstrained.Instance);
        }

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
                                    Currency = VoucherDetail.BaseCurrency,
                                    Title = 1001,
                                    Content = "asdf",
                                    Fund = 123.45
                                },
                            new VoucherDetail
                                {
                                    Currency = "USD",
                                    Title = 1002,
                                    SubTitle = 12,
                                    Fund = -123.45,
                                    Remark = "qwer"
                                }
                        }
                };

            m_Adapter.Upsert(voucher1);

            var voucher2 = m_Adapter.SelectVouchers(VoucherQueryUnconstrained.Instance).Single();

            Assert.Equal(voucher1, voucher2, new VoucherEqualityComparer());
        }
    }
}
