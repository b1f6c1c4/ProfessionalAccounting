using System;
using System.Collections.Generic;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using Xunit;

namespace AccountingServer.Test.UnitTest.Shell
{
    public class SerializerTest
    {
        public SerializerTest()
        {
            AbbrSerializer.Abbrs =
                new MockConfigManager<Abbreviations>(new Abbreviations { Abbrs = new List<Abbreviation>() });

            TitleManager.TitleInfos =
                new MockConfigManager<TitleInfos>(new TitleInfos { Titles = new List<TitleInfo>() });
        }

        [Theory]
        [InlineData(typeof(CSharpSerializer))]
        [InlineData(typeof(ExprSerializer))]
        [InlineData(typeof(AbbrSerializer))]
        public void VoucherTest(Type type)
        {
            var serializer = (IEntitySerializer)Activator.CreateInstance(type);

            var voucher1 = new Voucher
                {
                    Type = VoucherType.Ordinary,
                    Remark = " t 't\"-.\" %@!@#$%^&*( ",
                    Details = new List<VoucherDetail>
                        {
                            new VoucherDetail
                                {
                                    Currency = VoucherDetail.BaseCurrency,
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
                                }
                        }
                };

            var voucher2 = serializer.ParseVoucher(serializer.PresentVoucher(voucher1));

            Assert.Equal(voucher1, voucher2, new VoucherEqualityComparer());
        }
    }
}
