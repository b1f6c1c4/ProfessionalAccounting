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
                Assembly.GetAssembly(typeof(THUInfo))
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
    }
}
