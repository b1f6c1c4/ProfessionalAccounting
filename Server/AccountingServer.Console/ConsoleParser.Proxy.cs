using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class ConsoleParser
    {
        public partial class DetailQueryContext : IDetailQueryAtom
        {
            public VoucherDetail Filter
            {
                get
                {
                    var filter = new VoucherDetail();
                    if (DetailTitle() != null)
                    {
                        var t = Int32.Parse(DetailTitle().GetText().TrimStart('T'));
                        filter.Title = t / 100;
                        filter.SubTitle = t % 100;
                    }
                    else if (DetailTitleSubTitle() != null)
                    {
                        var t = Int32.Parse(DetailTitleSubTitle().GetText().TrimStart('T'));
                        filter.Title = t / 100;
                        filter.SubTitle = t % 100;
                    }
                    if (DoubleQuotedString() != null)
                    {
                        var s = DoubleQuotedString().GetText();
                        s = s.Substring(1, s.Length - 2);
                        filter.Content = s.Replace("\"\"", "\"");
                    }
                    if (SingleQuotedString() != null)
                    {
                        var s = SingleQuotedString().GetText();
                        s = s.Substring(1, s.Length - 2);
                        filter.Remark = s.Replace("''", "'");
                    }
                    return filter;
                }
            }

            public int Dir
            {
                get
                {
                    if (Direction.Text == "d")
                        return 1;
                    if (Direction.Text == "c")
                        return -1;
                    return 0;
                }
            }
        }

        public partial class DetailAtomContext : IDetailQueryCompounded { }

        public partial class DetailsXContext : IDetailQueryBinary
        {
            public BinaryOperatorType Operator { get { return BinaryOperatorType.Interect; } }
            public IDetailQueryCompounded Filter1 { get { return detailAtom(0); } }
            public IDetailQueryCompounded Filter2 { get { return detailAtom(1); } }
        }

        public partial class DetailsContext : IDetailQueryBinary
        {
            public BinaryOperatorType Operator
            {
                get
                {
                    if (Op.Text == "+")
                        return BinaryOperatorType.Union;
                    if (Op.Text == "-")
                        return BinaryOperatorType.Substract;
                    throw new InvalidOperationException();
                }
            }

            public IDetailQueryCompounded Filter1 { get { return detailsX(0); } }
            public IDetailQueryCompounded Filter2 { get { return detailsX(1); } }
        }

        public partial class DetailUnaryContext : IDetailQueryUnary
        {
            public UnaryOperatorType Operator { get { return UnaryOperatorType.Complement; } }
            public IDetailQueryCompounded Filter1 { get { return detailQuery(); } }
        }

        public partial class SubtotalContext : ISubtotal
        {
            public bool NonZero { get { return SubtotalMark.Text.Length < 2; } }

            public bool All { get { return SubtotalMark.Text.Contains("!"); } }

            public IList<SubtotalLevel> Levels
            {
                get
                {
                    return SubtotalFields.Text.Select(
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
                                                              case 'd':
                                                                  return SubtotalLevel.Day;
                                                              case 'w':
                                                                  return SubtotalLevel.Week;
                                                              case 'm':
                                                                  return SubtotalLevel.Month;
                                                              case 'f':
                                                                  return SubtotalLevel.FinancialMonth;
                                                              case 'b':
                                                                  return SubtotalLevel.BillingMonth;
                                                              case 'y':
                                                                  return SubtotalLevel.Year;
                                                          }
                                                          throw new InvalidOperationException();
                                                      }).ToList();
                }
            }

            public AggregationType AggrType
            {
                get
                {
                    switch (AggregationMethod.Text)
                    {
                        case "X":
                            return AggregationType.ExtendedEveryDay;
                        case "x":
                            return AggregationType.EveryDay;
                        case "D":
                            return AggregationType.ChangedDay;
                    }
                    throw new InvalidOperationException();
                }
            }
        }

        public partial class RangeDayContext : IDateRange
        {
            public DateFilter Range
            {
                get
                {
                    var dt = RangeDeltaDay() != null
                                 ? DateTime.Now.Date.AddDays(1 - RangeDeltaDay().GetText().Length)
                                 : DateTime.ParseExact(RangeADay().GetText(), "yyyyMMdd", null);
                    return new DateFilter(dt, dt);
                }
            }
        }

        public partial class RangeWeekContext : IDateRange
        {
            public DateFilter Range
            {
                get
                {
                    var delta = 1 - RangeDeltaWeek().GetText().Length;
                    var dt = DateTime.Now.Date;
                    dt = dt.AddDays(dt.DayOfWeek == DayOfWeek.Sunday ? -6 : 1 - (int)dt.DayOfWeek);
                    dt = dt.AddDays(delta * 7);
                    return new DateFilter(dt, dt);
                }
            }
        }

        public partial class RangeMonthContext : IDateRange
        {
            public DateFilter Range
            {
                get
                {
                    if (Modifier == null)
                    {
                        DateTime dt;
                        if (RangeDeltaMonth() != null)
                        {
                            var delta = Int32.Parse(RangeDeltaMonth().GetText().TrimStart('-'));
                            dt = new DateTime(
                                DateTime.Now.Year,
                                DateTime.Now.Day >= 20 ? DateTime.Now.Month + 1 : DateTime.Now.Month,
                                19);
                            dt = dt.AddMonths(-delta);
                        }
                        else
                            dt = DateTime.ParseExact(RangeAMonth().GetText() + "19", "yyyyMMdd", null);
                        return new DateFilter(dt.AddMonths(-1).AddDays(1), dt);
                    }
                    if (Modifier.Text == "@")
                    {
                        DateTime dt;
                        if (RangeDeltaMonth() != null)
                        {
                            var delta = Int32.Parse(RangeDeltaMonth().GetText().TrimStart('-'));
                            dt = new DateTime(DateTime.Now.Date.Year, DateTime.Now.Month, 1);
                            dt = dt.AddMonths(-delta);
                        }
                        else
                            dt = DateTime.ParseExact(RangeAMonth().GetText() + "01", "yyyyMMdd", null);
                        return new DateFilter(dt, dt.AddMonths(1).AddDays(-1));
                    }
                    if (Modifier.Text == "#")
                    {
                        DateTime dt;
                        if (RangeDeltaMonth() != null)
                        {
                            var delta = Int32.Parse(RangeDeltaMonth().GetText().TrimStart('-'));
                            dt = new DateTime(
                                DateTime.Now.Year,
                                DateTime.Now.Day >= 9 ? DateTime.Now.Month + 1 : DateTime.Now.Month,
                                19);
                            dt = dt.AddMonths(-delta);
                        }
                        else
                            dt = DateTime.ParseExact(RangeAMonth().GetText() + "08", "yyyyMMdd", null);
                        return new DateFilter(dt.AddMonths(-1).AddDays(1), dt);
                    }
                    throw new InvalidOperationException();
                }
            }
        }

        public partial class RangeYearContext : IDateRange
        {
            public DateFilter Range
            {
                get
                {
                    var year = Int32.Parse(RangeAYear().GetText());
                    return new DateFilter(new DateTime(year, 1, 1), new DateTime(year, 12, 31));
                }
            }
        }

        public partial class RangeCertainPointContext : IDateRange
        {
            public DateFilter Range
            {
                get
                {
                    if (rangeDay() != null)
                        return rangeDay().Range;
                    if (rangeWeek() != null)
                        return rangeWeek().Range;
                    if (rangeMonth() != null)
                        return rangeMonth().Range;
                    if (rangeYear() != null)
                        return rangeYear().Range;
                    throw new InvalidOperationException();
                }
            }
        }

        public partial class RangePointContext : IDateRange
        {
            public DateFilter Range
            {
                get
                {
                    if (All != null)
                        return DateFilter.Unconstrained;
                    if (RangeNull() != null)
                        return DateFilter.TheNullOnly;
                    return rangeCertainPoint().Range;
                }
            }
        }

        public partial class RangeCoreContext : IDateRange
        {
            public DateFilter Range
            {
                get
                {
                    if (RangeNull() != null)
                        return DateFilter.TheNullOnly;
                    if (RangeAllNotNull() != null)
                        return new DateFilter { Nullable = false, NullOnly = false };
                    if (Certain != null)
                        return Certain.Range;

                    DateTime? s = null, e = null;
                    if (Begin != null)
                        s = Begin.Range.StartDate;
                    if (End != null)
                        e = End.Range.EndDate;
                    if (!s.HasValue &&
                        Op != null &&
                        Op.Text == "~")
                        return new DateFilter(s, e) { Nullable = false };
                    return new DateFilter(s, e);
                }
            }
        }

        public partial class RangeContext : IDateRange
        {
            public DateFilter Range
            {
                get { return rangeCore() == null ? DateFilter.Unconstrained : rangeCore().Range; }
            }
        }

        public partial class VoucherQueryContext : IVoucherQuery
        {
            public Voucher VoucherFilter
            {
                get
                {
                    var vfilter = new Voucher();
                    if (VoucherRemark() != null)
                    {
                        var s = VoucherRemark().GetText();
                        s = s.Substring(1, s.Length - 2);
                        vfilter.Remark = s.Replace("%%", "%");
                    }
                    return vfilter;
                }
            }

            public IDetailQueryCompounded DetailFilter { get { return details(); } }

            public DateFilter Range { get { return range() == null ? DateFilter.Unconstrained : range().Range; } }
        }

        public partial class GroupedQueryContext : IGroupingQuery
        {
            public Voucher VoucherFilter { get { return voucherQuery().VoucherFilter; } }
            public IDetailQueryCompounded DetailFilter { get { return voucherQuery().DetailFilter; } }
            public DateFilter Range { get { return voucherQuery().Range; } }

            public bool NonZero { get { return subtotal().NonZero; } }
            public bool All { get { return subtotal().All; } }
            public IList<SubtotalLevel> Levels { get { return subtotal().Levels; } }
            public AggregationType AggrType { get { return subtotal().AggrType; } }
        }
    }
}
