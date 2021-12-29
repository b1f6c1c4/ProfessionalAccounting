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
using System.Text.RegularExpressions;
using AccountingServer.BLL.Util;
using AccountingServer.Entities.Util;
using AccountingServer.Shell;
using AccountingServer.Shell.Serializer;
using Xunit;

namespace AccountingServer.Test.SystemTest;

[CollectionDefinition("DbTestCollection", DisableParallelization = true)]
public class VoucherTest
{
    private readonly Facade m_Facade;
    private readonly Session m_Session;

    public VoucherTest()
    {
        DAL.Facade.Create(db: "accounting-test").DeleteVouchers(VoucherQueryUnconstrained.Instance).AsTask().Wait();

        BaseCurrency.BaseCurrencyInfos = new MockConfigManager<BaseCurrencyInfos>(
            new() { Infos = new() { new() { Date = null, Currency = "CNY" } } });

        TitleManager.TitleInfos = new MockConfigManager<TitleInfos>(new()
            {
                Titles = new()
                    {
                        new()
                            {
                                Direction = 0,
                                Id = 1234,
                                IsVirtual = true,
                                Name = "sth",
                                SubTitles = new() { new() { Direction = 0, Id = 01, Name = "obj" } },
                            },
                        new() { Direction = 0, Id = 5678, IsVirtual = false, Name = "els" },
                        new() { Direction = 0, Id = 3998, IsVirtual = false, Name = "kyh" },
                    },
            });

        AbbrSerializer.Abbrs = new MockConfigManager<Abbreviations>(new()
            {
                Abbrs = new() { new() { Abbr = "aaa", Title = 5678, Editable = false } },
            });

        m_Facade = new(db: "accounting-test");
        m_Session = m_Facade.CreateSession("b1", DateTime.UtcNow.Date);

        var res = m_Facade.ExecuteVoucherUpsert(m_Session, "new Voucher { Ub2 T123401 whatever / aaa huh 10 }");
        Assert.Matches(@"@new Voucher {\^[0-9a-f]{24}\^
[0-9]{8}
// kyh
T3998\s+-10
// els
T5678 '' ""huh""\s+10
// sth-obj
Ub2 T123401 'whatever'\s+-10
// kyh
Ub2 T3998\s+10
}@
", res);
        m_ID = new Regex(@"\^[0-9a-f]{24}\^").Match(res).Value;
    }

    private readonly string m_ID;

    [Fact]
    public void EmptyTest()
        => Assert.Equal("@new Voucher {\n\n}@\n", m_Facade.EmptyVoucher(m_Session));

    [Fact]
    public void SimpleTest()
    {
        var res = m_Facade.Execute(m_Session, "\"huh\"");
        Assert.Matches(@"@new Voucher {\^[0-9a-f]{24}\^
[0-9]{8}
// kyh
T3998\s+-10
// els
T5678 '' ""huh""\s+10
// sth-obj
Ub2 T123401 'whatever'\s+-10
// kyh
Ub2 T3998\s+10
}@
", res.ToString()!);
    }

    [Fact]
    public void SafeSrawTest()
        => Assert.ThrowsAny<Exception>(() => m_Facade.Execute(m_Session, "sraw Ub2"));

    [Fact]
    public void UnsafeSrawTest()
    {
        var res = m_Facade.Execute(m_Session, "unsafe sraw Ub2");
        Assert.Matches(@"[0-9]{8} // sth-obj
Ub2 T123401 'whatever'\s+-10
[0-9]{8} // kyh
Ub2 T3998\s+10
", res.ToString()!);
    }

    [Fact]
    public void InvalidTest()
        => Assert.ThrowsAny<Exception>(() => m_Facade.Execute(m_Session, "invalid command"));

    [Fact]
    public void SubtotalTest()
    {
        var res = m_Facade.Execute(m_Session, "json U > `t");
        Assert.Matches(@"{
  ""value"": 20.0,
  ""title"": {
    ""(?:3998|5678)"": {
      ""value"": 10.0
    },
    ""(?:5678|3998)"": {
      ""value"": 10.0
    }
  }
}", res.ToString()!);
    }

    [Fact]
    public void FancyTest()
    {
        var res = m_Facade.Execute(m_Session, "unsafe fancy U T3998");
        Assert.Matches(@"@new Voucher {\^[0-9a-f]{24}\^
[0-9]{8}
// kyh
T3998\s+-10
// kyh
Ub2 T3998\s+10
}@
", res.ToString()!);
    }

    [Fact]
    public void ChkTest()
    {
        var res = m_Facade.Execute(m_Session, "chk-1");
        Assert.Equal("OK", res.ToString()!);
    }

    [Fact]
    public void RemoveTest()
    {
        Assert.True(m_Facade.ExecuteVoucherRemoval(m_Session, $"new Voucher {{ {m_ID} }}"));
        Assert.False(m_Facade.ExecuteVoucherRemoval(m_Session, $"new Voucher {{ {m_ID} }}"));
    }
}
