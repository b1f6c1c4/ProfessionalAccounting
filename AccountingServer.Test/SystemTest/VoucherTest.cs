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
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell;
using AccountingServer.Shell.Serializer;
using Xunit;

namespace AccountingServer.Test.SystemTest
{
    [Collection("DbTestCollection")]
    public class VoucherTest
    {
        private readonly Facade m_Facade;

        public VoucherTest()
        {
            DAL.Facade.Create(db: "accounting-test").DeleteVouchers(VoucherQueryUnconstrained.Instance);

            BaseCurrency.BaseCurrencyInfos = new MockConfigManager<BaseCurrencyInfos>(
                new()
                    {
                        Infos = new()
                            {
                                new() { Date = null, Currency = "CNY" },
                            },
                    });

            TitleManager.TitleInfos = new MockConfigManager<TitleInfos>(new()
                {
                    Titles = new()
                        {
                            new() { Direction = 0, Id = 1234, IsVirtual = true, Name = "sth", SubTitles = new()
                                {
                                    new() { Direction = 0, Id = 01, Name = "obj" },
                                }},
                            new() { Direction = 0, Id = 5678, IsVirtual = false, Name = "els" },
                            new() { Direction = 0, Id = 3998, IsVirtual = false, Name = "kyh" },
                        },
                });

            AbbrSerializer.Abbrs = new MockConfigManager<Abbreviations>( new()
                        {
                            Abbrs = new()
                                {
                                    new() { Abbr = "aaa", Title = 5678, Editable = false },
                                },
                        });

            ClientUser.Set("b1");

            m_Facade = new(db: "accounting-test");
        }

        [Fact]
        public void Test()
        {
            Assert.Equal("@new Voucher {\n\n}@\n", m_Facade.EmptyVoucher(null));

            IQueryResult res;

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
", m_Facade.ExecuteVoucherUpsert("new Voucher { Ub2 T123401 whatever / aaa huh 10 }", null));

            res = m_Facade.Execute("\"huh\"", null);
            Assert.False(res.Dirty);
            Assert.False(res.AutoReturn);
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

            var id = new Regex(@"\^[0-9a-f]{24}\^").Match(res.ToString()!).Value;

            Assert.ThrowsAny<Exception>(() => m_Facade.Execute("sraw Ub2", null));

            res = m_Facade.Execute("unsafe sraw Ub2", null);
            Assert.False(res.Dirty);
            Assert.False(res.AutoReturn);
            Assert.Matches(@"[0-9]{8} // sth-obj
Ub2 T123401 'whatever'\s+-10
[0-9]{8} // kyh
Ub2 T3998\s+10
", res.ToString()!);

            Assert.ThrowsAny<Exception>(() => m_Facade.Execute("invalid command", null));

            res = m_Facade.Execute("json U > `t", null);
            Assert.False(res.Dirty);
            Assert.False(res.AutoReturn);
            Assert.Equal(@"{
  ""value"": 20.0,
  ""title"": {
    ""3998"": {
      ""value"": 10.0
    },
    ""5678"": {
      ""value"": 10.0
    }
  }
}", res.ToString()!);

            res = m_Facade.Execute("chk-1", null);
            Assert.False(res.Dirty);
            Assert.True(res.AutoReturn);
            Assert.Equal("OK", res.ToString()!);

            Assert.True(m_Facade.ExecuteVoucherRemoval($"new Voucher {{ {id} }}", null));
            Assert.False(m_Facade.ExecuteVoucherRemoval($"new Voucher {{ {id} }}", null));
        }
    }
}
