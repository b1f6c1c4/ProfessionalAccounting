using System;
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
            AbbrSerializer.Abbrs =
                new MockConfigManager<Abbreviations>(new Abbreviations { Abbrs = new List<Abbreviation>() });

            TitleManager.TitleInfos =
                new MockConfigManager<TitleInfos>(new TitleInfos { Titles = new List<TitleInfo>() });
        }

        protected abstract IEntitySerializer GetSerializer();

        public virtual void SimpleTest()
        {
            var serializer = GetSerializer();

            Assert.Throws(typeof(FormatException), () => serializer.ParseVoucher(""));
            Assert.Throws(typeof(FormatException), () => serializer.ParseVoucher("new Voucher {"));
        }

        [InlineData(null, VoucherType.Ordinary)]
        [InlineData("2017-01-01", VoucherType.Uncertain)]
        [InlineData(null, VoucherType.Carry)]
        [InlineData(null, VoucherType.AnnualCarry)]
        [InlineData("2017-01-01", VoucherType.Depreciation)]
        [InlineData(null, VoucherType.Devalue)]
        [InlineData("2017-01-01", VoucherType.Amortization)]
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
                                    Title = 1001,
                                    Content = "%@!@#$%^&*(\nas\rdf\\",
                                    Fund = 123.45
                                },
                            new VoucherDetail
                                {
                                    Currency = "USD",
                                    Title = 1002,
                                    SubTitle = 12,
                                    Fund = -123.45,
                                    Remark = "\\qw\ter%@!@#$%^&*(%"
                                },
                            new VoucherDetail
                                {
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
    }

    public class CSharpSerializerTest : SerializerTest
    {
        protected override IEntitySerializer GetSerializer() => new CSharpSerializer();

        [Fact]
        public override void SimpleTest() { base.SimpleTest(); }

        [Theory]
        public override void VoucherTest(string dt, VoucherType type) { base.VoucherTest(dt, type); }
    }

    public class ExprSerializerTest : SerializerTest
    {
        protected override IEntitySerializer GetSerializer() => new ExprSerializer();

        [Fact]
        public override void SimpleTest() { base.SimpleTest(); }

        [Theory]
        public override void VoucherTest(string dt, VoucherType type) { base.VoucherTest(dt, type); }

        [Fact]
        public void OtherTest()
        {
            var serializer = GetSerializer();

            Assert.Equal(null, serializer.ParseVoucher(@"new Voucher { T1001 null }").Details.Single().Fund);
        }
    }

    public class AbbrSerializerTest : SerializerTest
    {
        protected override IEntitySerializer GetSerializer() => new AbbrSerializer();

        [Fact]
        public override void SimpleTest() { base.SimpleTest(); }

        [Theory]
        public override void VoucherTest(string dt, VoucherType type) { base.VoucherTest(dt, type); }
    }
}
