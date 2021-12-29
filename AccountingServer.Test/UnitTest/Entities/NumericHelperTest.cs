/* Copyright (C) 2020-2021 b1f6c1c4
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

namespace AccountingServer.Test.UnitTest.Entities;

[SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
public class NumericHelperTest
{
    [Theory]
    [InlineData(true, 0)]
    [InlineData(true, +VoucherDetail.Tolerance)]
    [InlineData(true, -VoucherDetail.Tolerance * 0.999)]
    [InlineData(false, -VoucherDetail.Tolerance * 1.001)]
    public void IsNonNegativeTest(bool expected, double value)
        => Assert.Equal(expected, NumericHelper.IsNonNegative(value));

    [Theory]
    [InlineData(true, 0)]
    [InlineData(true, -VoucherDetail.Tolerance)]
    [InlineData(true, +VoucherDetail.Tolerance * 0.999)]
    [InlineData(false, +VoucherDetail.Tolerance * 1.001)]
    public void IsNonPositiveTest(bool expected, double value)
        => Assert.Equal(expected, NumericHelper.IsNonPositive(value));
}
