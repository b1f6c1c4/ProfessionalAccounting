using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccountingServer.BLL;
using AccountingServer.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AccountingServer.UnitTest
{
    [TestClass]
    public class AccountantTest
    {
        //private readonly Accountant m_Accountant;

        //public AccountantTest() { m_Accountant = new Accountant(); }

        [TestMethod]
        public void ProcessDaily()
        {
            var res = Accountant.ProcessDailyBalance(
                                                     new[]
                                                         {
                                                             new Balance { Date = null, Fund = 1D },
                                                             new Balance { Date = DateTime.Parse("2014-01-01"), Fund = 10D },
                                                             new Balance { Date = DateTime.Parse("2014-01-02"), Fund = 100D },
                                                             new Balance { Date = DateTime.Parse("2014-01-04"), Fund = 1000D },
                                                             new Balance { Date = DateTime.Parse("2014-01-05"), Fund = 10000D },
                                                         },
                                                     DateFilter.Unconstrained).ToList();
            var exp = new[]
                          {
                              new Balance { Date = DateTime.Parse("2014-01-01"), Fund = 11D },
                              new Balance { Date = DateTime.Parse("2014-01-02"), Fund = 111D },
                              new Balance { Date = DateTime.Parse("2014-01-03"), Fund = 111D },
                              new Balance { Date = DateTime.Parse("2014-01-04"), Fund = 1111D },
                              new Balance { Date = DateTime.Parse("2014-01-05"), Fund = 11111D },
                          };

            Assert.IsTrue(res.SequenceEqual(exp, new BalanceEqualityComparer()));
        }
    }
}
