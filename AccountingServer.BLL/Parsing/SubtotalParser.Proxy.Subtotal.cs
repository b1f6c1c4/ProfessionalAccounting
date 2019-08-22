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
                            return GatheringType.Zero;
                        case "`":
                            return GatheringType.NonZero;
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
                    var text = SubtotalFields()?.GetText();
                    if (text == null)
                        if (subtotalEqui() == null)
                            return new[]
                                {
                                    SubtotalLevel.Currency,
                                    SubtotalLevel.Title,
                                    SubtotalLevel.SubTitle,
                                    SubtotalLevel.User,
                                    SubtotalLevel.Content
                                };
                        else
                            return new[]
                                {
                                    SubtotalLevel.Title,
                                    SubtotalLevel.SubTitle,
                                    SubtotalLevel.User,
                                    SubtotalLevel.Content
                                };

                    if (text == "v")
                        return new SubtotalLevel[0];

                    return text
                        .Select(
                            ch =>
                            {
                                switch (ch)
                                {
                                    case 't':
                                        return SubtotalLevel.Title;
                                    case 's':
                                        return SubtotalLevel.SubTitle;
                                    case 'c':
                                        return SubtotalLevel.Content;
                                    case 'r':
                                        return SubtotalLevel.Remark;
                                    case 'C':
                                        return SubtotalLevel.Currency;
                                    case 'U':
                                        return SubtotalLevel.User;
                                    case 'd':
                                        return SubtotalLevel.Day;
                                    case 'w':
                                        return SubtotalLevel.Week;
                                    case 'm':
                                        return SubtotalLevel.Month;
                                    case 'y':
                                        return SubtotalLevel.Year;
                                }

                                throw new MemberAccessException("表达式错误");
                            })
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
                subtotalEqui().VoucherCurrency()?.GetText().ParseCurrency();

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
