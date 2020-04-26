using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using Xunit;

namespace AccountingServer.Test.UnitTest.Shell
{
    [Collection("SerializerTestCollection")]
    public abstract class SerializerTest
    {
        protected SerializerTest()
        {
            BaseCurrency.BaseCurrencyInfos = new MockConfigManager<BaseCurrencyInfos>(
                new BaseCurrencyInfos
                    {
                        Infos = new List<BaseCurrencyInfo>
                            {
                                new BaseCurrencyInfo { Date = null, Currency = "CNY" },
                            },
                    });

            TitleManager.TitleInfos =
                new MockConfigManager<TitleInfos>(new TitleInfos { Titles = new List<TitleInfo>() });

            ClientUser.Set("b1");
        }

        protected abstract IEntitySerializer Serializer { get; }

        public virtual void SimpleVoucherTest()
        {
            Assert.NotNull(Serializer.PresentVoucher(null));
            Assert.Throws<FormatException>(() => Serializer.ParseVoucher(""));
            Assert.Throws<FormatException>(() => Serializer.ParseVoucher("new Voucher {"));
        }

        public virtual void SimpleAssetAmortTest()
        {
            Assert.NotNull(Serializer.PresentAsset(null));
            Assert.Throws<FormatException>(() => Serializer.ParseAsset(""));
            Assert.Throws<FormatException>(() => Serializer.ParseAsset("new Asset {"));
            
            Assert.NotNull(Serializer.PresentAmort(null));
            Assert.Throws<FormatException>(() => Serializer.ParseAmort(""));
            Assert.Throws<FormatException>(() => Serializer.ParseAmort("new Amortization {"));
        }

        public virtual void VoucherTest(string dt, VoucherType type)
        {
            var voucher1 = VoucherDataProvider.Create(dt, type);
            var voucher2 = Serializer.ParseVoucher(Serializer.PresentVoucher(voucher1));
            voucher2.Type = voucher2.Type ?? VoucherType.Ordinary;
            Assert.Equal(voucher1, voucher2, new VoucherEqualityComparer());
        }

        public virtual void AssetTest(string dt, DepreciationMethod type)
        {
            var asset1 = AssetDataProvider.Create(dt, type);
            var asset2 = Serializer.ParseAsset(Serializer.PresentAsset(asset1));
            Assert.Equal(asset1, asset2, new AssetEqualityComparer());
        }

        public virtual void AmortTest(string dt, AmortizeInterval type)
        {
            var amort1 = AmortDataProvider.Create(dt, type);
            var amort2 = Serializer.ParseAmort(Serializer.PresentAmort(amort1));
            Assert.Equal(amort1, amort2, new AmortEqualityComparer());
        }
    }

    [Collection("SerializerTestCollection")]
    public class CSharpSerializerTest : SerializerTest
    {
        protected override IEntitySerializer Serializer { get; } = new CSharpSerializer();

        [Theory]
        [ClassData(typeof(VoucherDataProvider))]
        public override void VoucherTest(string dt, VoucherType type) => base.VoucherTest(dt, type);

        [Theory]
        [ClassData(typeof(AssetDataProvider))]
        public override void AssetTest(string dt, DepreciationMethod type) => base.AssetTest(dt, type);

        [Theory]
        [ClassData(typeof(AmortDataProvider))]
        public override void AmortTest(string dt, AmortizeInterval type) => base.AmortTest(dt, type);

        [Fact]
        public override void SimpleVoucherTest() => base.SimpleVoucherTest();

        [Fact]
        public override void SimpleAssetAmortTest() => base.SimpleAssetAmortTest();
    }

    [Collection("SerializerTestCollection")]
    public class ExprSerializerTest : SerializerTest
    {
        protected override IEntitySerializer Serializer { get; } = new ExprSerializer();

        [Theory]
        [ClassData(typeof(VoucherDataProvider))]
        public override void VoucherTest(string dt, VoucherType type) => base.VoucherTest(dt, type);

        [Fact]
        public void OtherTest()
            => Assert.Null(Serializer.ParseVoucher(@"new Voucher { T1001 null }").Details.Single().Fund);

        [Fact]
        public override void SimpleVoucherTest() => base.SimpleVoucherTest();

        [Fact]
        public void NotImplementedTest()
        {
            Assert.Throws<NotImplementedException>(() => Serializer.PresentAsset(null));
            Assert.Throws<NotImplementedException>(() => Serializer.ParseAsset(null));
            Assert.Throws<NotImplementedException>(() => Serializer.PresentAmort(null));
            Assert.Throws<NotImplementedException>(() => Serializer.ParseAmort(null));
        }
    }

    [Collection("SerializerTestCollection")]
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
                                            Editable = false,
                                        },
                                    new Abbreviation { Abbr = "abbr2", Title = 1234, Editable = true },
                                },
                        });

        protected override IEntitySerializer Serializer => new AbbrSerializer();

        [Theory]
        [ClassData(typeof(VoucherDataProvider))]
        public override void VoucherTest(string dt, VoucherType type) => base.VoucherTest(dt, type);

        [Fact]
        public void OtherTest()
        {
            var voucher = Serializer.ParseVoucher(@"new Voucher { abbr1 123 abbr2 ""gg"" 765 }");
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
                                        Fund = 123,
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "CNY",
                                        Title = 1234,
                                        Content = "gg",
                                        Fund = 765,
                                    },
                            },
                    },
                voucher,
                new VoucherEqualityComparer());
        }

        [Fact]
        public override void SimpleVoucherTest() => base.SimpleVoucherTest();

        [Fact]
        public void NotImplementedTest()
        {
            Assert.Throws<NotImplementedException>(() => Serializer.PresentAsset(null));
            Assert.Throws<NotImplementedException>(() => Serializer.ParseAsset(null));
            Assert.Throws<NotImplementedException>(() => Serializer.PresentAmort(null));
            Assert.Throws<NotImplementedException>(() => Serializer.ParseAmort(null));
        }
    }

    [Collection("SerializerTestCollection")]
    public class JsonSerializerTest : SerializerTest
    {
        protected override IEntitySerializer Serializer { get; } = new JsonSerializer();

        [Theory]
        [ClassData(typeof(VoucherDataProvider))]
        public override void VoucherTest(string dt, VoucherType type) => base.VoucherTest(dt, type);

        [Theory]
        [ClassData(typeof(AssetDataProvider))]
        public override void AssetTest(string dt, DepreciationMethod type) => base.AssetTest(dt, type);

        [Theory]
        [ClassData(typeof(AmortDataProvider))]
        public override void AmortTest(string dt, AmortizeInterval type) => base.AmortTest(dt, type);

        [Fact]
        public override void SimpleVoucherTest() => base.SimpleVoucherTest();

        [Fact]
        public override void SimpleAssetAmortTest() => base.SimpleAssetAmortTest();
    }

    [Collection("SerializerTestCollection")]
    public class DiscountSerializerTest
    {
        public DiscountSerializerTest()
        {
            BaseCurrency.BaseCurrencyInfos = new MockConfigManager<BaseCurrencyInfos>(
                new BaseCurrencyInfos
                    {
                        Infos = new List<BaseCurrencyInfo>
                            {
                                new BaseCurrencyInfo { Date = null, Currency = "CNY" },
                            },
                    });

            TitleManager.TitleInfos =
                new MockConfigManager<TitleInfos>(new TitleInfos { Titles = new List<TitleInfo>() });

            AbbrSerializer.Abbrs =
                new MockConfigManager<Abbreviations>(
                    new Abbreviations
                        {
                            Abbrs = new List<Abbreviation>
                                {
                                    new Abbreviation { Abbr = "aaa", Title = 1001, Editable = false },
                                },
                        });

            ClientUser.Set("b1");
        }

        private static IEntitySerializer Serializer { get; } = new DiscountSerializer();

        [Fact]
        public void ManyNullTest()
        {
            Assert.Throws<ApplicationException>(
                () => Serializer.ParseVoucher(
                    @"new Voucher {
! @usd
aaa : 1 ;
tnull
dnull

@usd T1001 null
}"));
            Assert.Throws<ApplicationException>(
                () => Serializer.ParseVoucher(
                    @"new Voucher {
! @usd
aaa : 1 ;
tnull
dnull

@usd T1001 -1
}"));
            Assert.Throws<ApplicationException>(
                () => Serializer.ParseVoucher(
                    @"new Voucher {
! @usd
aaa : 1 ;
dnull

@usd T1001 null
}"));
            Assert.Throws<ApplicationException>(
                () => Serializer.ParseVoucher(
                    @"new Voucher {
! @usd
aaa : 1 ;
tnull

@usd T1001 null
}"));
        }

        [Fact]
        public void MultUserDiscountTest()
        {
            var voucher = Serializer.ParseVoucher(
                @"new Voucher {
! @usd
Upn T1001 + ! T1002 : 1000-50 950+50 ;
! Upn aaa + T100366 hei ha : 1000 ;
t12
d48

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
                                        User = "b1", Currency = "USD", Title = 1001, Fund = -29120,
                                    },
                                new VoucherDetail
                                    {
                                        User = "pn", Currency = "USD", Title = 1001, Fund = 1000 + 500 + 6 - 8,
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1", Currency = "USD", Title = 1002, Fund = 950 + 4 - 16,
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1003,
                                        SubTitle = 66,
                                        Content = "hei",
                                        Remark = "ha",
                                        Fund = 500 + 2,
                                    },
                                new VoucherDetail
                                    {
                                        User = "pn", Currency = "USD", Title = 6603, Fund = -50 - 16,
                                    },
                                new VoucherDetail { User = "b1", Currency = "USD", Title = 6603, Fund = -8 },
                            },
                    },
                voucher,
                new VoucherEqualityComparer());
        }

        [Fact]
        public void SimpleTest()
        {
            Assert.Throws<FormatException>(() => Serializer.ParseVoucher(""));
            Assert.Throws<NotImplementedException>(() => Serializer.ParseVoucher("new Voucher {"));
            Assert.Throws<NotImplementedException>(() => Serializer.ParseVoucher("new Voucher { }"));
            Assert.Throws<NotImplementedException>(() => Serializer.ParseVoucherDetail("whatever"));
        }

        [Fact]
        public void TaxDiscountNullTest()
        {
            var voucher = Serializer.ParseVoucher(
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
                                new VoucherDetail { User = "b1", Currency = "USD", Title = 1001, Fund = -2864 },
                                new VoucherDetail
                                    {
                                        User = "b1", Currency = "USD", Title = 1001, Fund = 1000 + 500 + 6 - 8,
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1", Currency = "USD", Title = 1002, Fund = 950 + 4 - 16,
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1003,
                                        SubTitle = 66,
                                        Content = "hei",
                                        Remark = "ha",
                                        Fund = 500 + 2,
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1", Currency = "USD", Title = 6603, Fund = -50 - 16 - 8,
                                    },
                            },
                    },
                voucher,
                new VoucherEqualityComparer());
        }

        [Fact]
        public void TaxDiscountTest()
        {
            var voucher = Serializer.ParseVoucher(
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
                                        User = "b1", Currency = "USD", Title = 1001, Fund = -29120,
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1", Currency = "USD", Title = 1001, Fund = 1000 + 500 + 6 - 8,
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1", Currency = "USD", Title = 1002, Fund = 950 + 4 - 16,
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1003,
                                        SubTitle = 66,
                                        Content = "hei",
                                        Remark = "ha",
                                        Fund = 500 + 2,
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1", Currency = "USD", Title = 6603, Fund = -50 - 16 - 8,
                                    },
                            },
                    },
                voucher,
                new VoucherEqualityComparer());
        }

        [Fact]
        public void TaxNullDiscountTest()
        {
            var voucher = Serializer.ParseVoucher(
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
                                new VoucherDetail { User = "b1", Currency = "USD", Title = 1001, Fund = -2864 },
                                new VoucherDetail
                                    {
                                        User = "b1", Currency = "USD", Title = 1001, Fund = 1000 + 500 + 6 - 8,
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1", Currency = "USD", Title = 1002, Fund = 950 + 4 - 16,
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1003,
                                        SubTitle = 66,
                                        Content = "hei",
                                        Remark = "ha",
                                        Fund = 500 + 2,
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1", Currency = "USD", Title = 6603, Fund = -50 - 16 - 8,
                                    },
                            },
                    },
                voucher,
                new VoucherEqualityComparer());
        }

        [Fact]
        public void TaxTest()
        {
            var voucher = Serializer.ParseVoucher(
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
                                new VoucherDetail { User = "b1", Currency = "CNY", Title = 1001, Fund = -2912 },
                                new VoucherDetail
                                    {
                                        User = "b1", Currency = "USD", Title = 1001, Fund = 1000 + 500 + 6,
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1", Currency = "USD", Title = 1002, Fund = 950 + 4,
                                    },
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "USD",
                                        Title = 1003,
                                        SubTitle = 66,
                                        Fund = 500 + 2,
                                    },
                                new VoucherDetail { User = "b1", Currency = "USD", Title = 6603, Fund = -50 },
                            },
                    },
                voucher,
                new VoucherEqualityComparer());
        }

        [Fact]
        public void NotImplementedTest()
        {
            Assert.Throws<NotImplementedException>(() => Serializer.PresentVoucher(null));
            Assert.Throws<NotImplementedException>(() => Serializer.PresentVoucherDetail((VoucherDetail)null));
            // ReSharper disable once RedundantCast
            Assert.Throws<NotImplementedException>(() => Serializer.PresentVoucherDetail((VoucherDetailR)null));
            
            Assert.Throws<NotImplementedException>(() => Serializer.PresentAsset(null));
            Assert.Throws<NotImplementedException>(() => Serializer.ParseAsset(null));
            Assert.Throws<NotImplementedException>(() => Serializer.PresentAmort(null));
            Assert.Throws<NotImplementedException>(() => Serializer.ParseAmort(null));
        }
    }
    
    [Collection("SerializerTestCollection")]
    public class CsvDefaultSerializerTest : SerializerTest
    {
        protected override IEntitySerializer Serializer { get; } = new CsvSerializer("");

        [Fact]
        public void OtherTest()
        {
            var r = Serializer.PresentVoucher(new Voucher
                {
                    ID = "hhh",
                    Date = ClientDateTime.Today,
                    Type = VoucherType.Carry,
                    Details = new List<VoucherDetail>
                        {
                            new VoucherDetail
                                {
                                    User = "b1",
                                    Currency = "CNY",
                                    Title = 1234,
                                    SubTitle = 04,
                                    Content = "cnt",
                                    Remark = "rmk",
                                    Fund = 123,
                                },
                        },
                });
            
            Assert.Contains("hhh", r);
            Assert.DoesNotContain("Carry", r);
            Assert.Contains("b1", r);
            Assert.Contains("CNY", r);
            Assert.Contains("1234", r);
            Assert.Contains("04", r);
            Assert.Contains("cnt", r);
            Assert.Contains("rmk", r);
            Assert.Contains("123", r);
        }

        [Fact]
        public void NotImplementedTest()
        {
            Assert.Throws<NotImplementedException>(() => Serializer.ParseVoucher(null));
            Assert.Throws<NotImplementedException>(() => Serializer.ParseVoucherDetail(null));
            Assert.Throws<NotImplementedException>(() => Serializer.PresentAsset(null));
            Assert.Throws<NotImplementedException>(() => Serializer.ParseAsset(null));
            Assert.Throws<NotImplementedException>(() => Serializer.PresentAmort(null));
            Assert.Throws<NotImplementedException>(() => Serializer.ParseAmort(null));
        }
    }
    
    [Collection("SerializerTestCollection")]
    public class CsvCustomSerializerTest : SerializerTest
    {
        protected override IEntitySerializer Serializer { get; } = new CsvSerializer("type d C v r c s t id U s' t'");

        [Fact]
        public void OtherTest()
        {
            var r = Serializer.PresentVoucher(new Voucher
                {
                    ID = "hhh",
                    Date = ClientDateTime.Today,
                    Type = VoucherType.Carry,
                    Details = new List<VoucherDetail>
                        {
                            new VoucherDetail
                                {
                                    User = "b1",
                                    Currency = "CNY",
                                    Title = 1234,
                                    SubTitle = 04,
                                    Content = "cnt",
                                    Remark = "rmk",
                                    Fund = 123,
                                },
                        },
                });
            
            Assert.Contains("hhh", r);
            Assert.Contains("Carry", r);
            Assert.Contains("b1", r);
            Assert.Contains("CNY", r);
            Assert.Contains("1234", r);
            Assert.Contains("04", r);
            Assert.Contains("cnt", r);
            Assert.Contains("rmk", r);
            Assert.Contains("123", r);
        }

        [Fact]
        public void NotImplementedTest()
        {
            Assert.Throws<NotImplementedException>(() => Serializer.ParseVoucher(null));
            Assert.Throws<NotImplementedException>(() => Serializer.ParseVoucherDetail(null));
            Assert.Throws<NotImplementedException>(() => Serializer.PresentAsset(null));
            Assert.Throws<NotImplementedException>(() => Serializer.ParseAsset(null));
            Assert.Throws<NotImplementedException>(() => Serializer.PresentAmort(null));
            Assert.Throws<NotImplementedException>(() => Serializer.ParseAmort(null));
        }
    }
}
