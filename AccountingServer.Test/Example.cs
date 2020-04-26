using System;
using System.Collections;
using System.Collections.Generic;
using AccountingServer.Entities;

namespace AccountingServer.Test
{
    internal class VoucherDataProvider : IEnumerable<object[]>
    {
        private static readonly List<object[]> Data = new List<object[]>
            {
                new object[] { null, VoucherType.Ordinary },
                new object[] { "2017-01-01", VoucherType.Uncertain },
                new object[] { null, VoucherType.Carry },
                new object[] { null, VoucherType.AnnualCarry },
                new object[] { "2017-01-01", VoucherType.Depreciation },
                new object[] { null, VoucherType.Devalue },
                new object[] { "2017-01-01", VoucherType.Amortization },
            };

        public IEnumerator<object[]> GetEnumerator() => Data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Data.GetEnumerator();

        public static Voucher Create(string dt, VoucherType type) => new Voucher
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
                                Fund = 123.45,
                            },
                        new VoucherDetail
                            {
                                User = "b1",
                                Currency = "USD",
                                Title = 1002,
                                SubTitle = 12,
                                Fund = -123.45,
                                Remark = "\\qw\ter%@!@#$%^&*(%",
                            },
                        new VoucherDetail
                            {
                                User = "b1",
                                Currency = "EUR",
                                Title = 5555,
                                Fund = -5,
                                Remark = "  7' 46r0*\" &)%\" *%)^ Q23'4",
                            },
                    },
            };
    }

    internal class AssetDataProvider : IEnumerable<object[]>
    {
        private static readonly List<object[]> Data = new List<object[]>
            {
                new object[] { "2017-01-01", DepreciationMethod.None },
                new object[] { null, DepreciationMethod.StraightLine },
                new object[] { "2017-01-01", DepreciationMethod.DoubleDeclineMethod },
                new object[] { null, DepreciationMethod.SumOfTheYear },
            };

        public IEnumerator<object[]> GetEnumerator() => Data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Data.GetEnumerator();

        public static Asset Create(string dt, DepreciationMethod type) => new Asset
            {
                ID = Guid.Parse("9F4FDBA8-94BD-45C2-AF53-36BCFFB591FF"),
                Name = "nm",
                User = "b1",
                Currency = "MXN",
                Date = dt.ToDateTime(),
                Method = type,
                Remark = " t 't\"-.\" %@!@#$%^&*( ",
                Value = 33344,
                Salvage = 213,
                Life = 11,
                Title = 1234,
                DepreciationTitle = 2344,
                DevaluationTitle = 4634,
                DepreciationExpenseTitle = 6622,
                DepreciationExpenseSubTitle = 05,
                DevaluationExpenseTitle = 6623,
                DevaluationExpenseSubTitle = 06,
                Schedule = new List<AssetItem>
                    {
                        new AcquisitionItem { Value = 123, OrigValue = 553, Remark = "\\\t@#$%^&*(%", },
                        new DepreciateItem { Value = -23, Amount = 3412, Remark = "\\qw\ter%@!@#$%^&*(%", },
                        new DevalueItem { Value = -50, Amount = 2342, Remark = "  7' 46r0*\" &)%\" *%)^ Q23'4", },
                        new DispositionItem { Value = -50, Remark = "\\\t@#$%^&*(%", },
                    },
            };
    }

    internal class AmortDataProvider : IEnumerable<object[]>
    {
        private static readonly List<object[]> Data = new List<object[]>
            {
                new object[] { null, AmortizeInterval.EveryDay },
                new object[] { "2017-01-01", AmortizeInterval.LastDayOfMonth },
                new object[] { null, AmortizeInterval.LastDayOfWeek },
                new object[] { null, AmortizeInterval.LastDayOfYear },
                new object[] { "2017-01-01", AmortizeInterval.SameDayOfMonth },
                new object[] { null, AmortizeInterval.SameDayOfWeek },
                new object[] { "2017-01-01", AmortizeInterval.SameDayOfYear },
            };

        public IEnumerator<object[]> GetEnumerator() => Data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Data.GetEnumerator();

        public static Amortization Create(string dt, AmortizeInterval type) => new Amortization
            {
                ID = Guid.Parse("9F4FDBA8-94BD-45C2-AF53-36BCFFB591FF"),
                Name = "nm",
                User = "b1",
                Date = dt.ToDateTime(),
                Interval = type,
                TotalDays = 1233,
                Remark = " t 't\"-.\" %@!@#$%^&*( ",
                Value = 33344,
                Template = new Voucher
                    {
                        Date = dt.ToDateTime(),
                        Remark = " t 't\"-.\" %@!@#$%^&*( ",
                        Type = VoucherType.AnnualCarry,
                        Details = new List<VoucherDetail>
                            {
                                new VoucherDetail
                                    {
                                        User = "b1",
                                        Currency = "EUR",
                                        Title = 5555,
                                        Fund = -5,
                                        Remark = "  7' 46r0*\" &)%\" *%)^ Q23'4",
                                    },
                            },
                    },
                Schedule = new List<AmortItem>
                    {
                        new AmortItem { Amount = 123, Value = 666, Remark = "\\\t@#$%^&*(%", },
                        new AmortItem { Amount = 974, Value = 2342, Remark = "*(%", },
                    },
            };
    }
}
