using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using Xunit;

namespace AccountingServer.Test.UnitTest.Shell
{
    public abstract class SerializerTest
    {
        protected SerializerTest()
        {
            BaseCurrency.BaseCurrencyInfos = new MockConfigManager<BaseCurrencyInfos>(
                new BaseCurrencyInfos
                    {
                        Infos = new List<BaseCurrencyInfo>
                            {
                                new BaseCurrencyInfo { Date = null, Currency = "CNY" }
                            }
                    });

            TitleManager.TitleInfos =
                new MockConfigManager<TitleInfos>(new TitleInfos { Titles = new List<TitleInfo>() });

            ClientUser.Set("b1");
        }

        protected abstract IEntitySerializer GetSerializer();

        public virtual void SimpleTest()
        {
            var serializer = GetSerializer();

            Assert.NotNull(serializer.PresentVoucher(null));
            Assert.Throws<FormatException>(() => serializer.ParseVoucher(""));
            Assert.Throws<FormatException>(() => serializer.ParseVoucher("new Voucher {"));
        }

        public virtual void VoucherTest(string dt, VoucherType type)
        {
            var serializer = GetSerializer();

            var voucher1 = new Voucher
                {
                    Date = dt.ToDateTime(),
                    Type = type,
                    Remark = " t 't\"-.\" %@!@#$%^&*( ",
                    Details = new List<VoucherDetail>
                        {
                            new VoucherDetail
                                {
                                    User = "b1",
                                    Currency = "JPY",
                                    Title = 1001,
                                    Content = "%@!@#$%^&*(\nas\rdf\\",
                                    Fund = 123.45
                                },
                            new VoucherDetail
                                {
                                    User = "b1",
                                    Currency = "USD",
                                    Title = 1002,
                                    SubTitle = 12,
                                    Fund = -123.45,
                                    Remark = "\\qw\ter%@!@#$%^&*(%"
                                },
                            new VoucherDetail
                                {
                                    User = "b1",
                                    Currency = "EUR",
                                    Title = 5555,
                                    Fund = -5,
                                    Remark = "  7' 46r0*\" &)%\" *%)^ Q23'4"
                                }
                        }
                };

            var voucher2 = serializer.ParseVoucher(serializer.PresentVoucher(voucher1));
            voucher2.Type = voucher2.Type ?? VoucherType.Ordinary;

            Assert.Equal(voucher1, voucher2, new VoucherEqualityComparer());
        }

        protected class DataProvider : IEnumerable<object[]>
        {
            private static readonly List<object[]> Data = new List<object[]>
                {
                    new object[] { null, VoucherType.Ordinary },
                    new object[] { "2017-01-01", VoucherType.Uncertain },
                    new object[] { null, VoucherType.Carry },
                    new object[] { null, VoucherType.AnnualCarry },
                    new object[] { "2017-01-01", VoucherType.Depreciation },
                    new object[] { null, VoucherType.Devalue },
                    new object[] { "2017-01-01", VoucherType.Amortization }
                };

            public IEnumerator<object[]> GetEnumerator() => Data.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => Data.GetEnumerator();
        }
    }

    public class CSharpSerializerTest : SerializerTest
    {
        protected override IEntitySerializer GetSerializer() => new CSharpSerializer();

        [Theory]
        [ClassData(typeof(DataProvider))]
        public override void VoucherTest(string dt, VoucherType type) { base.VoucherTest(dt, type); }

        [Fact]
        public override void SimpleTest() { base.SimpleTest(); }
    }

    public class ExprSerializerTest : SerializerTest
    {
        protected override IEntitySerializer GetSerializer() => new ExprSerializer();

        [Theory]
        [ClassData(typeof(DataProvider))]
        public override void VoucherTest(string dt, VoucherType type) { base.VoucherTest(dt, type); }

        [Fact]
        public void OtherTest()
        {
            var serializer = GetSerializer();

            Assert.Null(serializer.ParseVoucher(@"new Voucher { T1001 null }").Details.Single().Fund);
        }

        [Fact]
        public override void SimpleTest() { base.SimpleTest(); }
    }

    public class AbbrSerializerTest : SerializerTest
    {
        public AbbrSerializerTest()
            => AbbrSerializer.Abbrs =
                new MockConfigManager<Abbreviations>(
                    new Abbreviations
                        {
                            Abbrs = new List<Abbreviation>
                                {
                                    new Abbreviation
                                        {
                                            Abbr = "abbr1",
                                            Title = 1234,
                                            SubTitle = 04,
                                            Content = "cnt",
                                            Editable = false
                                        },
                                    new Abbreviation
                                        {
                                            Abbr = "abbr2",
                                            Title = 1234,
                                            Editable = true
                                        }
                                }
                        });

        protected override IEntitySerializer GetSerializer() => new AbbrSerializer();

        [Theory]
        [ClassData(typeof(DataProvider))]
        public override void VoucherTest(string dt, VoucherType type) { base.VoucherTest(dt, type); }

        [Fact]
        public void OtherTest()
        {
            var serializer = GetSerializer();

            var voucher = serializer.ParseVoucher(@"new Voucher { abbr1 123 abbr2 ""gg"" 765 }");
            Assert.Equal(
                new Voucher
                    {
                        Date = ClientDateTime.Today,
                        Details = new List<VoucherDetail>
                            {
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "CNY",
                                        Title = 1234,
                                        SubTitle = 04,
                                        Content = "cnt",
                                        Remark = null,
                                        Fund = 123
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "CNY",
                                        Title = 1234,
                                        Content = "gg",
                                        Fund = 765
                                    }
                            }
                    },
                voucher,
                new VoucherEqualityComparer());
        }

        [Fact]
        public override void SimpleTest() { base.SimpleTest(); }
    }

    public class JsonSerializerTest : SerializerTest
    {
        protected override IEntitySerializer GetSerializer() => new JsonSerializer();

        [Theory]
        [ClassData(typeof(DataProvider))]
        public override void VoucherTest(string dt, VoucherType type) { base.VoucherTest(dt, type); }

        [Fact]
        public override void SimpleTest() { base.SimpleTest(); }
    }

    public class DiscountSerializerTest
    {
        public DiscountSerializerTest()
        {
            BaseCurrency.BaseCurrencyInfos = new MockConfigManager<BaseCurrencyInfos>(
                new BaseCurrencyInfos
                    {
                        Infos = new List<BaseCurrencyInfo>
                            {
                                new BaseCurrencyInfo { Date = null, Currency = "CNY" }
                            }
                    });

            TitleManager.TitleInfos =
                new MockConfigManager<TitleInfos>(new TitleInfos { Titles = new List<TitleInfo>() });

            AbbrSerializer.Abbrs =
                new MockConfigManager<Abbreviations>(
                    new Abbreviations
                        {
                            Abbrs = new List<Abbreviation>
                                {
                                    new Abbreviation
                                        {
                                            Abbr = "aaa",
                                            Title = 1001,
                                            Editable = false
                                        }
                                }
                        });

            ClientUser.Set("b1");
        }

        private static IEntitySerializer GetSerializer() => new DiscountSerializer();

        [Fact]
        public void SimpleTest()
        {
            var serializer = GetSerializer();

            Assert.Throws<FormatException>(() => serializer.ParseVoucher(""));
            Assert.Throws<NotImplementedException>(() => serializer.ParseVoucher("new Voucher {"));
            Assert.Throws<NotImplementedException>(() => serializer.ParseVoucher("new Voucher { }"));
            Assert.Throws<NotImplementedException>(() => serializer.ParseVoucherDetail("whatever"));
        }

        [Fact]
        public void TaxTest()
        {
            var serializer = GetSerializer();

            var voucher = serializer.ParseVoucher(
                @"new Voucher {
! 20180101 @usd
T1001 + ! T1002 : 1000-50 950+50 ;
! aaa + T100366 : 1000 ;
t10
t2

@cny T1001 -2912
}");
            Assert.Equal(
                new Voucher
                    {
                        Date = new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        Type = VoucherType.Ordinary,
                        Details = new List<VoucherDetail>
                            {
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "CNY",
                                        Title = 1001,
                                        Fund = -2912
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1001,
                                        Fund = 1000 + 500 + 6
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1002,
                                        Fund = 950 + 4
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1003,
                                        SubTitle = 66,
                                        Fund = 500 + 2
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 6603,
                                        Fund = -50
                                    }
                            }
                    },
                voucher,
                new VoucherEqualityComparer());
        }

        [Fact]
        public void TaxDiscountTest()
        {
            var serializer = GetSerializer();

            var voucher = serializer.ParseVoucher(
                @"new Voucher {
! @usd
T1001 + ! T1002 : 1000-0.1-49.9 950+50 ;
! aaa + T100366 hei ha : 1000 ;
t2
d40
t10
d8

@usd T1001 -29120
}");
            Assert.Equal(
                new Voucher
                {
                    Date = ClientDateTime.Today,
                    Type = VoucherType.Ordinary,
                    Details = new List<VoucherDetail>
                            {
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1001,
                                        Fund = -29120
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1001,
                                        Fund = 1000 + 500 + 6 - 8
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1002,
                                        Fund = 950 + 4 - 16
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1003,
                                        SubTitle = 66,
                                        Content = "hei",
                                        Remark = "ha",
                                        Fund = 500 + 2
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 6603,
                                        Fund = -50 - 16 - 8
                                    }
                            }
                },
                voucher,
                new VoucherEqualityComparer());
        }

        [Fact]
        public void TaxDiscountNullTest()
        {
            var serializer = GetSerializer();

            var voucher = serializer.ParseVoucher(
                @"new Voucher {
! @usd
T1001 + ! T1002 : 1000-50 950+5+45 ;
! aaa + T100366 hei ha : 1000 ;
t2
t10
dnull

@usd T1001 -2864
}");
            Assert.Equal(
                new Voucher
                {
                    Date = ClientDateTime.Today,
                    Type = VoucherType.Ordinary,
                    Details = new List<VoucherDetail>
                            {
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1001,
                                        Fund = -2864
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1001,
                                        Fund = 1000 + 500 + 6 - 8
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1002,
                                        Fund = 950 + 4 - 16
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1003,
                                        SubTitle = 66,
                                        Content = "hei",
                                        Remark = "ha",
                                        Fund = 500 + 2
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 6603,
                                        Fund = -50 - 16 - 8
                                    }
                            }
                },
                voucher,
                new VoucherEqualityComparer());
        }

        [Fact]
        public void TaxNullDiscountTest()
        {
            var serializer = GetSerializer();

            var voucher = serializer.ParseVoucher(
                @"new Voucher {
! @usd
T1001 + ! T1002 : 1000=950 950+50 ;
! aaa + T100366 hei ha : 1000=1000 ;
d48
tnull

@usd T1001 -2864
}");
            Assert.Equal(
                new Voucher
                {
                    Date = ClientDateTime.Today,
                    Type = VoucherType.Ordinary,
                    Details = new List<VoucherDetail>
                            {
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1001,
                                        Fund = -2864
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1001,
                                        Fund = 1000 + 500 + 6 - 8
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1002,
                                        Fund = 950 + 4 - 16
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1003,
                                        SubTitle = 66,
                                        Content = "hei",
                                        Remark = "ha",
                                        Fund = 500 + 2
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 6603,
                                        Fund = -50 - 16 - 8
                                    }
                            }
                },
                voucher,
                new VoucherEqualityComparer());
        }

        [Fact]
        public void ManyNullTest()
        {
            var serializer = GetSerializer();

            Assert.Throws<ApplicationException>(
                () => serializer.ParseVoucher(
                    @"new Voucher {
! @usd
aaa : 1 ;
tnull
dnull

@usd T1001 null
}"));
            Assert.Throws<ApplicationException>(
                () => serializer.ParseVoucher(
                    @"new Voucher {
! @usd
aaa : 1 ;
tnull
dnull

@usd T1001 -1
}"));
            Assert.Throws<ApplicationException>(
                () => serializer.ParseVoucher(
                    @"new Voucher {
! @usd
aaa : 1 ;
dnull

@usd T1001 null
}"));
            Assert.Throws<ApplicationException>(
                () => serializer.ParseVoucher(
                    @"new Voucher {
! @usd
aaa : 1 ;
tnull

@usd T1001 null
}"));
        }
    }
}
