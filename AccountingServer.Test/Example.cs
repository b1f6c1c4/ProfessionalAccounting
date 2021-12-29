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
using System.Collections;
using System.Collections.Generic;
using AccountingServer.Entities;

namespace AccountingServer.Test;

internal class VoucherDataProvider : IEnumerable<object[]>
{
    private static readonly List<object[]> Data = new()
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

    public static Voucher Create(string dt, VoucherType type) => new()
        {
            Date = dt.ToDateTime(),
            Type = type,
            Remark = " t 't\"-.\" %@!@#$%^&*( ",
            Details = new()
                {
                    new()
                        {
                            User = "b1",
                            Currency = "JPY",
                            Title = 1001,
                            Content = "%@!@#$%^&*(\nas\rdf\\",
                            Fund = 123.45,
                        },
                    new()
                        {
                            User = "b1",
                            Currency = "USD",
                            Title = 1002,
                            SubTitle = 12,
                            Fund = -123.45,
                            Remark = "\\qw\ter%@!@#$%^&*(%",
                        },
                    new()
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
    private static readonly List<object[]> Data = new()
        {
            new object[] { "2017-01-01", DepreciationMethod.None },
            new object[] { null, DepreciationMethod.StraightLine },
            new object[] { "2017-01-01", DepreciationMethod.DoubleDeclineMethod },
            new object[] { null, DepreciationMethod.SumOfTheYear },
        };

    public IEnumerator<object[]> GetEnumerator() => Data.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Data.GetEnumerator();

    public static Asset Create(string dt, DepreciationMethod type) => new()
        {
            ID = Guid.Parse("9F4FDBA8-94BD-45C2-AF53-36BCFFB591FF"),
            Name = "nm",
            User = "b1",
            Currency = "MXN",
            Date = dt.ToDateTime(),
            Method = type,
            Remark = " t 't\"-.\" %@!@#$%^&*( ",
            Value = 5553,
            Salvage = 213,
            Life = 11,
            Title = 1234,
            DepreciationTitle = 2344,
            DevaluationTitle = 4634,
            DepreciationExpenseTitle = 6622,
            DepreciationExpenseSubTitle = 05,
            DevaluationExpenseTitle = 6623,
            DevaluationExpenseSubTitle = 06,
            Schedule = new()
                {
                    new AcquisitionItem
                        {
                            Date = "2017-01-01".ToDateTime(),
                            Value = 5553,
                            OrigValue = 5553,
                            Remark = "\\\t@#$%^&*(%",
                        },
                    new DepreciateItem
                        {
                            Date = "2017-02-28".ToDateTime(),
                            Value = 2141,
                            Amount = 3412,
                            Remark = "\\qw\ter%@!@#$%^&*(%",
                        },
                    new DevalueItem
                        {
                            Date = "2017-02-28".ToDateTime(),
                            Value = 2140,
                            FairValue = 2140,
                            Amount = 1,
                            Remark = "  7')^ Q23'4",
                        },
                    new DispositionItem { Date = "2017-03-10".ToDateTime(), Value = 0, Remark = "\\\t@#$%^&*(%" },
                },
        };
}

internal class AmortDataProvider : IEnumerable<object[]>
{
    private static readonly List<object[]> Data = new()
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

    public static Amortization Create(string dt, AmortizeInterval type) => new()
        {
            ID = Guid.Parse("9F4FDBA8-94BD-45C2-AF53-36BCFFB591FF"),
            Name = "nm",
            User = "b1",
            Date = dt.ToDateTime(),
            Interval = type,
            TotalDays = 1233,
            Remark = " t 't\"-.\" %@!@#$%^&*( ",
            Value = 33344,
            Template = new()
                {
                    Date = dt.ToDateTime(),
                    Remark = " t 't\"-.\" %@!@#$%^&*( ",
                    Type = VoucherType.AnnualCarry,
                    Details = new()
                        {
                            new()
                                {
                                    User = "b1",
                                    Currency = "EUR",
                                    Title = 5555,
                                    Fund = -5,
                                    Remark = "  7' 46r0*\" &)%\" *%)^ Q23'4",
                                },
                        },
                },
            Schedule = new()
                {
                    new()
                        {
                            Date = "2001-02-03".ToDateTime(),
                            Amount = 123,
                            Value = 33344 - 123,
                            Remark = "\\\t@#$%^&*(%",
                        },
                    new()
                        {
                            Date = "2011-03-04".ToDateTime(),
                            Amount = 974,
                            Value = 33344 - 123 - 974,
                            Remark = "*(%",
                        },
                },
        };
}
