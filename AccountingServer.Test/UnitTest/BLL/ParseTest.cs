using System;
using System.Linq;
using System.Reflection;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using Xunit;
using static AccountingServer.BLL.Parsing.Facade;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Test.UnitTest.BLL
{
    public class ParseTest
    {
        private void PairedSucc<TR>(string name, string s, Action<TR> pred = null)
        {
            if (pred == null)
                pred = r => Assert.NotNull(r);

            {
                var m = typeof(FacadeBase).GetMethod(name, new[] { typeof(string) });
                Assert.NotNull(m);
                var par = new object[] { s };
                var r = m.Invoke(ParsingF, par);
                if (r != null || Nullable.GetUnderlyingType(typeof(TR)) == null)
                    Assert.IsAssignableFrom<TR>(r);
                pred((TR)r);
                r = m.Invoke(Parsing, par);
                if (r != null || Nullable.GetUnderlyingType(typeof(TR)) == null)
                    Assert.IsAssignableFrom<TR>(r);
                pred((TR)r);
            }
            {
                var m = typeof(FacadeBase).GetMethod(name, new[] { typeof(string).MakeByRefType() });
                Assert.NotNull(m);
                var par = new object[] { s };
                var r = m.Invoke(ParsingF, par);
                if (r != null || Nullable.GetUnderlyingType(typeof(TR)) == null)
                    Assert.IsAssignableFrom<TR>(r);
                pred((TR)r);
                Assert.Equal("", par[0]);
                par[0] = s;
                r = m.Invoke(Parsing, par);
                if (r != null || Nullable.GetUnderlyingType(typeof(TR)) == null)
                    Assert.IsAssignableFrom<TR>(r);
                pred((TR)r);
                Assert.Equal("", par[0]);
            }
        }

        private void PairedFail(string name, string s, object def = null)
        {
            {
                var m = typeof(FacadeBase).GetMethod(name, new[] { typeof(string) });
                Assert.NotNull(m);
                var par = new object[] { s };
                try
                {
                    m.Invoke(ParsingF, par);
                    Assert.True(false);
                }
                catch (TargetInvocationException e)
                {
                    Assert.IsAssignableFrom<Exception>(e.InnerException);
                }

                Assert.Equal(def, m.Invoke(Parsing, par));
            }
            {
                var m = typeof(FacadeBase).GetMethod(name, new[] { typeof(string).MakeByRefType() });
                Assert.NotNull(m);
                var par = new object[] { s };
                try
                {
                    m.Invoke(ParsingF, par);
                    Assert.True(false);
                }
                catch (TargetInvocationException e)
                {
                    Assert.IsAssignableFrom<Exception>(e.InnerException);
                }

                Assert.Equal(s, par[0]);

                Assert.Equal(def, m.Invoke(Parsing, par));
                Assert.Equal(s, par[0]);
            }
        }

        [Theory]
        [InlineData("T1001", 1001, null)]
        [InlineData("T4252", 4252, null)]
        [InlineData("T100000", 1000, 00)]
        [InlineData("T123405", 1234, 05)]
        [InlineData("T234500", 2345, 00)]
        [InlineData("T999999", 9999, 99)]
        public void ParseTitleTest(string s, int t, int? st)
            => PairedSucc<ITitle>(nameof(ParsingF.Title), s, r =>
                {
                    Assert.Equal(t, r.Title);
                    Assert.Equal(st, r.SubTitle);
                });

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("T0124")]
        [InlineData("T004355")]
        [InlineData("T15b0")]
        [InlineData("t1001")]
        [InlineData("test")]
        public void ParseTitleFailTest(string s)
            => PairedFail(nameof(ParsingF.Title), s);

        [Theory]
        [InlineData("null", null)]
        [InlineData("[null]", null)]
        [InlineData("20200101", "2020-01-01")]
        [InlineData("[20200101]", "2020-01-01")]
        public void UniqueTimeTest(string s, string t)
            => PairedSucc<DateTime?>(nameof(ParsingF.UniqueTime), s,
                r => { Assert.Equal(t, r?.ToString("yyyy-MM-dd")); });

        [Theory]
        [InlineData(".", 0)]
        [InlineData("..", 1)]
        [InlineData("...", 2)]
        [InlineData("[.]", 0)]
        [InlineData("[..]", 1)]
        [InlineData("[...]", 2)]
        public void UniqueTimeRelTest(string s, int d)
            => PairedSucc<DateTime?>(nameof(ParsingF.UniqueTime), s,
                r => { Assert.Equal(ClientDateTime.Today.AddDays(-d), r); });

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("~null")]
        [InlineData(",,")]
        [InlineData("0")]
        [InlineData("xxx")]
        [InlineData("2020")]
        [InlineData("202004")]
        [InlineData("20000229")]
        public void UniqueTimeFailTest(string s)
            => PairedFail(nameof(ParsingF.UniqueTime), s);

        [Theory]
        [InlineData("null", null, null, true, true)]
        [InlineData("~null", null, null, false, false)]
        [InlineData("2016", "2016-01-01", "2016-12-31", false, false)]
        [InlineData("2019~202004", "2019-01-01", "2020-04-30", false, false)]
        [InlineData("2015~", "2015-01-01", null, false, false)]
        [InlineData("20190305~~", "2019-03-05", null, true, false)]
        [InlineData("~2017", null, "2017-12-31", true, false)]
        [InlineData("~~201504", null, "2015-04-30", false, false)]
        [InlineData("2013~~2014", "2013-01-01", "2014-12-31", true, false)]
        [InlineData("[]", null, null, true, false)]
        [InlineData("[null]", null, null, true, true)]
        [InlineData("[~null]", null, null, false, false)]
        [InlineData("[2016]", "2016-01-01", "2016-12-31", false, false)]
        [InlineData("[2019~202004]", "2019-01-01", "2020-04-30", false, false)]
        [InlineData("[2015~]", "2015-01-01", null, false, false)]
        [InlineData("[20190305~~]", "2019-03-05", null, true, false)]
        [InlineData("[~2017]", null, "2017-12-31", true, false)]
        [InlineData("[~~201504]", null, "2015-04-30", false, false)]
        [InlineData("[2013~~2014]", "2013-01-01", "2014-12-31", true, false)]
        public void RangeTest(string s, string b, string e, bool n, bool no)
            => PairedSucc<DateFilter>(nameof(ParsingF.Range), s,
                r =>
                    {
                        Assert.Equal(b, r.StartDate?.ToString("yyyy-MM-dd"));
                        Assert.Equal(e, r.EndDate?.ToString("yyyy-MM-dd"));
                        Assert.Equal(n, r.Nullable);
                        Assert.Equal(no, r.NullOnly);
                    });

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("[")]
        [InlineData("xxx")]
        [InlineData("20000229")]
        public void RangeFailTest(string s)
            => PairedFail(nameof(ParsingF.Range), s);

        [Theory]
        [InlineData("{}+{}:>`v", GatheringType.Sum, SubtotalLevel.NonZero)]
        [InlineData("``cU", GatheringType.Sum, SubtotalLevel.None, SubtotalLevel.Content, SubtotalLevel.User)]
        [InlineData("!", GatheringType.Count, SubtotalLevel.None, SubtotalLevel.Currency, SubtotalLevel.Title,
            SubtotalLevel.SubTitle, SubtotalLevel.User, SubtotalLevel.Content)]
        public void GroupedQueryTest(string s, GatheringType gt, params SubtotalLevel[] levels)
            => PairedSucc<IGroupedQuery>(nameof(ParsingF.GroupedQuery), s,
                r =>
                    {
                        Assert.NotNull(r.VoucherEmitQuery.VoucherQuery);
                        Assert.Equal(gt, r.Subtotal.GatherType);
                        Assert.True(levels.SequenceEqual(r.Subtotal.Levels));
                    });

        [Theory]
        [InlineData(null)]
        [InlineData("!!")]
        [InlineData("!!v")]
        [InlineData("[[]")]
        public void GroupedQueryFailTest(string s)
            => PairedFail(nameof(ParsingF.GroupedQuery), s);

        [Theory]
        [InlineData("!!v", SubtotalLevel.None)]
        [InlineData("!!y", SubtotalLevel.None, SubtotalLevel.Year)]
        [InlineData("!!", SubtotalLevel.None, SubtotalLevel.Currency, SubtotalLevel.Title, SubtotalLevel.SubTitle,
            SubtotalLevel.User,
            SubtotalLevel.Content)]
        public void VoucherGroupedQueryTest(string s, params SubtotalLevel[] levels)
            => PairedSucc<IVoucherGroupedQuery>(nameof(ParsingF.VoucherGroupedQuery), s,
                r =>
                    {
                        Assert.NotNull(r.VoucherQuery);
                        Assert.Equal(GatheringType.VoucherCount, r.Subtotal.GatherType);
                        Assert.True(levels.SequenceEqual(r.Subtotal.Levels));
                    });

        [Theory]
        [InlineData(null)]
        [InlineData("`")]
        [InlineData("`v")]
        [InlineData("``")]
        [InlineData("``v")]
        [InlineData("!")]
        [InlineData("!v")]
        [InlineData("[[]")]
        public void VoucherGroupedQueryFailTest(string s)
            => PairedFail(nameof(ParsingF.VoucherGroupedQuery), s);

        [Theory]
        [InlineData("")]
        [InlineData("{}+{}")]
        public void VoucherQueryTest(string s)
            => PairedSucc<IQueryCompounded<IVoucherQueryAtom>>(nameof(ParsingF.VoucherQuery), s);

        [Theory]
        [InlineData(null)]
        [InlineData("[[]")]
        public void VoucherQueryFailTest(string s)
            => PairedFail(nameof(ParsingF.VoucherQuery), s, VoucherQueryUnconstrained.Instance);

        [Theory]
        [InlineData("`ts", GatheringType.Sum, SubtotalLevel.NonZero, SubtotalLevel.Title | SubtotalLevel.NonZero,
            SubtotalLevel.SubTitle | SubtotalLevel.NonZero)]
        [InlineData("``cUr", GatheringType.Sum, SubtotalLevel.None, SubtotalLevel.Content, SubtotalLevel.User,
            SubtotalLevel.Remark)]
        [InlineData("``Czryz", GatheringType.Sum, SubtotalLevel.None, SubtotalLevel.Currency | SubtotalLevel.NonZero,
            SubtotalLevel.Remark, SubtotalLevel.Year | SubtotalLevel.NonZero)]
        [InlineData("!!v", GatheringType.VoucherCount, SubtotalLevel.None)]
        [InlineData("`dC", GatheringType.Sum, SubtotalLevel.NonZero, SubtotalLevel.Day | SubtotalLevel.NonZero,
            SubtotalLevel.Currency | SubtotalLevel.NonZero)]
        [InlineData("!!wy", GatheringType.VoucherCount, SubtotalLevel.None, SubtotalLevel.Week, SubtotalLevel.Year)]
        [InlineData("``", GatheringType.Sum, SubtotalLevel.None, SubtotalLevel.Currency, SubtotalLevel.Title,
            SubtotalLevel.SubTitle, SubtotalLevel.User, SubtotalLevel.Content)]
        [InlineData("`", GatheringType.Sum, SubtotalLevel.NonZero, SubtotalLevel.Currency | SubtotalLevel.NonZero,
            SubtotalLevel.Title | SubtotalLevel.NonZero, SubtotalLevel.SubTitle | SubtotalLevel.NonZero,
            SubtotalLevel.User | SubtotalLevel.NonZero, SubtotalLevel.Content | SubtotalLevel.NonZero)]
        [InlineData("!", GatheringType.Count, SubtotalLevel.None, SubtotalLevel.Currency, SubtotalLevel.Title,
            SubtotalLevel.SubTitle, SubtotalLevel.User, SubtotalLevel.Content)]
        public void SubtotalTest(string s, GatheringType gt, params SubtotalLevel[] levels)
            => PairedSucc<ISubtotal>(nameof(ParsingF.Subtotal), s,
                r =>
                    {
                        Assert.Equal(gt, r.GatherType);
                        Assert.True(levels.SequenceEqual(r.Levels));
                    });

        [Theory]
        [InlineData(null)]
        public void SubtotalFailTest(string s)
            => PairedFail(nameof(ParsingF.Subtotal), s);

        [Theory]
        [InlineData("<+=1")]
        [InlineData("<:>")]
        public void DetailQueryTest(string s)
            => PairedSucc<IVoucherDetailQuery>(nameof(ParsingF.DetailQuery), s);

        [Theory]
        [InlineData(null)]
        [InlineData("[[]")]
        [InlineData("{}")]
        public void DetailQueryFailTest(string s)
            => PairedFail(nameof(ParsingF.DetailQuery), s);

        [Theory]
        [InlineData("U [[.]]")]
        [InlineData(@"/h\/h/")]
        public void DistributedQueryTest(string s)
            => PairedSucc<IQueryCompounded<IDistributedQueryAtom>>(nameof(ParsingF.DistributedQuery), s);

        [Theory]
        [InlineData(null)]
        [InlineData("[[]")]
        [InlineData("[[[")]
        public void DistributedQueryFailTest(string s)
            => PairedFail(nameof(ParsingF.DistributedQuery), s, DistributedQueryUnconstrained.Instance);
    }
}
