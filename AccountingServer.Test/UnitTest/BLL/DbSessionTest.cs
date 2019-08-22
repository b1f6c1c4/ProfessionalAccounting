using System.Collections.Generic;
using AccountingServer.BLL;
using AccountingServer.Entities;
using Xunit;

namespace AccountingServer.Test.UnitTest.BLL
{
    public class DbSessionTest
    {
        [Fact]
        public void TheComparisonTest()
        {
            var rhs = new VoucherDetail
                {
                    User = "b1",
                    Currency = "USD",
                    Title = 1234,
                    SubTitle = 06,
                    Content = "abc",
                    Remark = "def",
                    Fund = 123.45
                };

            Assert.Equal(0, DbSession.TheComparison(rhs, rhs));
            Assert.Equal(
                -1,
                DbSession.TheComparison(
                    new VoucherDetail
                        {
                            User = "b0",
                            Currency = "USD",
                            Title = 1235,
                            SubTitle = 07,
                            Content = "abd",
                            Remark = "deg",
                            Fund = 123.46
                        },
                    rhs));
            Assert.Equal(
                +1,
                DbSession.TheComparison(
                    new VoucherDetail
                        {
                            User = "b2",
                            Currency = "USD",
                            Title = 1233,
                            SubTitle = 05,
                            Content = "abb",
                            Remark = "dee",
                            Fund = 123.44
                        },
                    rhs));

            Assert.Equal(
                -1,
                DbSession.TheComparison(
                    new VoucherDetail
                        {
                            User = "b1",
                            Currency = "USC",
                            Title = 1235,
                            SubTitle = 07,
                            Content = "abd",
                            Remark = "deg",
                            Fund = 123.46
                        },
                    rhs));
            Assert.Equal(
                +1,
                DbSession.TheComparison(
                    new VoucherDetail
                        {
                            User = "b1",
                            Currency = "USE",
                            Title = 1233,
                            SubTitle = 05,
                            Content = "abb",
                            Remark = "dee",
                            Fund = 123.44
                        },
                    rhs));

            Assert.Equal(
                -1,
                DbSession.TheComparison(
                    new VoucherDetail
                        {
                            User = "b1",
                            Currency = "USD",
                            Title = 1233,
                            SubTitle = 07,
                            Content = "abd",
                            Remark = "deg",
                            Fund = 123.46
                        },
                    rhs));
            Assert.Equal(
                +1,
                DbSession.TheComparison(
                    new VoucherDetail
                        {
                            User = "b1",
                            Currency = "USD",
                            Title = 1235,
                            SubTitle = 05,
                            Content = "abb",
                            Remark = "dee",
                            Fund = 123.44
                        },
                    rhs));

            Assert.Equal(
                -1,
                DbSession.TheComparison(
                    new VoucherDetail
                        {
                            User = "b1",
                            Currency = "USD",
                            Title = 1234,
                            SubTitle = 05,
                            Content = "abd",
                            Remark = "deg",
                            Fund = 123.46
                        },
                    rhs));
            Assert.Equal(
                +1,
                DbSession.TheComparison(
                    new VoucherDetail
                        {
                            User = "b1",
                            Currency = "USD",
                            Title = 1234,
                            SubTitle = 07,
                            Content = "abb",
                            Remark = "dee",
                            Fund = 123.44
                        },
                    rhs));

            Assert.Equal(
                -1,
                DbSession.TheComparison(
                    new VoucherDetail
                        {
                            User = "b1",
                            Currency = "USD",
                            Title = 1234,
                            SubTitle = 06,
                            Content = "abb",
                            Remark = "deg",
                            Fund = 123.46
                        },
                    rhs));
            Assert.Equal(
                +1,
                DbSession.TheComparison(
                    new VoucherDetail
                        {
                            User = "b1",
                            Currency = "USD",
                            Title = 1234,
                            SubTitle = 06,
                            Content = "abd",
                            Remark = "dee",
                            Fund = 123.44
                        },
                    rhs));

            Assert.Equal(
                -1,
                DbSession.TheComparison(
                    new VoucherDetail
                        {
                            User = "b1",
                            Currency = "USD",
                            Title = 1234,
                            SubTitle = 06,
                            Content = "abc",
                            Remark = "dee",
                            Fund = 123.46
                        },
                    rhs));
            Assert.Equal(
                +1,
                DbSession.TheComparison(
                    new VoucherDetail
                        {
                            User = "b1",
                            Currency = "USD",
                            Title = 1234,
                            SubTitle = 06,
                            Content = "abc",
                            Remark = "deg",
                            Fund = 123.44
                        },
                    rhs));

            Assert.Equal(
                -1,
                DbSession.TheComparison(
                    new VoucherDetail
                        {
                            User = "b1",
                            Currency = "USD",
                            Title = 1234,
                            SubTitle = 06,
                            Content = "abc",
                            Remark = "def",
                            Fund = 123.44
                        },
                    rhs));
            Assert.Equal(
                +1,
                DbSession.TheComparison(
                    new VoucherDetail
                        {
                            User = "b1",
                            Currency = "USD",
                            Title = 1234,
                            SubTitle = 06,
                            Content = "abc",
                            Remark = "def",
                            Fund = 123.46
                        },
                    rhs));
        }

        [Fact]
        public void VoucherRegularizeTest()
        {
            var voucher = new Voucher
                {
                    Details = new List<VoucherDetail>
                        {
                            new VoucherDetail { User = "b1", Currency = "jPy" },
                            new VoucherDetail { User = "b2", Currency = "cnY" },
                            new VoucherDetail { User = "b1", Currency = "Cny" }
                        }
                };

            DbSession.Regularize(voucher);

            Assert.Equal(3, voucher.Details.Count);
            Assert.Equal("b1", voucher.Details[0].User);
            Assert.Equal("CNY", voucher.Details[0].Currency);
            Assert.Equal("b1", voucher.Details[1].User);
            Assert.Equal("JPY", voucher.Details[1].Currency);
            Assert.Equal("b2", voucher.Details[2].User);
            Assert.Equal("CNY", voucher.Details[2].Currency);

            voucher.Details = null;
            DbSession.Regularize(voucher);
            // ReSharper disable once PossibleNullReferenceException
            Assert.Empty(voucher.Details);
        }
    }
}
