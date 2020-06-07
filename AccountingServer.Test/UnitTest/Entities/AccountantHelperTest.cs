/* Copyright (C) 2020 b1f6c1c4
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

using System.Diagnostics.CodeAnalysis;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using Xunit;

namespace AccountingServer.Test.UnitTest.Entities
{
    [SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
    public class AccountantHelperTest
    {
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
            Assert.Equal(expected, AccountantHelper.LastDayOfMonth(year, month));
        }

        [Theory]
        [InlineData(true, 0)]
        [InlineData(true, +VoucherDetail.Tolerance)]
        [InlineData(true, -VoucherDetail.Tolerance * 0.999)]
        [InlineData(false, -VoucherDetail.Tolerance * 1.001)]
        public void IsNonNegativeTest(bool expected, double value)
            => Assert.Equal(expected, AccountantHelper.IsNonNegative(value));

        [Theory]
        [InlineData(true, 0)]
        [InlineData(true, -VoucherDetail.Tolerance)]
        [InlineData(true, +VoucherDetail.Tolerance * 0.999)]
        [InlineData(false, +VoucherDetail.Tolerance * 1.001)]
        public void IsNonPositiveTest(bool expected, double value)
            => Assert.Equal(expected, AccountantHelper.IsNonPositive(value));
    }
}
