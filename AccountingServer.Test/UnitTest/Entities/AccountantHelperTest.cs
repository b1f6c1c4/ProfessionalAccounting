using System.Diagnostics.CodeAnalysis;
using AccountingServer.Entities.Util;
using Xunit;

namespace AccountingServer.Test.UnitTest.Entities
{
    [SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
    public class AccountantHelperTest
    {
        [Theory]
        [InlineData("2017-01-31", 2017, 1)]
        [InlineData("2017-02-28", 2017, 2)]
        [InlineData("2017-04-30", 2017, 4)]
        [InlineData("2020-01-31", 2020, 1)]
        [InlineData("2020-02-29", 2020, 2)]
        [InlineData("2020-04-30", 2020, 4)]
        [InlineData("2100-01-31", 2100, 1)]
        [InlineData("2100-02-28", 2100, 2)]
        [InlineData("2100-04-30", 2100, 4)]
        public void LastDayOfMonthTest(string expectedS, int year, int month)
        {
            var expected = expectedS.ToDateTime();
            Assert.Equal(expected, AccountantHelper.LastDayOfMonth(year, month));
        }
    }
}
