using System;
using System.Reflection;
using AccountingServer.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AccountingServer.UnitTest
{
    [TestClass]
    public class ConsoleTest
    {
        [TestMethod]
        public void ParseQuery()
        {
            object[] pars;
            VoucherDetail vd;

            var method =
                Assembly.GetAssembly(typeof(AccountingServer.Console.AccountingConsole))
                        .GetType("AccountingServer.Console.AccountingConsole")
                        .GetMethod("ParseQuery", BindingFlags.Static | BindingFlags.NonPublic);

            pars = new object[] { "T123401", null };
            vd = (VoucherDetail)method.Invoke(null, pars);
            Assert.AreEqual(1234, vd.Title);
            Assert.AreEqual(01, vd.SubTitle);
            Assert.AreEqual(null, vd.Content);
            Assert.AreEqual(String.Empty, (string)pars[1]);

            pars = new object[] { "T432101 '' []", null };
            vd = (VoucherDetail)method.Invoke(null, pars);
            Assert.AreEqual(4321, vd.Title);
            Assert.AreEqual(01, vd.SubTitle);
            Assert.AreEqual(String.Empty, vd.Content);
            Assert.AreEqual("[]", (string)pars[1]);

            pars = new object[] { "T4567 '' []", null };
            vd = (VoucherDetail)method.Invoke(null, pars);
            Assert.AreEqual(4567, vd.Title);
            Assert.AreEqual(null, vd.SubTitle);
            Assert.AreEqual(String.Empty, vd.Content);
            Assert.AreEqual("[]", (string)pars[1]);

            pars = new object[] { "'ccc' []", null };
            vd = (VoucherDetail)method.Invoke(null, pars);
            Assert.AreEqual(null, vd.Title);
            Assert.AreEqual(null, vd.SubTitle);
            Assert.AreEqual("ccc", vd.Content);
            Assert.AreEqual("[]", (string)pars[1]);

            pars = new object[] { "'ccc' ", null };
            vd = (VoucherDetail)method.Invoke(null, pars);
            Assert.AreEqual(null, vd.Title);
            Assert.AreEqual(null, vd.SubTitle);
            Assert.AreEqual("ccc", vd.Content);
            Assert.AreEqual(String.Empty, (string)pars[1]);

            pars = new object[] { "'asdf'", null };
            vd = (VoucherDetail)method.Invoke(null, pars);
            Assert.AreEqual(null, vd.Title);
            Assert.AreEqual(null, vd.SubTitle);
            Assert.AreEqual("asdf", vd.Content);
            Assert.AreEqual(String.Empty, (string)pars[1]);
        }

        [TestMethod]
        public void ParseDateQuery()
        {
            object[] pars;
            DateFilter rng;

            var method =
                Assembly.GetAssembly(typeof(THUInfo))
                        .GetType("AccountingServer.Console.AccountingConsole")
                        .GetMethod("ParseDateQuery", BindingFlags.Static | BindingFlags.NonPublic);

            pars = new object[] { "201412", false };
            rng = (DateFilter)method.Invoke(null, pars);
            Assert.AreEqual(DateTime.Parse("2014-11-20"), rng.StartDate);
            Assert.AreEqual(DateTime.Parse("2014-12-19"), rng.EndDate);
            Assert.AreEqual(false, rng.Nullable);

            pars = new object[] { "[201412 ]", false };
            rng = (DateFilter)method.Invoke(null, pars);
            Assert.AreEqual(DateTime.Parse("2014-11-20"), rng.StartDate);
            Assert.AreEqual(null, rng.EndDate);
            Assert.AreEqual(false, rng.Nullable);

            pars = new object[] { "[ 201501]", false };
            rng = (DateFilter)method.Invoke(null, pars);
            Assert.AreEqual(null, rng.StartDate);
            Assert.AreEqual(DateTime.Parse("2015-01-19"), rng.EndDate);
            Assert.AreEqual(true, rng.Nullable);

            pars = new object[] { "[ 201602]", true };
            rng = (DateFilter)method.Invoke(null, pars);
            Assert.AreEqual(null, rng.StartDate);
            Assert.AreEqual(DateTime.Parse("2016-02-29"), rng.EndDate);
            Assert.AreEqual(true, rng.Nullable);

            pars = new object[] { "[190002 201402]", true };
            rng = (DateFilter)method.Invoke(null, pars);
            Assert.AreEqual(DateTime.Parse("1900-02-01"), rng.StartDate);
            Assert.AreEqual(DateTime.Parse("2014-02-28"), rng.EndDate);
            Assert.AreEqual(false, rng.Nullable);

            pars = new object[] { "[201405]", false };
            rng = (DateFilter)method.Invoke(null, pars);
            Assert.AreEqual(DateTime.Parse("2014-04-20"), rng.StartDate);
            Assert.AreEqual(DateTime.Parse("2014-05-19"), rng.EndDate);
            Assert.AreEqual(false, rng.Nullable);

            pars = new object[] { "[@201405]", false };
            rng = (DateFilter)method.Invoke(null, pars);
            Assert.AreEqual(DateTime.Parse("2014-05-01"), rng.StartDate);
            Assert.AreEqual(DateTime.Parse("2014-05-31"), rng.EndDate);
            Assert.AreEqual(false, rng.Nullable);

            pars = new object[] { "[20140517]", false };
            rng = (DateFilter)method.Invoke(null, pars);
            Assert.AreEqual(DateTime.Parse("2014-05-17"), rng.StartDate);
            Assert.AreEqual(DateTime.Parse("2014-05-17"), rng.EndDate);
            Assert.AreEqual(false, rng.Nullable);

            pars = new object[] { "[null]", false };
            rng = (DateFilter)method.Invoke(null, pars);
            Assert.IsTrue(rng.NullOnly);
        }
    }
}
