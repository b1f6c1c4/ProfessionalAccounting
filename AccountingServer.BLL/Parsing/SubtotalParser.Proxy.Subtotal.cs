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
                    if (subtotalFields()?.SubtotalNoField != null)
                        return new SubtotalLevel[0];

                    var text = SubtotalFields()?.GetText();
                    if (text == null)
                        if (subtotalEqui() == null)
                            return new[]
                                {
                                    z | SubtotalLevel.Currency,
                                    z | SubtotalLevel.Title,
                                    z | SubtotalLevel.SubTitle,
                                    z | SubtotalLevel.User,
                                    z | SubtotalLevel.Content,
                                };
                        else
                            return new[]
                                {
                                    z | SubtotalLevel.Title,
                                    z | SubtotalLevel.SubTitle,
                                    z | SubtotalLevel.User,
                                    z | SubtotalLevel.Content,
                                };

                    return text
                        .Select(
                            ch =>
                                {
                                    switch (ch)
                                    {
                                        case 't':
                                            return z | SubtotalLevel.Title;
                                        case 's':
                                            return z | SubtotalLevel.SubTitle;
                                        case 'c':
                                            return z | SubtotalLevel.Content;
                                        case 'r':
                                            return z | SubtotalLevel.Remark;
                                        case 'C':
                                            return z | SubtotalLevel.Currency;
                                        case 'U':
                                            return z | SubtotalLevel.User;
                                        case 'd':
                                            return z | SubtotalLevel.Day;
                                        case 'w':
                                            return z | SubtotalLevel.Week;
                                        case 'm':
                                            return z | SubtotalLevel.Month;
                                        case 'y':
                                            return z | SubtotalLevel.Year;
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
