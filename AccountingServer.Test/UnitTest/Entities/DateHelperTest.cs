using System;
using System.Diagnostics.CodeAnalysis;
using AccountingServer.Entities;
using Xunit;

namespace AccountingServer.Test.UnitTest.Entities
{
    [SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
    public class DateHelperTest
    {
        [Theory]
        [InlineData(0, null, null)]
        [InlineData(-1, null, "2017-01-01")]
        [InlineData(+1, "2017-01-01", null)]
        [InlineData(-1, "2016-12-31", "2017-01-01")]
        [InlineData(0, "2017-01-01", "2017-01-01")]
        [InlineData(+1, "2017-01-02", "2017-01-01")]
        public void CompareDateTest(int expected, string b1S, string b2S)
        {
            var b1 = b1S.ToDateTime();
            var b2 = b2S.ToDateTime();

            var result = DateHelper.CompareDate(b1, b2);
            if (expected == 0)
                Assert.Equal(0, result);
            if (expected < 0)
                Assert.InRange(result, int.MinValue, -1);
            if (expected > 0)
                Assert.InRange(result, +1, int.MaxValue);
        }

        [Theory]
        [InlineData(true, null)]
        [InlineData(true, "2017-01-01")]
        public void WithinTestUnconstrained(bool expected, string valueS)
        {
            var value = valueS == null ? (DateTime?)null : ClientDateTime.Parse(valueS);
            Assert.Equal(expected, DateHelper.Within(value, DateFilter.Unconstrained));
        }

        [Theory]
        [InlineData(true, null)]
        [InlineData(false, "2017-01-01")]
        public void WithinTestNullOnly(bool expected, string valueS)
        {
            var value = valueS == null ? (DateTime?)null : ClientDateTime.Parse(valueS);
            Assert.Equal(expected, DateHelper.Within(value, DateFilter.TheNullOnly));
        }

        [Theory]
        [InlineData(true, null)]
        [InlineData(true, "2016-12-31")]
        [InlineData(true, "2017-01-01")]
        [InlineData(false, "2017-01-02")]
        public void WithinTestBy(bool expected, string valueS)
        {
            var value = valueS == null ? (DateTime?)null : ClientDateTime.Parse(valueS);
            var filter = new DateFilter(null, new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            Assert.True(filter.Nullable);
            Assert.Equal(expected, DateHelper.Within(value, filter));
        }

        [Theory]
        [InlineData(false, null)]
        [InlineData(true, "2016-12-31")]
        [InlineData(true, "2017-01-01")]
        [InlineData(false, "2017-01-02")]
        public void WithinTestByA(bool expected, string valueS)
        {
            var value = valueS == null ? (DateTime?)null : ClientDateTime.Parse(valueS);
            var filter = new DateFilter(null, new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc)) { Nullable = false };
            Assert.Equal(expected, DateHelper.Within(value, filter));
        }

        [Theory]
        [InlineData(false, null)]
        [InlineData(false, "2016-12-31")]
        [InlineData(true, "2017-01-01")]
        [InlineData(true, "2017-01-02")]
        public void WithinTestSince(bool expected, string valueS)
        {
            var value = valueS == null ? (DateTime?)null : ClientDateTime.Parse(valueS);
            var filter = new DateFilter(new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc), null);
            Assert.False(filter.Nullable);
            Assert.Equal(expected, DateHelper.Within(value, filter));
        }

        [Theory]
        [InlineData(true, null)]
        [InlineData(false, "2016-12-31")]
        [InlineData(true, "2017-01-01")]
        [InlineData(true, "2017-01-02")]
        public void WithinTestSinceA(bool expected, string valueS)
        {
            var value = valueS == null ? (DateTime?)null : ClientDateTime.Parse(valueS);
            var filter = new DateFilter(new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc), null) { Nullable = true };
            Assert.Equal(expected, DateHelper.Within(value, filter));
        }
    }
}
