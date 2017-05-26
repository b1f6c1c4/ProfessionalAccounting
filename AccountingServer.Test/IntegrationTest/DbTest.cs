using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.DAL;
using AccountingServer.Entities;
using Xunit;

namespace AccountingServer.Test.IntegrationTest
{
    [Collection("DbTestCollection")]
    public class DbTest : IDisposable
    {
        private readonly IDbAdapter m_Adapter;

        public DbTest()
        {
            m_Adapter = Facade.Create("mongodb://localhost/accounting-test");

            m_Adapter.DeleteVouchers(null);
            m_Adapter.DeleteAssets(null);
            m_Adapter.DeleteAmortizations(null);
        }

        public void Dispose()
        {
            m_Adapter.DeleteVouchers(null);
            m_Adapter.DeleteAssets(null);
            m_Adapter.DeleteAmortizations(null);
        }

        [Fact]
        public void VoucherStoreTest()
        {
            var voucher1 = new Voucher
                {
                    Type = VoucherType.Ordinary,
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

            var voucher2 = m_Adapter.SelectVouchers(null).Single();

            Assert.Equal(voucher1, voucher2, new VoucherEqualityComparer());
        }
    }
}
