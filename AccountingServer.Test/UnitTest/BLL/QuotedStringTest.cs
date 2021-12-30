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

using System;
using AccountingServer.BLL.Util;
using Xunit;

namespace AccountingServer.Test.UnitTest.BLL;

public class QuotedStringTest
{
    [Theory]
    [InlineData(null, '\'')]
    [InlineData("", '\'')]
    [InlineData("simple", '\'')]
    [InlineData("'s'i'm'ple'''", '\'')]
    [InlineData("\"\"'s\\'i'm\"'\"ple'\"''\"", '\'')]
    [InlineData("simple", '"')]
    [InlineData("'s'i'm'ple'''", '"')]
    [InlineData("\"\"'s\\'i'm\"'\"ple'\"''\"", '"')]
    public void QuotationTest(string text, char ch)
        => Assert.Equal(text ?? "", text.Quotation(ch).Dequotation());

    [Fact]
    public void DequotationTest()
    {
        Assert.Null(QuotedStringHelper.Dequotation(null));
        Assert.Equal("", "".Dequotation());
        Assert.Throws<ArgumentException>(static () => "\'".Dequotation());
        Assert.Throws<ArgumentException>(static () => "\'aaerv\"".Dequotation());
    }
}
