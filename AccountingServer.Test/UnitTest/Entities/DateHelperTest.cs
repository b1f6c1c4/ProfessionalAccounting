/* Copyright (C) 2020-2023 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.Diagnostics.CodeAnalysis;
using AccountingServer.Entities;
using Xunit;

namespace AccountingServer.Test.UnitTest.Entities;

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
        switch (expected)
        {
            case 0:
                Assert.Equal(0, result);
                break;
            case < 0:
                Assert.InRange(result, int.MinValue, -1);
                break;
            case > 0:
                Assert.InRange(result, +1, int.MaxValue);
                break;
        }

    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(true, "2017-01-01")]
    public void WithinTestUnconstrained(bool expected, string valueS)
    {
        var value = valueS == null ? (DateTime?)null : DateTimeParser.Parse(valueS);
        Assert.Equal(expected, DateHelper.Within(value, DateFilter.Unconstrained));
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, "2017-01-01")]
    public void WithinTestNullOnly(bool expected, string valueS)
    {
        var value = valueS == null ? (DateTime?)null : DateTimeParser.Parse(valueS);
        Assert.Equal(expected, DateHelper.Within(value, DateFilter.TheNullOnly));
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(true, "2016-12-31")]
    [InlineData(true, "2017-01-01")]
    [InlineData(false, "2017-01-02")]
    public void WithinTestBy(bool expected, string valueS)
    {
        var value = valueS == null ? (DateTime?)null : DateTimeParser.Parse(valueS);
        var filter = new DateFilter(null, new(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc));
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
        var value = valueS == null ? (DateTime?)null : DateTimeParser.Parse(valueS);
        var filter = new DateFilter(null, new(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc)) { Nullable = false };
        Assert.Equal(expected, DateHelper.Within(value, filter));
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(false, "2016-12-31")]
    [InlineData(true, "2017-01-01")]
    [InlineData(true, "2017-01-02")]
    public void WithinTestSince(bool expected, string valueS)
    {
        var value = valueS == null ? (DateTime?)null : DateTimeParser.Parse(valueS);
        var filter = new DateFilter(new(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc), null);
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
        var value = valueS == null ? (DateTime?)null : DateTimeParser.Parse(valueS);
        var filter = new DateFilter(new(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc), null) { Nullable = true };
        Assert.Equal(expected, DateHelper.Within(value, filter));
    }

    [Theory]
    [InlineData("2017-01-31", 2016, 12 + 1)]
    [InlineData("2017-02-28", 2016, 12 + 2)]
    [InlineData("2017-04-30", 2016, 12 + 4)]
    [InlineData("2017-01-31", 2018, 1 - 12)]
    [InlineData("2017-02-28", 2018, 2 - 12)]
    [InlineData("2017-04-30", 2018, 4 - 12)]
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
        Assert.Equal(expected, DateHelper.LastDayOfMonth(year, month));
    }
}
