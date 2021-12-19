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
using AccountingServer.Shell.Util;
using Xunit;

namespace AccountingServer.Test.UnitTest.Shell;

public class ParseTest
{
    [Theory]
    [InlineData("", "\t // \r\n ")]
    [InlineData("simple test", "\t // \r\n simple test")]
    [InlineData("/ //aer\r\nabc\r\ne//rt", "\t  / //aer\r\nabc\r\ne//rt")]
    [InlineData("abc\r\ne//rt", "\t  ///aer\r\nabc\r\ne//rt")]
    [InlineData("abc\r\ne//rt", "\t ///aer\t\r\n//'-*\"\r\nabc\r\ne//rt")]
    [InlineData("abc\r\ne//rt", " \t///aer\t\n\r\r\n//'-*\"\n\r\r\nabc\r\ne//rt")]
    [InlineData("", "\t // \n ")]
    [InlineData("simple test", "\t // \n simple test")]
    [InlineData("/ //aer\nabc\ne//rt", "\t  / //aer\nabc\ne//rt")]
    [InlineData("abc\ne//rt", "\t  ///aer\nabc\ne//rt")]
    [InlineData("abc\ne//rt", "\t ///aer\t\n//'-*\"\nabc\ne//rt")]
    [InlineData("abc\ne//rt", " \t///aer\t\n\n//'-*\"\n\nabc\ne//rt")]
    [InlineData("", "\t // \n\r ")]
    [InlineData("simple test", "\t // \n\r simple test")]
    [InlineData("/ //aer\n\rabc\n\re//rt", "\t  / //aer\n\rabc\n\re//rt")]
    [InlineData("abc\n\re//rt", "\t  ///aer\n\rabc\n\re//rt")]
    [InlineData("abc\n\re//rt", "\t ///aer\t\n\r//'-*\"\n\rabc\n\re//rt")]
    [InlineData("abc\n\re//rt", " \t///aer\t\n\r\n\r//'-*\"\n\r\n\rabc\n\re//rt")]
    public void TrimStartCommentTest(string expected, string expr)
    {
        ParseHelper.TrimStartComment(null, ref expr);
        Assert.Equal(expected, expr);
    }

    [Theory]
    [InlineData(null, "\t  \r\n ", "")]
    [InlineData("s-im_ple", "  s-im_ple test", " test")]
    [InlineData("si'm\"ple", "  si'm\"ple te\"s't", " te\"s't")]
    [InlineData("si' m\"ple te\"s", "  'si'' m\"ple te\"s't'\"'", "t'\"'")]
    [InlineData("si\r\n'm\"ple te\"s", "  \"si\r\n'm\"\"ple te\"\"s\"t'\"'", "t'\"'")]
    public void TokenTestAllow(string expected, string expr, string remain)
    {
        Assert.Equal(expected, ParseHelper.Token(null, ref expr));
        Assert.Equal(remain, expr);
    }

    [Theory]
    [InlineData("simple", " \talt tive", "alt tive", false)]
    [InlineData("alt", " \talt tive", " tive", true)]
    [InlineData("alt", " \t'alt' tive", " tive", true)]
    [InlineData("al't", " \t'al\'t' tive", "'al\'t' tive", false)]
    [InlineData("al't", " \t'al\'\'t' tive", " tive", true)]
    public void TokenTestAllowF(string expected, string expr, string remain, bool exists)
    {
        Assert.Equal(exists ? expected : null, ParseHelper.Token(null, ref expr, true, s => s == expected));
        Assert.Equal(remain, expr);
    }

    [Theory]
    [InlineData(null, "\t  \r\n ", "")]
    [InlineData("s-im_ple", "  s-im_ple test", " test")]
    [InlineData("si'm\"ple", "  si'm\"ple te\"s't", " te\"s't")]
    [InlineData("'si''", "  'si'' m\"ple te\"s't'\"'", " m\"ple te\"s't'\"'")]
    [InlineData("\"si", "  \"si\r\n'm\"\"ple te\"\"s\"t'\"'", "\r\n'm\"\"ple te\"\"s\"t'\"'")]
    public void TokenTestDisallow(string expected, string expr, string remain)
    {
        Assert.Equal(expected, ParseHelper.Token(null, ref expr, false));
        Assert.Equal(remain, expr);
    }

    [Theory]
    [InlineData("simple", " \talt tive", "alt tive", false)]
    [InlineData("alt", " \talt tive", " tive", true)]
    [InlineData("alt", " \t'alt' tive", "'alt' tive", false)]
    [InlineData("al't", " \tal\'t tive", " tive", true)]
    [InlineData("al't", " \tal\'t' tive", "al\'t' tive", false)]
    [InlineData("al't", " \t'al\'t' tive", "'al\'t' tive", false)]
    [InlineData("al't", " \t'al\'\'t' tive", "'al\'\'t' tive", false)]
    public void TokenTestDisallowF(string expected, string expr, string remain, bool exists)
    {
        Assert.Equal(exists ? expected : null, ParseHelper.Token(null, ref expr, false, s => s == expected));
        Assert.Equal(remain, expr);
    }

    [Theory]
    [InlineData(0, " 0.0000 asdf", " asdf")]
    [InlineData(null, " '0.0000' asdf", "'0.0000' asdf")]
    [InlineData(-1.57e-323, " -1.57e-323 asdf", " asdf")]
    [InlineData(157.0e305, " 157.0e305 asdf", " asdf")]
    [InlineData(.85e-0, " .85e-0 asdf", " asdf")]
    public void DoubleTest(double? expected, string expr, string remain)
    {
        Assert.Equal(expected, ParseHelper.Double(null, ref expr));
        Assert.Equal(remain, expr);
    }

    [Theory]
    [InlineData(0, " 0.0000 asdf", " asdf")]
    [InlineData(null, " '0.0000' asdf", "'0.0000' asdf")]
    [InlineData(-1.57e-323, " -1.57e-323 asdf", " asdf")]
    [InlineData(157.0e305, " 157.0e305 asdf", " asdf")]
    [InlineData(.85e-0, " .85e-0 asdf", " asdf")]
    public void DoubleFTest(double? expected, string expr, string remain)
    {
        if (expected == null)
            Assert.Throws<FormatException>(() => ParseHelper.DoubleF(null, ref expr));
        else
            Assert.Equal(expected, ParseHelper.DoubleF(null, ref expr));
        Assert.Equal(remain, expr);
    }

    [Theory]
    [InlineData("te xt  ", " te xt ", "te xt ", false)]
    [InlineData(" te xt ", " te xt ", "te xt ", false)]
    [InlineData("te xt  ", " te xt   qwe", " qwe", true)]
    public void OptionalTest(string opt, string expr, string remain, bool exists)
    {
        var res = ParseHelper.Optional(null, ref expr, opt);
        Assert.Equal(res, exists);
        Assert.Equal(remain, expr);
    }

    [Theory]
    [InlineData(null, "\t", "", null)]
    [InlineData("", " ''t es", "t es", null)]
    [InlineData(null, "  s-im_ple test", "s-im_ple test", '"')]
    [InlineData("haha", " 'haha'", "", null)]
    [InlineData("haha", " 'haha'", "", '\'')]
    [InlineData("-im_ple te", "  s-im_ple test", "t", null)]
    [InlineData("s-im_ple", "  's-im_ple' test", " test", null)]
    [InlineData(null, "  si'm\"ple te\"s't", "si'm\"ple te\"s't", '\'')]
    [InlineData("si'm\"ple te\"s", "  'si''m\"ple te\"s't'\"'", "t'\"'", null)]
    [InlineData("si\r\n'm\"ple te\"s", "  \"si\r\n'm\"\"ple te\"\"s\"t'\"'", "t'\"'", null)]
    public void QuotedTest(string expected, string expr, string remain, char? c)
    {
        Assert.Equal(expected, ParseHelper.Quoted(null, ref expr, c));
        Assert.Equal(remain, expr);
    }

    [Fact]
    public void EofTest()
    {
        ParseHelper.Eof(null, null);
        ParseHelper.Eof(null, "");
        Assert.Throws<ArgumentException>(() => ParseHelper.Eof(null, "asdf"));
    }

    [Fact]
    public void QuotedTest2()
    {
        var expr = " '";
        Assert.Throws<ArgumentException>(() => ParseHelper.Quoted(null, ref expr));
    }
}