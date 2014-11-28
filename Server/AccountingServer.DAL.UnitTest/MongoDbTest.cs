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
            Assert.AreEqual(1, res.Length);
            Assert.AreEqual(voucher1.Date.Value.Ticks, res[0].Date.Value.Ticks, 100000);
        }

        [TestMethod]
        public void VoucherDetails()
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
            var voucher2 = new Voucher
                               {
                                   Date = DateTime.Now,
                                   Type = VoucherType.Ordinal,
                                   Details =
                                       new[]
                                           {
                                               new VoucherDetail
                                                   {
                                                       Title = 1002,
                                                       Fund = -100
                                                   },
                                               new VoucherDetail
                                                   {
                                                       Title = 6602,
                                                       SubTitle = 8,
                                                       Fund = 100
                                                   }
                                           }
                               };
            m_MongoDb.DeleteVouchers(new Voucher());
            m_MongoDb.InsertVoucher(voucher1);
            m_MongoDb.InsertVoucher(voucher2);
            m_MongoDb.DeleteDetails(new VoucherDetail { Title = 6602 });
            var res = m_MongoDb.SelectVouchers(new Voucher()).ToArray();
            Assert.AreEqual(2, res.Length);
            Assert.AreEqual(1, res[0].Details.Length);
            Assert.AreEqual(1, res[1].Details.Length);
        }
    }
}
