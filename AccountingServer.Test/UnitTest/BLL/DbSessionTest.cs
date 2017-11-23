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
                            new VoucherDetail { Currency = "jPy" },
                            new VoucherDetail { Currency = null }
                        }
                };

            DbSession.Regularize(voucher);

            Assert.Equal(2, voucher.Details.Count);
            Assert.Equal(VoucherDetail.BaseCurrency, voucher.Details[0].Currency);
            Assert.Equal("JPY", voucher.Details[1].Currency);

            voucher.Details = null;
            DbSession.Regularize(voucher);
            // ReSharper disable once PossibleNullReferenceException
            Assert.Empty(voucher.Details);
        }
    }
}
