/* Copyright (C) 2020-2022 b1f6c1c4
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using Xunit;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Test.UnitTest.BLL;

public class SubtotalTest
{
    private readonly Client m_Client = new() { User = "b1", Today = DateTime.UtcNow.Date };

    private readonly IHistoricalExchange m_Exchange = new MockExchange
        {
            { new(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc), "JPY", 456 },
            { new(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc), "USD", 789 },
            { new(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc), "CNY", 1 },
        };

    public SubtotalTest()
        => Cfg.Assign(new BaseCurrencyInfos() { Infos = new() { new() { Date = null, Currency = "CNY" } } });

    [Theory]
    [InlineData("", "101110")]
    [InlineData("~null", "001110")]
    [InlineData("null", "100000")]
    [InlineData("~20170105", "101111")]
    [InlineData("~20170104", "101110")]
    [InlineData("~20170103", "101100")]
    [InlineData("~20170102", "101000")]
    [InlineData("~20170101", "110000")]
    [InlineData("~~20170105", "001111")]
    [InlineData("~~20170104", "001110")]
    [InlineData("~~20170103", "001100")]
    [InlineData("~~20170102", "001000")]
    [InlineData("~~20170101", "010000")]
    [InlineData("20170105~", "000001")]
    [InlineData("20170104~", "000010")]
    [InlineData("20170103~", "000110")]
    [InlineData("20170102~", "001110")]
    [InlineData("20170101~", "011110")]
    [InlineData("20170105~~", "100001")]
    [InlineData("20170104~~", "100010")]
    [InlineData("20170103~~", "100110")]
    [InlineData("20170102~~", "101110")]
    [InlineData("20170101~~", "111110")]
    [InlineData("20170101~~20170105", "111111")]
    [InlineData("20170101~~20170104", "111110")]
    [InlineData("20170101~~20170103", "111100")]
    [InlineData("20170101~~20170102", "111000")]
    [InlineData("20170101~~20170101", "110000")]
    [InlineData("20170105~~20170105", "100001")]
    [InlineData("20170104~~20170105", "100011")]
    [InlineData("20170103~~20170105", "100111")]
    [InlineData("20170102~~20170105", "101111")]
    [InlineData("20170101~20170105", "011111")]
    [InlineData("20170101~20170104", "011110")]
    [InlineData("20170101~20170103", "011100")]
    [InlineData("20170101~20170102", "011000")]
    [InlineData("20170101~20170101", "010000")]
    [InlineData("20170105~20170105", "000001")]
    [InlineData("20170104~20170105", "000011")]
    [InlineData("20170103~20170105", "000111")]
    [InlineData("20170102~20170105", "001111")]
    public async Task TestAggrEvery(string rng, string incl)
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery($"``vD[{rng}]", m_Client).Subtotal, m_Exchange);

        var bal = new Balance[]
            {
                new() { Date = null, Fund = 1 },
                new() { Date = new(2017, 01, 02, 0, 0, 0, DateTimeKind.Utc), Fund = 2 },
                new() { Date = new(2017, 01, 04, 0, 0, 0, DateTimeKind.Utc), Fund = 4 },
            };

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalDate>(item);
            var resxx = (ISubtotalDate)item;
            Assert.Null(resxx.Items);
            Assert.Equal(SubtotalLevel.Day, resxx.Level);
            lst.Add(new() { Date = resxx.Date, Fund = resxx.Fund });
        }

        Assert.Equal(bal.Sum(static b => b.Fund), res.Fund);

        var lst0 = new List<Balance>();
        if (incl[0] == '1')
            lst0.Add(new() { Date = null, Fund = 1 });
        if (incl[1] == '1')
            lst0.Add(new() { Date = new(2017, 01, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 1 });
        if (incl[2] == '1')
            lst0.Add(new() { Date = new(2017, 01, 02, 0, 0, 0, DateTimeKind.Utc), Fund = 1 + 2 });
        if (incl[3] == '1')
            lst0.Add(new() { Date = new(2017, 01, 03, 0, 0, 0, DateTimeKind.Utc), Fund = 1 + 2 });
        if (incl[4] == '1')
            lst0.Add(new() { Date = new(2017, 01, 04, 0, 0, 0, DateTimeKind.Utc), Fund = 1 + 2 + 4 });
        if (incl[5] == '1')
            lst0.Add(new() { Date = new(2017, 01, 05, 0, 0, 0, DateTimeKind.Utc), Fund = 1 + 2 + 4 });
        Assert.Equal(lst0, lst, new BalanceEqualityComparer());
    }

    [Theory]
    [InlineData("", "001110")]
    [InlineData("~null", "001110")]
    [InlineData("null", "100000")]
    [InlineData("~20170105", "001111")]
    [InlineData("~20170104", "001110")]
    [InlineData("~20170103", "001100")]
    [InlineData("~20170102", "001000")]
    [InlineData("~20170101", "010000")]
    [InlineData("~~20170105", "001111")]
    [InlineData("~~20170104", "001110")]
    [InlineData("~~20170103", "001100")]
    [InlineData("~~20170102", "001000")]
    [InlineData("~~20170101", "010000")]
    [InlineData("20170105~", "000001")]
    [InlineData("20170104~", "000010")]
    [InlineData("20170103~", "000110")]
    [InlineData("20170102~", "001110")]
    [InlineData("20170101~", "011110")]
    [InlineData("20170105~~", "100001")]
    [InlineData("20170104~~", "100010")]
    [InlineData("20170103~~", "100110")]
    [InlineData("20170102~~", "101110")]
    [InlineData("20170101~~", "111110")]
    [InlineData("20170101~~20170105", "111111")]
    [InlineData("20170101~~20170104", "111110")]
    [InlineData("20170101~~20170103", "111100")]
    [InlineData("20170101~~20170102", "111000")]
    [InlineData("20170101~~20170101", "110000")]
    [InlineData("20170105~~20170105", "100001")]
    [InlineData("20170104~~20170105", "100011")]
    [InlineData("20170103~~20170105", "100111")]
    [InlineData("20170102~~20170105", "101111")]
    [InlineData("20170101~20170105", "011111")]
    [InlineData("20170101~20170104", "011110")]
    [InlineData("20170101~20170103", "011100")]
    [InlineData("20170101~20170102", "011000")]
    [InlineData("20170101~20170101", "010000")]
    [InlineData("20170105~20170105", "000001")]
    [InlineData("20170104~20170105", "000011")]
    [InlineData("20170103~20170105", "000111")]
    [InlineData("20170102~20170105", "001111")]
    public async Task TestAggrEveryNoNull(string rng, string incl)
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery($"``vD[{rng}]", m_Client).Subtotal, m_Exchange);

        var bal = new Balance[]
            {
                new() { Date = new(2017, 01, 02, 0, 0, 0, DateTimeKind.Utc), Fund = 2 },
                new() { Date = new(2017, 01, 04, 0, 0, 0, DateTimeKind.Utc), Fund = 4 },
            };

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalDate>(item);
            var resxx = (ISubtotalDate)item;
            Assert.Null(resxx.Items);
            Assert.Equal(SubtotalLevel.Day, resxx.Level);
            lst.Add(new() { Date = resxx.Date, Fund = resxx.Fund });
        }

        Assert.Equal(bal.Sum(static b => b.Fund), res.Fund);

        var lst0 = new List<Balance>();
        if (incl[0] == '1')
            lst0.Add(new() { Date = null, Fund = 0 });
        if (incl[1] == '1')
            lst0.Add(new() { Date = new(2017, 01, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 0 });
        if (incl[2] == '1')
            lst0.Add(new() { Date = new(2017, 01, 02, 0, 0, 0, DateTimeKind.Utc), Fund = 2 });
        if (incl[3] == '1')
            lst0.Add(new() { Date = new(2017, 01, 03, 0, 0, 0, DateTimeKind.Utc), Fund = 2 });
        if (incl[4] == '1')
            lst0.Add(new() { Date = new(2017, 01, 04, 0, 0, 0, DateTimeKind.Utc), Fund = 2 + 4 });
        if (incl[5] == '1')
            lst0.Add(new() { Date = new(2017, 01, 05, 0, 0, 0, DateTimeKind.Utc), Fund = 2 + 4 });
        Assert.Equal(lst0, lst, new BalanceEqualityComparer());
    }

    [Theory]
    [InlineData("", "0000")]
    [InlineData("~null", "0000")]
    [InlineData("null", "1000")]
    [InlineData("~20170103", "0001")]
    [InlineData("~20170102", "0010")]
    [InlineData("~20170101", "0100")]
    [InlineData("~~20170103", "0001")]
    [InlineData("~~20170102", "0010")]
    [InlineData("~~20170101", "0100")]
    [InlineData("20170103~", "0001")]
    [InlineData("20170102~", "0010")]
    [InlineData("20170101~", "0100")]
    [InlineData("20170103~~", "1001")]
    [InlineData("20170102~~", "1010")]
    [InlineData("20170101~~", "1100")]
    [InlineData("20170101~~20170103", "1111")]
    [InlineData("20170101~~20170102", "1110")]
    [InlineData("20170101~~20170101", "1100")]
    [InlineData("20170103~~20170103", "1001")]
    [InlineData("20170102~~20170103", "1011")]
    [InlineData("20170101~20170103", "0111")]
    [InlineData("20170101~20170102", "0110")]
    [InlineData("20170101~20170101", "0100")]
    [InlineData("20170103~20170103", "0001")]
    [InlineData("20170102~20170103", "0011")]
    public async Task TestAggrEveryNull(string rng, string incl)
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery($"``vD[{rng}]", m_Client).Subtotal, m_Exchange);

        var bal = Array.Empty<Balance>();

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalDate>(item);
            var resxx = (ISubtotalDate)item;
            Assert.Null(resxx.Items);
            Assert.Equal(SubtotalLevel.Day, resxx.Level);
            lst.Add(new() { Date = resxx.Date, Fund = resxx.Fund });
        }

        Assert.Equal(bal.Sum(static b => b.Fund), res.Fund);

        var lst0 = new List<Balance>();
        if (incl[0] == '1')
            lst0.Add(new() { Date = null, Fund = 0 });
        if (incl[1] == '1')
            lst0.Add(new() { Date = new(2017, 01, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 0 });
        if (incl[2] == '1')
            lst0.Add(new() { Date = new(2017, 01, 02, 0, 0, 0, DateTimeKind.Utc), Fund = 0 });
        if (incl[3] == '1')
            lst0.Add(new() { Date = new(2017, 01, 03, 0, 0, 0, DateTimeKind.Utc), Fund = 0 });
        Assert.Equal(lst0, lst, new BalanceEqualityComparer());
    }


    [Theory]
    [InlineData("", "101110")]
    [InlineData("~null", "001110")]
    [InlineData("null", "100000")]
    [InlineData("~20170105", "101111")]
    [InlineData("~20170104", "101110")]
    [InlineData("~20170103", "101100")]
    [InlineData("~20170102", "101000")]
    [InlineData("~20170101", "110000")]
    [InlineData("~~20170105", "001111")]
    [InlineData("~~20170104", "001110")]
    [InlineData("~~20170103", "001100")]
    [InlineData("~~20170102", "001000")]
    [InlineData("~~20170101", "010000")]
    [InlineData("20170105~", "000001")]
    [InlineData("20170104~", "000010")]
    [InlineData("20170103~", "000110")]
    [InlineData("20170102~", "001110")]
    [InlineData("20170101~", "011110")]
    [InlineData("20170105~~", "100001")]
    [InlineData("20170104~~", "100010")]
    [InlineData("20170103~~", "100110")]
    [InlineData("20170102~~", "101110")]
    [InlineData("20170101~~", "111110")]
    [InlineData("20170101~~20170105", "111111")]
    [InlineData("20170101~~20170104", "111110")]
    [InlineData("20170101~~20170103", "111100")]
    [InlineData("20170101~~20170102", "111000")]
    [InlineData("20170101~~20170101", "110000")]
    [InlineData("20170105~~20170105", "100001")]
    [InlineData("20170104~~20170105", "100011")]
    [InlineData("20170103~~20170105", "100111")]
    [InlineData("20170102~~20170105", "101111")]
    [InlineData("20170101~20170105", "011111")]
    [InlineData("20170101~20170104", "011110")]
    [InlineData("20170101~20170103", "011100")]
    [InlineData("20170101~20170102", "011000")]
    [InlineData("20170101~20170101", "010000")]
    [InlineData("20170105~20170105", "000001")]
    [InlineData("20170104~20170105", "000011")]
    [InlineData("20170103~20170105", "000111")]
    [InlineData("20170102~20170105", "001111")]
    public async Task TestAggrEveryEqui(string rng, string incl)
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery($"``vD[{rng}]X[20170101]", m_Client).Subtotal,
            m_Exchange);

        var bal = new Balance[]
            {
                new() { Date = null, Currency = "JPY", Fund = 1 / 2D / 456D },
                new()
                    {
                        Date = new(2017, 01, 02, 0, 0, 0, DateTimeKind.Utc),
                        Currency = "JPY",
                        Fund = 2 / 2D / 456D,
                    },
                new()
                    {
                        Date = new(2017, 01, 04, 0, 0, 0, DateTimeKind.Utc),
                        Currency = "JPY",
                        Fund = 4 / 2D / 456D,
                    },
                new() { Date = null, Currency = "USD", Fund = 1 / 2D / 789D },
                new()
                    {
                        Date = new(2017, 01, 02, 0, 0, 0, DateTimeKind.Utc),
                        Currency = "USD",
                        Fund = 2 / 2D / 789D,
                    },
                new()
                    {
                        Date = new(2017, 01, 04, 0, 0, 0, DateTimeKind.Utc),
                        Currency = "USD",
                        Fund = 4 / 2D / 789D,
                    },
            };

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalDate>(item);
            var resxx = (ISubtotalDate)item;
            Assert.Null(resxx.Items);
            Assert.Equal(SubtotalLevel.Day, resxx.Level);
            lst.Add(new() { Date = resxx.Date, Fund = resxx.Fund });
        }

        Assert.Equal(1 + 2 + 4, res.Fund);

        var lst0 = new List<Balance>();
        if (incl[0] == '1')
            lst0.Add(new() { Date = null, Fund = 1 });
        if (incl[1] == '1')
            lst0.Add(new() { Date = new(2017, 01, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 1 });
        if (incl[2] == '1')
            lst0.Add(new() { Date = new(2017, 01, 02, 0, 0, 0, DateTimeKind.Utc), Fund = 1 + 2 });
        if (incl[3] == '1')
            lst0.Add(new() { Date = new(2017, 01, 03, 0, 0, 0, DateTimeKind.Utc), Fund = 1 + 2 });
        if (incl[4] == '1')
            lst0.Add(new() { Date = new(2017, 01, 04, 0, 0, 0, DateTimeKind.Utc), Fund = 1 + 2 + 4 });
        if (incl[5] == '1')
            lst0.Add(new() { Date = new(2017, 01, 05, 0, 0, 0, DateTimeKind.Utc), Fund = 1 + 2 + 4 });
        Assert.Equal(lst0, lst, new BalanceEqualityComparer());
    }

    [Theory]
    [InlineData("", "0000")]
    [InlineData("~null", "0000")]
    [InlineData("null", "1000")]
    [InlineData("~20170103", "0001")]
    [InlineData("~20170102", "0010")]
    [InlineData("~20170101", "0100")]
    [InlineData("~~20170103", "0001")]
    [InlineData("~~20170102", "0010")]
    [InlineData("~~20170101", "0100")]
    [InlineData("20170103~", "0001")]
    [InlineData("20170102~", "0010")]
    [InlineData("20170101~", "0100")]
    [InlineData("20170103~~", "1001")]
    [InlineData("20170102~~", "1010")]
    [InlineData("20170101~~", "1100")]
    [InlineData("20170101~~20170103", "1111")]
    [InlineData("20170101~~20170102", "1110")]
    [InlineData("20170101~~20170101", "1100")]
    [InlineData("20170103~~20170103", "1001")]
    [InlineData("20170102~~20170103", "1011")]
    [InlineData("20170101~20170103", "0111")]
    [InlineData("20170101~20170102", "0110")]
    [InlineData("20170101~20170101", "0100")]
    [InlineData("20170103~20170103", "0001")]
    [InlineData("20170102~20170103", "0011")]
    public async Task TestAggrEveryNullEqui(string rng, string incl)
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery($"``vD[{rng}]X[20170101]", m_Client).Subtotal,
            m_Exchange);

        var bal = Array.Empty<Balance>();

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalDate>(item);
            var resxx = (ISubtotalDate)item;
            Assert.Null(resxx.Items);
            Assert.Equal(SubtotalLevel.Day, resxx.Level);
            lst.Add(new() { Date = resxx.Date, Fund = resxx.Fund });
        }

        Assert.Equal(0, res.Fund);

        var lst0 = new List<Balance>();
        if (incl[0] == '1')
            lst0.Add(new() { Date = null, Fund = 0 });
        if (incl[1] == '1')
            lst0.Add(new() { Date = new(2017, 01, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 0 });
        if (incl[2] == '1')
            lst0.Add(new() { Date = new(2017, 01, 02, 0, 0, 0, DateTimeKind.Utc), Fund = 0 });
        if (incl[3] == '1')
            lst0.Add(new() { Date = new(2017, 01, 03, 0, 0, 0, DateTimeKind.Utc), Fund = 0 });
        Assert.Equal(lst0, lst, new BalanceEqualityComparer());
    }

    [Fact]
    public async Task TestAggrChanged()
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`vD", m_Client).Subtotal, m_Exchange);

        var bal = new Balance[]
            {
                new() { Date = new(2017, 01, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 8 },
                new() { Date = new(2017, 01, 02, 0, 0, 0, DateTimeKind.Utc), Fund = 1 },
                new() { Date = new(2017, 01, 04, 0, 0, 0, DateTimeKind.Utc), Fund = 4 },
                new() { Date = new(2017, 01, 05, 0, 0, 0, DateTimeKind.Utc), Fund = 2 },
            };

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalDate>(item);
            var resxx = (ISubtotalDate)item;
            Assert.Null(resxx.Items);
            Assert.Equal(SubtotalLevel.Day, resxx.Level);
            lst.Add(new() { Date = resxx.Date, Fund = resxx.Fund });
        }

        Assert.Equal(bal.Sum(static b => b.Fund), res.Fund);
        Assert.Equal(
            new Balance[]
                {
                    new() { Date = new(2017, 01, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 8 },
                    new() { Date = new(2017, 01, 02, 0, 0, 0, DateTimeKind.Utc), Fund = 8 + 1 },
                    new() { Date = new(2017, 01, 04, 0, 0, 0, DateTimeKind.Utc), Fund = 8 + 1 + 4 },
                    new() { Date = new(2017, 01, 05, 0, 0, 0, DateTimeKind.Utc), Fund = 8 + 1 + 4 + 2 },
                },
            lst,
            new BalanceEqualityComparer());
    }

    [Fact]
    public async Task TestAggrChangedEquivalent()
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`vDX[20170101]", m_Client).Subtotal, m_Exchange);

        var bal = new Balance[]
            {
                new() { Currency = "JPY", Date = new(2017, 01, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 8 },
                new() { Currency = "CNY", Date = new(2017, 01, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 1 },
                new() { Currency = "USD", Date = new(2017, 01, 05, 0, 0, 0, DateTimeKind.Utc), Fund = 4 },
                new() { Currency = "CNY", Date = new(2017, 01, 05, 0, 0, 0, DateTimeKind.Utc), Fund = 2 },
            };

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalDate>(item);
            var resxx = (ISubtotalDate)item;
            Assert.Null(resxx.Items);
            Assert.Equal(SubtotalLevel.Day, resxx.Level);
            lst.Add(new() { Date = resxx.Date, Fund = resxx.Fund });
        }

        Assert.Equal(8 * 456 + 1 + 4 * 789 + 2, res.Fund);
        Assert.Equal(
            new Balance[]
                {
                    new() { Date = new(2017, 01, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 8 * 456 + 1 },
                    new()
                        {
                            Date = new(2017, 01, 05, 0, 0, 0, DateTimeKind.Utc),
                            Fund = 8 * 456 + 1 + 4 * 789 + 2,
                        },
                },
            lst,
            new BalanceEqualityComparer());
    }

    [Fact]
    public async Task TestContent()
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`c", m_Client).Subtotal, m_Exchange);

        var bal = new Balance[]
            {
                new() { Content = "JPY", Fund = 8 },
                new() { Content = "CNY", Fund = 1 },
                new() { Content = "USD", Fund = 4 },
                new() { Content = "CNY", Fund = 2 },
            };

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalContent>(item);
            var resxx = (ISubtotalContent)item;
            Assert.Null(resxx.Items);
            lst.Add(new() { Content = resxx.Content, Fund = resxx.Fund });
        }

        Assert.Equal(bal.Sum(static b => b.Fund), res.Fund);
        Assert.Equal(
            new List<Balance>
                {
                    new() { Content = "JPY", Fund = 8 },
                    new() { Content = "CNY", Fund = 3 },
                    new() { Content = "USD", Fund = 4 },
                },
            lst,
            new BalanceEqualityComparer());
    }


    [Fact]
    public async Task TestCurrency()
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`C", m_Client).Subtotal, m_Exchange);

        var bal = new Balance[]
            {
                new() { Currency = "JPY", Fund = 8 },
                new() { Currency = "CNY", Fund = 1 },
                new() { Currency = "USD", Fund = 4 },
                new() { Currency = "CNY", Fund = 2 },
            };

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalCurrency>(item);
            var resxx = (ISubtotalCurrency)item;
            Assert.Null(resxx.Items);
            lst.Add(new() { Currency = resxx.Currency, Fund = resxx.Fund });
        }

        Assert.Equal(bal.Sum(static b => b.Fund), res.Fund);
        Assert.Equal(
            new List<Balance>
                {
                    new() { Currency = "JPY", Fund = 8 },
                    new() { Currency = "CNY", Fund = 3 },
                    new() { Currency = "USD", Fund = 4 },
                },
            lst,
            new BalanceEqualityComparer());
    }

    [Fact]
    public async Task TestDay()
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`d", m_Client).Subtotal, m_Exchange);

        var bal = new Balance[]
            {
                new() { Date = new(2017, 01, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 8 },
                new() { Date = new(2017, 01, 02, 0, 0, 0, DateTimeKind.Utc), Fund = 1 },
                new() { Date = new(2017, 01, 03, 0, 0, 0, DateTimeKind.Utc), Fund = 4 },
                new() { Date = new(2017, 01, 04, 0, 0, 0, DateTimeKind.Utc), Fund = 2 },
            };

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalDate>(item);
            var resxx = (ISubtotalDate)item;
            Assert.Null(resxx.Items);
            Assert.Equal(SubtotalLevel.Day | SubtotalLevel.NonZero, resxx.Level);
            lst.Add(new() { Date = resxx.Date, Fund = resxx.Fund });
        }

        Assert.Equal(bal.Sum(static b => b.Fund), res.Fund);
        Assert.Equal(bal, lst, new BalanceEqualityComparer());
    }

    [Fact]
    public async Task TestEquivalent()
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`vX@JPY[20170101]", m_Client).Subtotal, m_Exchange);

        var bal = new Balance[]
            {
                new() { Currency = "JPY", Fund = 8 },
                new() { Currency = "CNY", Fund = 1 },
                new() { Currency = "USD", Fund = 4 },
                new() { Currency = "CNY", Fund = 2 },
            };

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        Assert.Null(res.Items);
        Assert.Equal((8 * 456 + 1 + 4 * 789 + 2) / 456D, res.Fund, 14);
    }

    [Fact]
    public async Task TestMonth()
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`m", m_Client).Subtotal, m_Exchange);

        var bal = new Balance[]
            {
                new() { Date = new(2017, 01, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 8 },
                new() { Date = new(2017, 02, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 1 },
                new() { Date = new(2017, 03, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 4 },
                new() { Date = new(2017, 04, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 2 },
            };

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalDate>(item);
            var resxx = (ISubtotalDate)item;
            Assert.Null(resxx.Items);
            Assert.Equal(SubtotalLevel.Month | SubtotalLevel.NonZero, resxx.Level);
            lst.Add(new() { Date = resxx.Date, Fund = resxx.Fund });
        }

        Assert.Equal(bal.Sum(static b => b.Fund), res.Fund);
        Assert.Equal(bal, lst, new BalanceEqualityComparer());
    }

    [Fact]
    public async Task TestRemark()
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`r", m_Client).Subtotal, m_Exchange);

        var bal = new Balance[]
            {
                new() { Remark = "JPY", Fund = 8 },
                new() { Remark = "CNY", Fund = 1 },
                new() { Remark = "USD", Fund = 4 },
                new() { Remark = "CNY", Fund = 2 },
            };

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalRemark>(item);
            var resxx = (ISubtotalRemark)item;
            Assert.Null(resxx.Items);
            lst.Add(new() { Remark = resxx.Remark, Fund = resxx.Fund });
        }

        Assert.Equal(bal.Sum(static b => b.Fund), res.Fund);
        Assert.Equal(
            new List<Balance>
                {
                    new() { Remark = "JPY", Fund = 8 },
                    new() { Remark = "CNY", Fund = 3 },
                    new() { Remark = "USD", Fund = 4 },
                },
            lst,
            new BalanceEqualityComparer());
    }

    [Fact]
    public async Task TestTitle()
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`t", m_Client).Subtotal, m_Exchange);

        var bal = new Balance[]
            {
                new() { Title = 4567, Fund = 8 },
                new() { Title = 1234, Fund = 1 },
                new() { Title = 6543, Fund = 4 },
                new() { Title = 1234, Fund = 2 },
            };

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalTitle>(item);
            var resxx = (ISubtotalTitle)item;
            Assert.Null(resxx.Items);
            lst.Add(new() { Title = resxx.Title, Fund = resxx.Fund });
        }

        Assert.Equal(bal.Sum(static b => b.Fund), res.Fund);
        Assert.Equal(
            new List<Balance>
                {
                    new() { Title = 4567, Fund = 8 },
                    new() { Title = 1234, Fund = 3 },
                    new() { Title = 6543, Fund = 4 },
                },
            lst,
            new BalanceEqualityComparer());
    }

    [Fact]
    public async Task TestTitleSubTitle()
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`ts", m_Client).Subtotal, m_Exchange);

        var bal = new Balance[]
            {
                new() { Title = 4567, Fund = 8 },
                new() { Title = 1234, Fund = 1 },
                new() { Title = 6543, Fund = 4 },
                new() { Title = 1234, Fund = 2 },
                new() { Title = 4567, SubTitle = 01, Fund = 128 },
                new() { Title = 1234, SubTitle = 02, Fund = 16 },
                new() { Title = 6543, SubTitle = 01, Fund = 64 },
                new() { Title = 1234, SubTitle = 08, Fund = 32 },
            };

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalTitle>(item);
            var resxx = (ISubtotalTitle)item;
            foreach (var item2 in resxx.Items)
            {
                Assert.IsAssignableFrom<ISubtotalSubTitle>(item2);
                var resxxx = (ISubtotalSubTitle)item2;
                Assert.Null(resxxx.Items);
                lst.Add(new() { Title = resxx.Title, SubTitle = resxxx.SubTitle, Fund = resxxx.Fund });
            }
        }

        Assert.Equal(bal.Sum(static b => b.Fund), res.Fund);
        Assert.Equal(
            new List<Balance>
                {
                    new() { Title = 4567, Fund = 8 },
                    new() { Title = 4567, SubTitle = 01, Fund = 128 },
                    new() { Title = 1234, Fund = 3 },
                    new() { Title = 1234, SubTitle = 02, Fund = 16 },
                    new() { Title = 1234, SubTitle = 08, Fund = 32 },
                    new() { Title = 6543, Fund = 4 },
                    new() { Title = 6543, SubTitle = 01, Fund = 64 },
                },
            lst,
            new BalanceEqualityComparer());
    }

    [Fact]
    public async Task TestUser()
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`U", m_Client).Subtotal, m_Exchange);

        var bal = new Balance[]
            {
                new() { User = "JPY", Fund = 8 },
                new() { User = "CNY", Fund = 1 },
                new() { User = "USD", Fund = 4 },
                new() { User = "CNY", Fund = 2 },
            };

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalUser>(item);
            var resxx = (ISubtotalUser)item;
            Assert.Null(resxx.Items);
            lst.Add(new() { User = resxx.User, Fund = resxx.Fund });
        }

        Assert.Equal(bal.Sum(static b => b.Fund), res.Fund);
        Assert.Equal(
            new List<Balance>
                {
                    new() { User = "JPY", Fund = 8 },
                    new() { User = "CNY", Fund = 3 },
                    new() { User = "USD", Fund = 4 },
                },
            lst,
            new BalanceEqualityComparer());
    }

    [Fact]
    public async Task TestWeek()
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`w", m_Client).Subtotal, m_Exchange);

        var bal = new Balance[]
            {
                new() { Date = new(2017, 01, 02, 0, 0, 0, DateTimeKind.Utc), Fund = 8 },
                new() { Date = new(2017, 01, 09, 0, 0, 0, DateTimeKind.Utc), Fund = 1 },
                new() { Date = new(2017, 01, 16, 0, 0, 0, DateTimeKind.Utc), Fund = 4 },
                new() { Date = new(2017, 01, 23, 0, 0, 0, DateTimeKind.Utc), Fund = 2 },
            };

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalDate>(item);
            var resxx = (ISubtotalDate)item;
            Assert.Null(resxx.Items);
            Assert.Equal(SubtotalLevel.Week | SubtotalLevel.NonZero, resxx.Level);
            lst.Add(new() { Date = resxx.Date, Fund = resxx.Fund });
        }

        double sum = 0;
        foreach (var b in bal)
            sum += b.Fund;

        Assert.Equal(sum, res.Fund);
        Assert.Equal(bal, lst, new BalanceEqualityComparer());
    }

    [Fact]
    public async Task TestYear()
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`y", m_Client).Subtotal, m_Exchange);

        var bal = new Balance[]
            {
                new() { Date = new(2014, 01, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 8 },
                new() { Date = new(2015, 01, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 1 },
                new() { Date = new(2016, 01, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 4 },
                new() { Date = new(2017, 01, 01, 0, 0, 0, DateTimeKind.Utc), Fund = 2 },
            };

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalDate>(item);
            var resxx = (ISubtotalDate)item;
            Assert.Null(resxx.Items);
            Assert.Equal(SubtotalLevel.Year | SubtotalLevel.NonZero, resxx.Level);
            lst.Add(new() { Date = resxx.Date, Fund = resxx.Fund });
        }

        Assert.Equal(bal.Sum(static b => b.Fund), res.Fund);
        Assert.Equal(bal, lst, new BalanceEqualityComparer());
    }

    [Fact]
    public async Task TestValue()
    {
        var builder = new SubtotalBuilder(ParsingF.GroupedQuery("`V", m_Client).Subtotal, m_Exchange);

        var bal = new Balance[]
            {
                new() { Value = 1, Fund = 8 },
                new() { Value = 2, Fund = 1 },
                new() { Value = 3, Fund = 4 },
                new() { Value = 2, Fund = 2 },
            };

        var res = await builder.Build(bal.ToAsyncEnumerable());
        Assert.IsAssignableFrom<ISubtotalRoot>(res);
        var resx = (ISubtotalRoot)res;

        var lst = new List<Balance>();
        foreach (var item in resx.Items)
        {
            Assert.IsAssignableFrom<ISubtotalValue>(item);
            var resxx = (ISubtotalValue)item;
            Assert.Null(resxx.Items);
            lst.Add(new() { Value = resxx.Value, Fund = resxx.Fund });
        }

        Assert.Equal(bal.Sum(static b => b.Fund), res.Fund);
        Assert.Equal(
            new List<Balance>
                {
                    new() { Value = 1, Fund = 8 }, new() { Value = 2, Fund = 3 }, new() { Value = 3, Fund = 4 },
                },
            lst,
            new BalanceEqualityComparer());
    }
}
