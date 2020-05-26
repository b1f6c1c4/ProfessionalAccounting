using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;

namespace AccountingServer.BLL.Parsing
{
    internal partial class SubtotalParser
    {
        public partial class SubtotalContext : ISubtotal
        {
            /// <inheritdoc />
            public GatheringType GatherType
            {
                get
                {
                    var text = Mark.Text;
                    switch (text)
                    {
                        case "``":
                        case "`":
                            return GatheringType.Sum;
                        case "!":
                            return GatheringType.Count;
                        case "!!":
                            return GatheringType.VoucherCount;
                        default:
                            throw new MemberAccessException("表达式错误");
                    }
                }
            }

            /// <inheritdoc />
            public IReadOnlyList<SubtotalLevel> Levels
            {
                get
                {
                    var z = Mark.Text == "`" ? SubtotalLevel.NonZero : SubtotalLevel.None;
                    if (subtotalFields() == null)
                        if (subtotalEqui() == null)
                            return new[]
                                {
                                    z,
                                    z | SubtotalLevel.Currency,
                                    z | SubtotalLevel.Title,
                                    z | SubtotalLevel.SubTitle,
                                    z | SubtotalLevel.User,
                                    z | SubtotalLevel.Content,
                                };
                        else
                            return new[]
                                {
                                    z,
                                    z | SubtotalLevel.Title,
                                    z | SubtotalLevel.SubTitle,
                                    z | SubtotalLevel.User,
                                    z | SubtotalLevel.Content,
                                };

                    if (subtotalFields().SubtotalNoField() != null)
                        return new[] { z };

                    return new[] { z }.Concat(subtotalFields().subtotalField()
                            .Select(
                                f =>
                                    {
                                        var ch = f.SubtotalField().GetText()[0];
                                        var zz = z | (f.SubtotalFieldZ() == null
                                            ? SubtotalLevel.None
                                            : SubtotalLevel.NonZero);
                                        switch (ch)
                                        {
                                            case 't':
                                                return zz | SubtotalLevel.Title;
                                            case 's':
                                                return zz | SubtotalLevel.SubTitle;
                                            case 'c':
                                                return zz | SubtotalLevel.Content;
                                            case 'r':
                                                return zz | SubtotalLevel.Remark;
                                            case 'C':
                                                return zz | SubtotalLevel.Currency;
                                            case 'U':
                                                return zz | SubtotalLevel.User;
                                            case 'd':
                                                return zz | SubtotalLevel.Day;
                                            case 'w':
                                                return zz | SubtotalLevel.Week;
                                            case 'm':
                                                return zz | SubtotalLevel.Month;
                                            case 'y':
                                                return zz | SubtotalLevel.Year;
                                        }

                                        throw new MemberAccessException("表达式错误");
                                    }))
                        .ToList();
                }
            }

            /// <inheritdoc />
            public AggregationType AggrType
            {
                get
                {
                    if (subtotalAggr() == null)
                        return AggregationType.None;
                    if (subtotalAggr().AllDate() == null &&
                        subtotalAggr().rangeCore() == null)
                        return AggregationType.ChangedDay;

                    return AggregationType.EveryDay;
                }
            }

            /// <inheritdoc />
            public SubtotalLevel AggrInterval
            {
                get
                {
                    if (subtotalAggr() == null)
                        return SubtotalLevel.None;

                    switch (subtotalAggr().AggrMark().GetText())
                    {
                        case "D":
                            return SubtotalLevel.Day;
                        case "W":
                            return SubtotalLevel.Week;
                        case "M":
                            return SubtotalLevel.Month;
                        case "Y":
                            return SubtotalLevel.Year;
                        default:
                            throw new MemberAccessException("表达式错误");
                    }
                }
            }

            /// <inheritdoc />
            public IDateRange EveryDayRange
            {
                get
                {
                    if (subtotalAggr().AllDate() != null)
                        return DateFilter.Unconstrained;

                    return subtotalAggr().rangeCore();
                }
            }

            /// <inheritdoc />
            public string EquivalentCurrency =>
                subtotalEqui() == null
                    ? null
                    : subtotalEqui().VoucherCurrency()?.GetText().ParseCurrency() ?? BaseCurrency.Now;

            /// <inheritdoc />
            public DateTime? EquivalentDate
            {
                get
                {
                    if (subtotalEqui() == null)
                        return null;

                    return subtotalEqui().rangeDay() ?? ClientDateTime.Today;
                }
            }
        }
    }
}
