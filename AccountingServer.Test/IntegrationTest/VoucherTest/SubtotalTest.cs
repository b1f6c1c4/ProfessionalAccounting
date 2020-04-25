using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AccountingServer.DAL;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using Xunit;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Test.IntegrationTest.VoucherTest
{
    [Collection("DbTestCollection")]
    [SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
    public class SubtotalTest : IDisposable
    {
        private readonly IDbAdapter m_Adapter;

        public SubtotalTest()
        {
            m_Adapter = Facade.Create("mongodb://localhost/accounting-test");

            m_Adapter.DeleteVouchers(VoucherQueryUnconstrained.Instance);

            m_Adapter.Upsert(
                new Voucher
                    {
                        Date = new DateTime(2016, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                        Remark = "xrmk1",
                        Details = new List<VoucherDetail>
                            {
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "JPY",
                                        Title = 1234,
                                        SubTitle = 56,
                                        Content = "cnt1",
                                        Fund = 123.45,
                                        Remark = "rmk1"
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "JPY",
                                        Title = 6541,
                                        SubTitle = 98,
                                        Content = "cnt1",
                                        Fund = -123.45,
                                        Remark = "rmk2"
                                    }
                            }
                    });

            m_Adapter.Upsert(
                new Voucher
                    {
                        Date = new DateTime(2017, 02, 01, 0, 0, 0, DateTimeKind.Utc),
                        Remark = "xrmk2",
                        Details = new List<VoucherDetail>
                            {
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "JPY",
                                        Title = 1234,
                                        SubTitle = 56,
                                        Content = "cnt1",
                                        Fund = 78.53,
                                        Remark = "rmk1"
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "JPY",
                                        Title = 6541,
                                        SubTitle = 98,
                                        Content = "cnt1",
                                        Fund = -78.53,
                                        Remark = "rmk2"
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1234,
                                        SubTitle = 56,
                                        Content = "cnt2",
                                        Fund = 66.66,
                                        Remark = "rmk1"
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 6541,
                                        SubTitle = 98,
                                        Content = "cnt2",
                                        Fund = -66.66,
                                        Remark = "rmk2"
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1&b2", Currency = "EUR", Title = 2333, Fund = 114514
                                    }
                            }
                    });

            ClientUser.Set("b1");
        }

        public void Dispose() => m_Adapter.DeleteVouchers(VoucherQueryUnconstrained.Instance);

        [Theory]
        [InlineData(6, "")]
        [InlineData(0, "-{} :")]
        [InlineData(0, ": ()-")]
        [InlineData(1, ": -")]
        [InlineData(2, "%xrmk1%")]
        [InlineData(1, "\"rmk2\" %xrmk1%")]
        [InlineData(2, "\"rmk2\" %xrmk1% :")]
        [InlineData(3, "\"rmk2\"")]
        [InlineData(6, "\"rmk2\" :")]
        [InlineData(3, "\"rmk2\" : \"rmk2\"")]
        [InlineData(4, "\"rmk2\" : @JPY")]
        [InlineData(3, "%xrmk2% : 'cnt1'+\"rmk1\"")]
        public void RunTestQuery(int number, string query)
        {
            Assert.Equal(number, m_Adapter.SelectVoucherDetails(ParsingF.DetailQuery(query)).Count());
            Assert.Equal(
                number,
                m_Adapter.SelectVoucherDetailsGrouped(ParsingF.GroupedQuery(query + "!v")).SingleOrDefault()?.Fund ??
                0);
        }

        [Theory]
        [InlineData(null, "T9999 `v")]
        [InlineData(null, "`v")]
        [InlineData(0, "``v")]
        [InlineData(123.45 + 78.53 + 66.66, "\"rmk1\"`v")]
        [InlineData(66.66, "\"rmk1\" : T123456-'cnt1' `v")]
        [InlineData(78.53 + 66.66, "< 20170201 : > `v")]
        public void RunTestValue(double? value, string query)
            => Assert.Equal(
                value,
                m_Adapter.SelectVoucherDetailsGrouped(ParsingF.GroupedQuery(query)).SingleOrDefault()?.Fund);

        [Theory]
        [InlineData("b2", null, "``U")]
        [InlineData("b1", 0, "``U")]
        [InlineData("b1&b2", null, "``U")]
        [InlineData("b2", null, "U``U")]
        [InlineData("b1", 0, "U``U")]
        [InlineData("b1&b2", 114514, "U``U")]
        [InlineData("b2", null, "Ub1&b2``U")]
        [InlineData("b1", null, "Ub1&b2``U")]
        [InlineData("b1&b2", 114514, "Ub1&b2``U")]
        public void RunTestUser(string user, double? value, string query)
            => Assert.Equal(
                value,
                m_Adapter.SelectVoucherDetailsGrouped(ParsingF.GroupedQuery(query))
                    .SingleOrDefault(b => b.User == user)?.Fund);

        [Theory]
        [InlineData("CNY", null, "``C")]
        [InlineData("JPY", 0, "``C")]
        [InlineData("JPY", -123.45 - 78.53, "\"rmk2\"``C")]
        [InlineData("JPY", 123.45, "%xrmk1% : \"rmk1\"``C")]
        [InlineData("USD", -66.66, "\"rmk2\"``C")]
        [InlineData("USD", null, "%xrmk1% : \"rmk1\"``C")]
        public void RunTestCurrency(string currency, double? value, string query)
            => Assert.Equal(
                value,
                m_Adapter.SelectVoucherDetailsGrouped(ParsingF.GroupedQuery(query))
                    .SingleOrDefault(b => b.Currency == currency)?.Fund);

        [Theory]
        [InlineData("qwer", null, "``c")]
        [InlineData("cnt1", 0, "``c")]
        [InlineData("cnt1", -123.45 - 78.53, "\"rmk2\"``c")]
        [InlineData("cnt1", 123.45, "%xrmk1% : \"rmk1\"``c")]
        [InlineData("cnt2", -66.66, "\"rmk2\"``c")]
        [InlineData("cnt2", null, "%xrmk1% : \"rmk1\"``c")]
        public void RunTestContent(string content, double? value, string query)
            => Assert.Equal(
                value,
                m_Adapter.SelectVoucherDetailsGrouped(ParsingF.GroupedQuery(query))
                    .SingleOrDefault(b => b.Content == content)?.Fund);

        [Theory]
        [InlineData("qwer", null, "``r")]
        [InlineData("rmk1", 123.45 + 78.53 + 66.66, "``r")]
        [InlineData("rmk1", 66.66, "'cnt2'``r")]
        [InlineData("rmk1", 123.45, "%xrmk1% : 'cnt1'``r")]
        [InlineData("rmk2", -123.45 - 78.53, "'cnt1'``r")]
        [InlineData("rmk2", null, "%xrmk1% : 'cnt2'``r")]
        public void RunTestRemark(string remark, double? value, string query)
            => Assert.Equal(
                value,
                m_Adapter.SelectVoucherDetailsGrouped(ParsingF.GroupedQuery(query))
                    .SingleOrDefault(b => b.Remark == remark)?.Fund);

        [Theory]
        [InlineData(null, null, "``t")]
        [InlineData(1234, 123.45 + 78.53 + 66.66, "``t")]
        [InlineData(1234, 66.66, "'cnt2'``t")]
        [InlineData(1234, 123.45, "%xrmk1% : 'cnt1'``t")]
        [InlineData(6541, -123.45 - 78.53, "'cnt1'``t")]
        [InlineData(6541, null, "%xrmk1% : 'cnt2'``t")]
        public void RunTestTitle(int? title, double? value, string query)
            => Assert.Equal(
                value,
                m_Adapter.SelectVoucherDetailsGrouped(ParsingF.GroupedQuery(query))
                    .SingleOrDefault(b => b.Title == title)?.Fund);

        [Theory]
        [InlineData(6666, null, null, "``ts")]
        [InlineData(1234, 56, 123.45 + 78.53 + 66.66, "``ts")]
        [InlineData(1234, 56, 66.66, "'cnt2'``ts")]
        [InlineData(1234, 56, 123.45, "%xrmk1% : 'cnt1'``ts")]
        [InlineData(6541, 98, -123.45 - 78.53, "'cnt1'``ts")]
        [InlineData(6541, 98, null, "%xrmk1% : 'cnt2'``ts")]
        public void RunTestTitleSubTitle(int? title, int? subTitle, double? value, string query)
            => Assert.Equal(
                value,
                m_Adapter.SelectVoucherDetailsGrouped(ParsingF.GroupedQuery(query))
                    .SingleOrDefault(b => b.Title == title && b.SubTitle == subTitle)?.Fund);

        [Theory]
        [InlineData(null, null, "``d")]
        [InlineData("2016-01-01", null, "``d")]
        [InlineData("2016-12-31", 123.45, ":T1234 ``d")]
        [InlineData("2017-02-01", 78.53 + 66.66, ":T1234 ``d")]
        [InlineData(null, null, "``vD[2017~2018]")]
        [InlineData("2016-01-01", null, "``vD[]")]
        [InlineData("2016-12-31", 123.45, ":T1234 ``vD")]
        [InlineData("2017-02-01", 78.53 + 66.66, ":T1234 ``vD[]")]
        public void RunTestDay(string dt, double? value, string query)
            => Assert.Equal(
                value,
                m_Adapter.SelectVoucherDetailsGrouped(ParsingF.GroupedQuery(query))
                    .SingleOrDefault(b => b.Date == dt.ToDateTime())?.Fund);

        [Theory]
        [InlineData(null, null, "``w")]
        [InlineData("2016-01-01", null, "``w")]
        [InlineData("2016-12-31", null, ":T1234 ``w")]
        [InlineData("2016-12-26", 123.45, ":T1234 ``w")]
        [InlineData("2017-02-01", null, ":T1234 ``w")]
        [InlineData("2017-01-30", 78.53 + 66.66, ":T1234 ``w")]
        public void RunTestWeek(string dt, double? value, string query)
            => Assert.Equal(
                value,
                m_Adapter.SelectVoucherDetailsGrouped(ParsingF.GroupedQuery(query))
                    .SingleOrDefault(b => b.Date == dt.ToDateTime())?.Fund);

        [Theory]
        [InlineData(null, null, "``m")]
        [InlineData("2016-01-01", null, "``m")]
        [InlineData("2016-12-31", null, ":T1234 ``m")]
        [InlineData("2016-12-01", 123.45, ":T1234 ``m")]
        [InlineData("2017-02-01", 78.53 + 66.66, ":T1234 ``m")]
        public void RunTestMonth(string dt, double? value, string query)
            => Assert.Equal(
                value,
                m_Adapter.SelectVoucherDetailsGrouped(ParsingF.GroupedQuery(query))
                    .SingleOrDefault(b => b.Date == dt.ToDateTime())?.Fund);

        [Theory]
        [InlineData(null, null, "``y")]
        [InlineData("2016-12-31", null, ":T1234 ``y")]
        [InlineData("2016-01-01", 123.45, ":T1234 ``y")]
        [InlineData("2017-01-01", 78.53 + 66.66, ":T1234 ``y")]
        public void RunTestYear(string dt, double? value, string query)
            => Assert.Equal(
                value,
                m_Adapter.SelectVoucherDetailsGrouped(ParsingF.GroupedQuery(query))
                    .SingleOrDefault(b => b.Date == dt.ToDateTime())?.Fund);
    }
}
