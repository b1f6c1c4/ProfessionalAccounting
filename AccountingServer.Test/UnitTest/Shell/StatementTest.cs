/* Copyright (C) 2024-2025 b1f6c1c4
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
using AccountingServer.Entities;
using AccountingServer.Shell.Plugins.Statement;
using Xunit;

namespace AccountingServer.Test.UnitTest.Plugins;

public class StatementTest
{
    private static Target Target = new()
        {
            Lengths = new()
                {
                    new LengthSpec { Period = "202305", Tolerance = 5 },
                },
            Overlaps = new()
                {
                    new OverlapSpec { From = "202302", To = "202303", Tolerance = 2 },
                    new OverlapSpec { From = "", To = "202301", Tolerance = 3 },
                    new OverlapSpec { From = "202307", To = "", Tolerance = 4 },
                },
        };

    private static IList<(string, DateTime)> L(List<(string, string)> l)
        => l.Select(static t => (t.Item1, DateTimeParser.ParseExact(t.Item2, "yyyyMMdd"))).ToList();

    [Fact]
    public void EmptyTest()
    {
        var inc = Statement.CalculateIncidents(L(new()), Target).ToList();
        Assert.Equal(0, inc.Count);
    }

    [Theory]
    [InlineData(0, 1, 1, 2, 3, 3)]
    [InlineData(0, 1, 2, 1, 2, 3)]
    [InlineData(1, 1, 2, 1, 3, 3)]
    [InlineData(0, 1, 2, 3, 2, 3)]
    [InlineData(1, 1, 1, 3, 2, 3)]
    [InlineData(1, 1, 3, 1, 2, 3)]
    [InlineData(2, 1, 2, 3, 1, 3)]
    public void NonMonotonicTest(int count, params int[] ps)
    {
        var d = DateTimeParser.ParseExact("20240101", "yyyyMMdd");
        var inc = Statement.CalculateIncidents(ps.Select(p => ($"20230{p}", d)).ToList(), Target).ToList();
        Assert.Equal(count, inc.Where(static inc => inc.Ty == Statement.IncidentType.NonMonotonic).Count());
        inc = Statement.CalculateIncidents(ps.Select(p => ($"20230{p * 2}", d)).ToList(), Target).ToList();
        Assert.Equal(count, inc.Where(static inc => inc.Ty == Statement.IncidentType.NonMonotonic).Count());
        inc = Statement.CalculateIncidents(ps.Select(p => ($"20230{p * 3}", d)).ToList(), Target).ToList();
        Assert.Equal(count, inc.Where(static inc => inc.Ty == Statement.IncidentType.NonMonotonic).Count());
    }

    [Theory]
    // 31+1 maximum
    [InlineData("202304", "20230201", "20230304", 0)]
    [InlineData("202304", "20230201", "20230305", 1)]
    // 31+5 maximum
    [InlineData("202305", "20230201", "20230308", 0)]
    [InlineData("202305", "20230201", "20230309", 1)]
    public void TargetLengthTest(string p, string d0, string d1, int count)
    {
        var inc = Statement.CalculateIncidents(L(new() { (p, d0), (p, d1) }), Target).ToList();
        Assert.Equal(count, inc.Count);
    }

    [Theory]
    [InlineData(0, 1, 1, 1, 1)]
    [InlineData(0, 1, 1, 0, 1)]
    [InlineData(0, 1, 3, 5, 7)]
    [InlineData(2, 1, 0, 3, 5, 0, 7)]
    [InlineData(1, 1, 0, 1, 3, 5, 0, 7)]
    [InlineData(1, 0, 1, 3, 5, 0, 7)]
    [InlineData(0, 0, 1, 3, 5, 0, 3, 5, 7)] // actually infeasible, but not to be detected
    [InlineData(1, 1, 0, 3, 5, 7, 0)]
    [InlineData(0, 0, 1, 4, 0, 4, 7)]
    [InlineData(1, 1, 4, 4, 0, 7)]
    [InlineData(0, 0, 1, 0, 1, 7, 0, 7, 0)]
    [InlineData(1, 1, 1, 0, 1, 0, 0, 7, 7)]
    public void PeriodUnmarkedTest(int count, params int[] ps)
    {
        var d = DateTimeParser.ParseExact("20240101", "yyyyMMdd");
        var inc = Statement.CalculateIncidents(ps.Select(p => (p == 0 ? "" : $"20230{p}", d)).ToList(), Target).ToList();
        Assert.Equal(count, inc.Where(static inc => inc.Ty == Statement.IncidentType.PeriodUnmarked).Count());
    }
}
