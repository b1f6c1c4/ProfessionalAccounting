/* Copyright (C) 2020-2025 b1f6c1c4
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
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using Xunit;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Test.UnitTest.BLL;

public class DataFormatterTest
{
    private readonly Client m_Client = new() { User = "b1", Today = DateTime.UtcNow.Date };

    public DataFormatterTest()
    {
        Cfg.Assign(new BaseCurrencyInfos { Infos = new() { new() { Date = null, Currency = "CNY" } } });

        Cfg.Assign(new CurrencySymbols { Symbols = new() { new() { Currency = "DOGE", Symbol = "#" } } });
    }

    [Theory]
    [InlineData("U", null)]
    [InlineData("", "b1")]
    [InlineData(null, "b1")]
    [InlineData("Ub0", "b0")]
    [InlineData("U'b-1'", "b-1")]
    public void ParseUserSpecTest(string value, string fmt)
        => Assert.Equal(fmt, value.ParseUserSpec(m_Client));

    [Theory]
    [InlineData(null, "")]
    [InlineData("b1", "Ub1")]
    [InlineData("b-1", "U'b-1'")]
    public void AsUserTest(string value, string fmt)
        => Assert.Equal(fmt, value.AsUser());

    [Theory]
    [InlineData(null, null)]
    [InlineData("@@", "CNY")]
    [InlineData("@mXn", "MXN")]
    [InlineData("@#mXn#", "mXn#")]
    [InlineData("@#y@##n#", "y@#n#")]
    [InlineData("@###yYn#", "#yYn#")]
    [InlineData("@##", "#")]
    [InlineData("@####", "##")]
    public void ParseCurrencyTest(string value, string fmt)
        => Assert.Equal(fmt, value.ParseCurrency());

    [Theory]
    [InlineData(null, "")]
    [InlineData("cNy", "@cNy")]
    [InlineData("@@", "@@@")]
    [InlineData("##", "@####")]
    [InlineData("#mXn", "@#mXn")]
    [InlineData("#mXn#", "@###mXn#")]
    [InlineData("m@n#", "@#m@n#")]
    public void AsCurrencyTest(string value, string fmt)
        => Assert.Equal(fmt, value.AsCurrency());

    [Theory]
    [InlineData(null, null, "")]
    [InlineData(null, 12500.0, "12,500.    ")]
    [InlineData(null, 123.45, "123.45  ")]
    [InlineData("DOGE", null, "")]
    [InlineData("DOGE", 123.45, "#123.45  ")]
    [InlineData("DOGE", 0.00235, "#0.0024")]
    public void AsFundTest(string value, double? v, string fmt)
        => Assert.Equal(fmt, v.AsFund(value));

    [Theory]
    [InlineData(null, SubtotalLevel.None, "[null]")]
    [InlineData("2021-02-10", SubtotalLevel.None, "20210210")]
    [InlineData("2021-02-10", SubtotalLevel.Day, "20210210")]
    [InlineData("2021-02-08", SubtotalLevel.Week, "20210208")]
    [InlineData("2021-02-01", SubtotalLevel.Month, "202102")]
    [InlineData("2021-02-28", SubtotalLevel.Quarter, "2021Q1")]
    [InlineData("2021-03-31", SubtotalLevel.Quarter, "2021Q1")]
    [InlineData("2021-04-01", SubtotalLevel.Quarter, "2021Q2")]
    [InlineData("2021-06-11", SubtotalLevel.Quarter, "2021Q2")]
    [InlineData("2021-01-01", SubtotalLevel.Year, "2021")]
    public void AsDateTest(string value, SubtotalLevel level, string fmt)
        => Assert.Equal(fmt, value.ToDateTime().AsDate(level));

    [Theory]
    [InlineData("[]", "[]")]
    [InlineData("null", "[null]")]
    [InlineData("~null", "[~null]")]
    [InlineData("2019~202002", "[20190101~20200229]")]
    [InlineData("2019~~202002", "[20190101~~20200229]")]
    [InlineData("~2017", "[~20171231]")]
    [InlineData("~~2017", "[~~20171231]")]
    [InlineData("202002~", "[20200201~]")]
    [InlineData("202002~~", "[20200201~~]")]
    public void AsDateRangeTest(string value, string fmt)
        => Assert.Equal(fmt, ParsingF.Range(value, m_Client).AsDateRange());
}
