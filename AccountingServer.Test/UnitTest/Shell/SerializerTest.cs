/* Copyright (C) 2020-2024 b1f6c1c4
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
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using Xunit;

namespace AccountingServer.Test.UnitTest.Shell;

[Collection("SerializerTestCollection")]
public abstract class SerializerTest
{
    protected readonly Client Client = new() { User = "b1", Today = DateTime.UtcNow.Date };

    protected SerializerTest()
    {
        Cfg.Assign(new BaseCurrencyInfos { Infos = new() { new() { Date = null, Currency = "CNY" } } });

        Cfg.Assign(new TitleInfos { Titles = new() });
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
        voucher2.Type ??= VoucherType.Ordinary;
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
    public override void SimpleAssetAmortTest() => base.SimpleAssetAmortTest();

    [Fact]
    public override void SimpleVoucherTest() => base.SimpleVoucherTest();
}

[Collection("SerializerTestCollection")]
public class ExprSerializerTest : SerializerTest
{
    public ExprSerializerTest() => Serializer = new ExprSerializer { Client = Client };

    protected override IEntitySerializer Serializer { get; }

    [Theory]
    [ClassData(typeof(VoucherDataProvider))]
    public override void VoucherTest(string dt, VoucherType type) => base.VoucherTest(dt, type);

    [Fact]
    public void NotImplementedTest()
    {
        Assert.Throws<NotImplementedException>(() => Serializer.PresentAsset(null));
        Assert.Throws<NotImplementedException>(() => Serializer.ParseAsset(null));
        Assert.Throws<NotImplementedException>(() => Serializer.PresentAmort(null));
        Assert.Throws<NotImplementedException>(() => Serializer.ParseAmort(null));
    }

    [Fact]
    public void OtherTest()
        => Assert.Null(Serializer.ParseVoucher(@"new Voucher { T1001 null }").Details.Single().Fund);

    [Fact]
    public override void SimpleVoucherTest() => base.SimpleVoucherTest();
}

[Collection("SerializerTestCollection")]
public class AbbrSerializerTest : SerializerTest
{
    public AbbrSerializerTest()
    {
        Serializer = new AbbrSerializer { Client = Client };
        Cfg.Assign(new Abbreviations
            {
                Abbrs = new()
                    {
                        new()
                            {
                                Abbr = "abbr1",
                                Title = 1234,
                                SubTitle = 04,
                                Content = "cnt",
                                Editable = false,
                            },
                        new() { Abbr = "abbr2", Title = 1234, Editable = true },
                    },
            });
    }

    protected override IEntitySerializer Serializer { get; }

    [Theory]
    [ClassData(typeof(VoucherDataProvider))]
    public override void VoucherTest(string dt, VoucherType type) => base.VoucherTest(dt, type);

    [Fact]
    public void NotImplementedTest()
    {
        Assert.Throws<NotImplementedException>(() => Serializer.PresentAsset(null));
        Assert.Throws<NotImplementedException>(() => Serializer.ParseAsset(null));
        Assert.Throws<NotImplementedException>(() => Serializer.PresentAmort(null));
        Assert.Throws<NotImplementedException>(() => Serializer.ParseAmort(null));
    }

    [Fact]
    public void OtherTest()
    {
        var voucher = Serializer.ParseVoucher(@"new Voucher { abbr1 123 abbr2 ""gg"" 765 }");
        Assert.Equal(
            new()
                {
                    Date = Client.Today,
                    Details = new()
                        {
                            new()
                                {
                                    User = "b1",
                                    Currency = "CNY",
                                    Title = 1234,
                                    SubTitle = 04,
                                    Content = "cnt",
                                    Remark = null,
                                    Fund = 123,
                                },
                            new()
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
    public override void SimpleAssetAmortTest() => base.SimpleAssetAmortTest();

    [Fact]
    public override void SimpleVoucherTest() => base.SimpleVoucherTest();
}

[Collection("SerializerTestCollection")]
public class DiscountSerializerTest
{
    private readonly Client m_Client = new() { User = "b1", Today = DateTime.UtcNow.Date };

    public DiscountSerializerTest()
    {
        Serializer = new DiscountSerializer { Client = m_Client };

        Cfg.Assign(new BaseCurrencyInfos { Infos = new() { new() { Date = null, Currency = "CNY" } } });

        Cfg.Assign(new TitleInfos { Titles = new() });

        Cfg.Assign(new Abbreviations { Abbrs = new() { new() { Abbr = "aaa", Title = 1001, Editable = false } } });
    }

    private IEntitySerializer Serializer { get; }

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
            new()
                {
                    Date = m_Client.Today,
                    Type = VoucherType.Ordinary,
                    Details = new()
                        {
                            new()
                                {
                                    User = "b1", Currency = "USD", Title = 1001, Fund = -29120,
                                },
                            new()
                                {
                                    User = "pn", Currency = "USD", Title = 1001, Fund = 1000 + 500 + 6 - 8,
                                },
                            new()
                                {
                                    User = "b1", Currency = "USD", Title = 1002, Fund = 950 + 4 - 16,
                                },
                            new()
                                {
                                    User = "b1",
                                    Currency = "USD",
                                    Title = 1003,
                                    SubTitle = 66,
                                    Content = "hei",
                                    Remark = "ha",
                                    Fund = 500 + 2,
                                },
                            new()
                                {
                                    User = "pn", Currency = "USD", Title = 6603, Fund = -50 - 16,
                                },
                            new() { User = "b1", Currency = "USD", Title = 6603, Fund = -8 },
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
! aaa + T100366 hei ha : 100*10 ;
t2
t10
dnull

@usd T1001 -2864
}");
        Assert.Equal(
            new()
                {
                    Date = m_Client.Today,
                    Type = VoucherType.Ordinary,
                    Details = new()
                        {
                            new() { User = "b1", Currency = "USD", Title = 1001, Fund = -2864 },
                            new()
                                {
                                    User = "b1", Currency = "USD", Title = 1001, Fund = 1000 + 500 + 6 - 8,
                                },
                            new()
                                {
                                    User = "b1", Currency = "USD", Title = 1002, Fund = 950 + 4 - 16,
                                },
                            new()
                                {
                                    User = "b1",
                                    Currency = "USD",
                                    Title = 1003,
                                    SubTitle = 66,
                                    Content = "hei",
                                    Remark = "ha",
                                    Fund = 500 + 2,
                                },
                            new()
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
T1001 + ! T1002 : 1000-.1-49.9 9500+500*.1 ;
! aaa + T100366 hei ha : 1000 ;
t2
d40
t10
d8

@usd T1001 -29120
}");
        Assert.Equal(
            new()
                {
                    Date = m_Client.Today,
                    Type = VoucherType.Ordinary,
                    Details = new()
                        {
                            new()
                                {
                                    User = "b1", Currency = "USD", Title = 1001, Fund = -29120,
                                },
                            new()
                                {
                                    User = "b1", Currency = "USD", Title = 1001, Fund = 1000 + 500 + 6 - 8,
                                },
                            new()
                                {
                                    User = "b1", Currency = "USD", Title = 1002, Fund = 950 + 4 - 16,
                                },
                            new()
                                {
                                    User = "b1",
                                    Currency = "USD",
                                    Title = 1003,
                                    SubTitle = 66,
                                    Content = "hei",
                                    Remark = "ha",
                                    Fund = 500 + 2,
                                },
                            new()
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
            new()
                {
                    Date = m_Client.Today,
                    Type = VoucherType.Ordinary,
                    Details = new()
                        {
                            new() { User = "b1", Currency = "USD", Title = 1001, Fund = -2864 },
                            new()
                                {
                                    User = "b1", Currency = "USD", Title = 1001, Fund = 1000 + 500 + 6 - 8,
                                },
                            new()
                                {
                                    User = "b1", Currency = "USD", Title = 1002, Fund = 950 + 4 - 16,
                                },
                            new()
                                {
                                    User = "b1",
                                    Currency = "USD",
                                    Title = 1003,
                                    SubTitle = 66,
                                    Content = "hei",
                                    Remark = "ha",
                                    Fund = 500 + 2,
                                },
                            new()
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
            new()
                {
                    Date = new(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Type = VoucherType.Ordinary,
                    Details = new()
                        {
                            new() { User = "b1", Currency = "CNY", Title = 1001, Fund = -2912 },
                            new()
                                {
                                    User = "b1", Currency = "USD", Title = 1001, Fund = 1000 + 500 + 6,
                                },
                            new()
                                {
                                    User = "b1", Currency = "USD", Title = 1002, Fund = 950 + 4,
                                },
                            new()
                                {
                                    User = "b1",
                                    Currency = "USD",
                                    Title = 1003,
                                    SubTitle = 66,
                                    Fund = 500 + 2,
                                },
                            new() { User = "b1", Currency = "USD", Title = 6603, Fund = -50 },
                        },
                },
            voucher,
            new VoucherEqualityComparer());
    }
}

[Collection("SerializerTestCollection")]
public class CsvDefaultSerializerTest : SerializerTest
{
    protected override IEntitySerializer Serializer { get; } = new CsvSerializer("");

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

    [Fact]
    public void OtherTest()
    {
        var r = Serializer.PresentVoucher(new()
            {
                ID = "hhh",
                Date = Client.Today,
                Type = VoucherType.Carry,
                Details = new()
                    {
                        new()
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
}

[Collection("SerializerTestCollection")]
public class CsvCustomSerializerTest : SerializerTest
{
    protected override IEntitySerializer Serializer { get; } = new CsvSerializer("type d C v r c s t id U s' t'");

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

    [Fact]
    public void OtherTest()
    {
        var r = Serializer.PresentVoucher(new()
            {
                ID = "hhh",
                Date = Client.Today,
                Type = VoucherType.Carry,
                Details = new()
                    {
                        new()
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
}
