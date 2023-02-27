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
using AccountingServer.Entities.Util;
using Xunit;

namespace AccountingServer.Test.UnitTest.Entities;

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

    private static void StringPrefixMatchTest(Func<string, string, bool> pred)
    {
        Assert.True(pred(null, null));
        Assert.True(pred("", null));
        Assert.True(pred("whatever", null));

        Assert.False(pred(null, ""));
        Assert.True(pred("", ""));
        Assert.True(pred("whatever", ""));

        Assert.False(pred(null, "W"));
        Assert.False(pred("", "W"));
        Assert.True(pred("w", "W"));
        Assert.True(pred("whatever", "W"));
        Assert.False(pred("hat", "W"));

        Assert.False(pred("a", "[ab]"));
        Assert.False(pred("ba", "[ab]"));
        Assert.True(pred("[ab]", "[ab]"));
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
        => Assert.Equal(expected, MatchHelper.IsMatch(new() { ID = value }, new Voucher { ID = filter }));

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
        Assert.Equal(expected, MatchHelper.IsMatch(new() { Date = value }, new Voucher { Date = filter }));
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
        => Assert.Equal(expected, MatchHelper.IsMatch(new() { Type = value }, new Voucher { Type = filter }));

    [Theory]
    [InlineData(true, "b1", null)]
    [InlineData(true, "b2", null)]
    [InlineData(false, "b1", "")]
    [InlineData(false, "b2", "")]
    [InlineData(false, "b1", "b2")]
    [InlineData(true, "b2", "b2")]
    public void DetailMatchTestUser(bool expected, string value, string filter)
        => Assert.Equal(
            expected,
            MatchHelper.IsMatch(new() { User = value }, new VoucherDetail { User = filter }));

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
        => Assert.Equal(
            expected,
            MatchHelper.IsMatch(new() { Currency = value }, new VoucherDetail { Currency = filter }));

    [Theory]
    [InlineData(true, 1001, null)]
    [InlineData(false, 1000, 1001)]
    [InlineData(true, 1001, 1001)]
    public void DetailMatchTestTitle(bool expected, int? value, int? filter)
        => Assert.Equal(
            expected,
            MatchHelper.IsMatch(new() { Title = value }, new VoucherDetail { Title = filter }));

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
        => Assert.Equal(
            expected,
            MatchHelper.IsMatch(new() { SubTitle = value }, new VoucherDetail { SubTitle = filter }));

    [Theory]
    [InlineData(true, null, null, false)]
    [InlineData(true, null, null, true)]
    [InlineData(true, 123.45, null, false)]
    [InlineData(true, 123.45, null, true)]
    [InlineData(false, null, 123.45, false)]
    [InlineData(false, null, 123.45, true)]
    [InlineData(false, 123.45 + 2 * VoucherDetail.Tolerance, 123.45, false)]
    [InlineData(false, 123.45 + 2 * VoucherDetail.Tolerance, 123.45, true)]
    [InlineData(true, 123.45 + 0.5 * VoucherDetail.Tolerance, 123.45, false)]
    [InlineData(true, 123.45 + 0.5 * VoucherDetail.Tolerance, 123.45, true)]
    [InlineData(false, 123.45 + 2 * VoucherDetail.Tolerance, -123.45, false)]
    [InlineData(false, 123.45 + 2 * VoucherDetail.Tolerance, -123.45, true)]
    [InlineData(false, 123.45 + 0.5 * VoucherDetail.Tolerance, -123.45, false)]
    [InlineData(true, 123.45 + 0.5 * VoucherDetail.Tolerance, -123.45, true)]
    [InlineData(false, -123.45 + 2 * VoucherDetail.Tolerance, 123.45, false)]
    [InlineData(false, -123.45 + 2 * VoucherDetail.Tolerance, 123.45, true)]
    [InlineData(false, -123.45 + 0.5 * VoucherDetail.Tolerance, 123.45, false)]
    [InlineData(true, -123.45 + 0.5 * VoucherDetail.Tolerance, 123.45, true)]
    public void DetailMatchTestFund(bool expected, double? value, double? filter, bool isBi)
        => Assert.Equal(expected, MatchHelper.IsMatch(new() { Fund = value }, new() { Fund = filter }, isBi: isBi));

    [Theory]
    [InlineData(true, null, null)]
    [InlineData(false, 500, TitleKind.Asset)]
    [InlineData(false, 500, TitleKind.Liquidity)]
    [InlineData(true, 1099, TitleKind.Liquidity)]
    [InlineData(false, 1200, TitleKind.Liquidity)]
    [InlineData(true, 1500, TitleKind.Asset)]
    [InlineData(false, 1500, TitleKind.Liquidity)]
    [InlineData(false, 2000, TitleKind.Asset)]
    [InlineData(false, 2000, TitleKind.Liquidity)]
    [InlineData(false, 1500, TitleKind.Liability)]
    [InlineData(true, 2500, TitleKind.Liability)]
    [InlineData(false, 3000, TitleKind.Liability)]
    [InlineData(false, 3500, TitleKind.Equity)]
    [InlineData(true, 4500, TitleKind.Equity)]
    [InlineData(false, 5000, TitleKind.Equity)]
    [InlineData(false, 5800, TitleKind.Revenue)]
    [InlineData(true, 6000, TitleKind.Revenue)]
    [InlineData(false, 6400, TitleKind.Revenue)]
    [InlineData(false, 6300, TitleKind.Expense)]
    [InlineData(true, 6500, TitleKind.Expense)]
    [InlineData(false, 7000, TitleKind.Expense)]
    [InlineData(false, 1012, TitleKind.Investment, 00)]
    [InlineData(true, 1012, TitleKind.Investment, 04)]
    [InlineData(false, 1100, TitleKind.Investment)]
    [InlineData(true, 1101, TitleKind.Investment)]
    [InlineData(true, 1599, TitleKind.Investment)]
    [InlineData(true, 6199, TitleKind.Investment)]
    [InlineData(true, 1601, TitleKind.Spending)]
    [InlineData(false, 1602, TitleKind.Spending)]
    [InlineData(true, 6401, TitleKind.Spending)]
    [InlineData(true, 6602, TitleKind.Spending, 00)]
    [InlineData(false, 6602, TitleKind.Spending, 11)]
    public void DetailMatchTestKind(bool expected, int? value, TitleKind? kind, int? sub = null)
        => Assert.Equal(expected,
            MatchHelper.IsMatch(new() { Title = value, SubTitle = sub}, new(), kind));

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
        => Assert.Equal(expected,
            MatchHelper.IsMatch(new() { Fund = value }, new(), dir: dir));

    [Fact]
    public void DetailMatchTestContent()
        => StringMatchTest(static (v, f)
            => MatchHelper.IsMatch(new() { Content = v }, new VoucherDetail { Content = f }));

    [Fact]
    public void DetailMatchTestContentPrefix()
        => StringPrefixMatchTest(static (v, f) => MatchHelper.IsMatch(new() { Content = v }, new(), contentPrefix: f));

    [Fact]
    public void DetailMatchTestNull()
        => Assert.True(
            MatchHelper.IsMatch(
                new() { Content = "cnt", Fund = 10, Remark = "rmk" },
                (VoucherDetail)null));

    [Fact]
    public void DetailMatchTestRemark()
        => StringMatchTest(static (v, f)
            => MatchHelper.IsMatch(new() { Remark = v }, new VoucherDetail { Remark = f }));

    [Fact]
    public void VoucherMatchTest()
    {
        var filter = new Voucher
            {
                ID = "abc",
                Date = new(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Type = VoucherType.Ordinary,
                Remark = "abc",
            };

        Assert.False(
            MatchHelper.IsMatch(
                new()
                    {
                        ID = "abc0",
                        Date = new(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        Type = VoucherType.Ordinary,
                        Remark = "abc",
                    },
                filter));
        Assert.False(
            MatchHelper.IsMatch(
                new()
                    {
                        ID = "abc",
                        Date = new(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        Type = VoucherType.Ordinary,
                        Remark = "abc",
                    },
                filter));
        Assert.False(
            MatchHelper.IsMatch(
                new()
                    {
                        ID = "abc",
                        Date = new(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        Type = VoucherType.Carry,
                        Remark = "abc",
                    },
                filter));
        Assert.False(
            MatchHelper.IsMatch(
                new()
                    {
                        ID = "abc",
                        Date = new(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        Type = VoucherType.Ordinary,
                        Remark = "abc0",
                    },
                filter));
        Assert.True(
            MatchHelper.IsMatch(
                new()
                    {
                        ID = "abc",
                        Date = new(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        Type = VoucherType.Ordinary,
                        Remark = "abc",
                    },
                filter));
    }

    [Fact]
    public void VoucherMatchTestNull()
        => Assert.True(MatchHelper.IsMatch(new() { ID = "id", Remark = "rmk" }, (Voucher)null));

    [Fact]
    public void VoucherMatchTestRemark()
        => StringMatchTest(static (v, f) => MatchHelper.IsMatch(new() { Remark = v }, new Voucher { Remark = f }));

    [Fact]
    public void DetailMatchTestRemarkPrefix()
        => StringPrefixMatchTest(static (v, f) => MatchHelper.IsMatch(new() { Remark = v }, new(), remarkPrefix: f));
}
