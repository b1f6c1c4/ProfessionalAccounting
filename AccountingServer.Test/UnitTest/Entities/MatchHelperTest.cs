using System;
using System.Diagnostics.CodeAnalysis;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using Xunit;

namespace AccountingServer.Test.UnitTest.Entities
{
    [SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
    public class MatchHelperTest
    {
        private static void StringMatchTest(Func<string, string, bool> pred)
        {
            Assert.True(pred(null, null));
            Assert.True(pred("", null));
            Assert.True(pred("abc", null));

            Assert.True(pred(null, ""));
            Assert.True(pred("", ""));
            Assert.False(pred("abc", ""));

            Assert.False(pred(null, "abc"));
            Assert.False(pred("", "abc"));
            Assert.True(pred("abc", "abc"));
        }

        [Theory]
        [InlineData(true, null, null)]
        [InlineData(true, "", null)]
        [InlineData(true, "test", null)]
        [InlineData(false, null, "")]
        [InlineData(true, "", "")]
        [InlineData(false, "test", "")]
        [InlineData(false, null, "test")]
        [InlineData(false, "", "test")]
        [InlineData(true, "test", "test")]
        public void VoucherMatchTestID(bool expected, string value, string filter)
        {
            Assert.Equal(expected, MatchHelper.IsMatch(new Voucher { ID = value }, new Voucher { ID = filter }));
        }

        [Theory]
        [InlineData(true, null, null)]
        [InlineData(true, "2017-01-01", null)]
        [InlineData(false, null, "2017-01-01")]
        [InlineData(true, "2017-01-01", "2017-01-01")]
        [InlineData(false, "2018-01-01", "2017-01-01")]
        public void VoucherMatchTestDate(bool expected, string valueS, string filterS)
        {
            var value = valueS.ToDateTime();
            var filter = filterS.ToDateTime();
            Assert.Equal(expected, MatchHelper.IsMatch(new Voucher { Date = value }, new Voucher { Date = filter }));
        }

        [Theory]
        [InlineData(true, VoucherType.Ordinary, null)]
        [InlineData(true, VoucherType.Uncertain, null)]
        [InlineData(true, VoucherType.Carry, null)]
        [InlineData(true, VoucherType.AnnualCarry, null)]
        [InlineData(true, VoucherType.Ordinary, VoucherType.Ordinary)]
        [InlineData(false, VoucherType.Uncertain, VoucherType.Ordinary)]
        [InlineData(false, VoucherType.Carry, VoucherType.Ordinary)]
        [InlineData(false, VoucherType.AnnualCarry, VoucherType.Ordinary)]
        [InlineData(true, VoucherType.Ordinary, VoucherType.General)]
        [InlineData(true, VoucherType.Uncertain, VoucherType.General)]
        [InlineData(true, VoucherType.Amortization, VoucherType.General)]
        [InlineData(true, VoucherType.Depreciation, VoucherType.General)]
        [InlineData(true, VoucherType.Devalue, VoucherType.General)]
        [InlineData(false, VoucherType.Carry, VoucherType.General)]
        [InlineData(false, VoucherType.AnnualCarry, VoucherType.General)]
        public void VoucherMatchTestType(bool expected, VoucherType? value, VoucherType? filter)
        {
            Assert.Equal(expected, MatchHelper.IsMatch(new Voucher { Type = value }, new Voucher { Type = filter }));
        }

        [Theory]
        [InlineData(true, "b1", null)]
        [InlineData(true, "b2", null)]
        [InlineData(false, "b1", "")]
        [InlineData(false, "b2", "")]
        [InlineData(false, "b1", "b2")]
        [InlineData(true, "b2", "b2")]
        public void DetailMatchTestUser(bool expected, string value, string filter)
        {
            Assert.Equal(
                expected,
                MatchHelper.IsMatch(new VoucherDetail { User = value }, new VoucherDetail { User = filter }));
        }

        [Theory]
        [InlineData(true, null, null)]
        [InlineData(true, "", null)]
        [InlineData(true, "USD", null)]
        [InlineData(false, null, "")]
        [InlineData(true, "", "")]
        [InlineData(false, "USD", "")]
        [InlineData(false, null, "USD")]
        [InlineData(false, "", "USD")]
        [InlineData(true, "USD", "USD")]
        public void DetailMatchTestCurrency(bool expected, string value, string filter)
        {
            Assert.Equal(
                expected,
                MatchHelper.IsMatch(new VoucherDetail { Currency = value }, new VoucherDetail { Currency = filter }));
        }

        [Theory]
        [InlineData(true, 1001, null)]
        [InlineData(false, 1000, 1001)]
        [InlineData(true, 1001, 1001)]
        public void DetailMatchTestTitle(bool expected, int? value, int? filter)
        {
            Assert.Equal(
                expected,
                MatchHelper.IsMatch(new VoucherDetail { Title = value }, new VoucherDetail { Title = filter }));
        }

        [Theory]
        [InlineData(true, null, null)]
        [InlineData(true, 99, null)]
        [InlineData(true, null, 00)]
        [InlineData(false, 01, 00)]
        [InlineData(false, 99, 00)]
        [InlineData(false, null, 99)]
        [InlineData(false, 01, 99)]
        [InlineData(true, 99, 99)]
        public void DetailMatchTestSubTitle(bool expected, int? value, int? filter)
        {
            Assert.Equal(
                expected,
                MatchHelper.IsMatch(new VoucherDetail { SubTitle = value }, new VoucherDetail { SubTitle = filter }));
        }

        [Theory]
        [InlineData(true, null, null)]
        [InlineData(true, 123.45, null)]
        [InlineData(false, null, 123.45)]
        [InlineData(false, 123.45 + 2 * VoucherDetail.Tolerance, 123.45)]
        [InlineData(true, 123.45 + 0.5 * VoucherDetail.Tolerance, 123.45)]
        public void DetailMatchTestFund(bool expected, double? value, double? filter)
        {
            Assert.Equal(
                expected,
                MatchHelper.IsMatch(new VoucherDetail { Fund = value }, new VoucherDetail { Fund = filter }));
        }

        [Theory]
        [InlineData(true, null, 0)]
        [InlineData(true, 123.45, 0)]
        [InlineData(false, null, +1)]
        [InlineData(false, -2 * VoucherDetail.Tolerance, +1)]
        [InlineData(true, -0.5 * VoucherDetail.Tolerance, +1)]
        [InlineData(false, null, -1)]
        [InlineData(false, +2 * VoucherDetail.Tolerance, -1)]
        [InlineData(true, +0.5 * VoucherDetail.Tolerance, -1)]
        public void DetailMatchTestDir(bool expected, double? value, int dir)
        {
            Assert.Equal(expected, MatchHelper.IsMatch(new VoucherDetail { Fund = value }, new VoucherDetail(), dir));
        }

        [Fact]
        public void DetailMatchTestContent()
        {
            StringMatchTest(
                (v, f) => MatchHelper.IsMatch(new VoucherDetail { Content = v }, new VoucherDetail { Content = f }));
        }

        [Fact]
        public void DetailMatchTestNull()
        {
            Assert.True(
                MatchHelper.IsMatch(
                    new VoucherDetail { Content = "cnt", Fund = 10, Remark = "rmk" },
                    (VoucherDetail)null));
        }

        [Fact]
        public void DetailMatchTestRemark()
        {
            StringMatchTest(
                (v, f) => MatchHelper.IsMatch(new VoucherDetail { Remark = v }, new VoucherDetail { Remark = f }));
        }

        [Fact]
        public void VoucherMatchTest()
        {
            var filter = new Voucher
                {
                    ID = "abc",
                    Date = new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Type = VoucherType.Ordinary,
                    Remark = "abc"
                };

            Assert.False(
                MatchHelper.IsMatch(
                    new Voucher
                        {
                            ID = "abc0",
                            Date = new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            Type = VoucherType.Ordinary,
                            Remark = "abc"
                        },
                    filter));
            Assert.False(
                MatchHelper.IsMatch(
                    new Voucher
                        {
                            ID = "abc",
                            Date = new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            Type = VoucherType.Ordinary,
                            Remark = "abc"
                        },
                    filter));
            Assert.False(
                MatchHelper.IsMatch(
                    new Voucher
                        {
                            ID = "abc",
                            Date = new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            Type = VoucherType.Carry,
                            Remark = "abc"
                        },
                    filter));
            Assert.False(
                MatchHelper.IsMatch(
                    new Voucher
                        {
                            ID = "abc",
                            Date = new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            Type = VoucherType.Ordinary,
                            Remark = "abc0"
                        },
                    filter));
            Assert.True(
                MatchHelper.IsMatch(
                    new Voucher
                        {
                            ID = "abc",
                            Date = new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            Type = VoucherType.Ordinary,
                            Remark = "abc"
                        },
                    filter));
        }

        [Fact]
        public void VoucherMatchTestNull()
        {
            Assert.True(MatchHelper.IsMatch(new Voucher { ID = "id", Remark = "rmk" }, (Voucher)null));
        }

        [Fact]
        public void VoucherMatchTestRemark()
        {
            StringMatchTest((v, f) => MatchHelper.IsMatch(new Voucher { Remark = v }, new Voucher { Remark = f }));
        }
    }
}
