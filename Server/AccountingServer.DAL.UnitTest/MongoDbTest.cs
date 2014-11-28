using System;
using System.Linq;
using AccountingServer.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AccountingServer.DAL.UnitTest
{
    [TestClass]
    public class MongoDbTest
    {
        private readonly MongoDbHelper m_MongoDb;

        public MongoDbTest()
        {
            m_MongoDb = new MongoDbHelper();
        }

        [TestMethod]
        public void Vouchers()
        {
            var voucher1 = new Voucher
                              {
                                  Date = DateTime.Now,
                                  Type = VoucherType.Ordinal,
                                  Details =
                                      new[]
                                          {
                                              new VoucherDetail
                                                  {
                                                      Title = 1001,
                                                      Fund = -48
                                                  },
                                              new VoucherDetail
                                                  {
                                                      Title = 6602,
                                                      SubTitle = 3,
                                                      Fund = 48,
                                                      Content = "庆丰包子铺"
                                                  }
                                          }
                              };
            m_MongoDb.DeleteVouchers(new Voucher());
            m_MongoDb.InsertVoucher(voucher1);
            var res = m_MongoDb.SelectVouchers(new Voucher()).ToArray();
            Assert.AreEqual(res.Length, 1);
            Assert.AreEqual(res[0], voucher1);
        }
    }
}
