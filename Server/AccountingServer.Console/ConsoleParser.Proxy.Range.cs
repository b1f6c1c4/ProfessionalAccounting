using System;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    internal static class NullDateRangeHelper
    {
        public static DateFilter TheRange(this IDateRange range)
        {
            if (range == null)
                return DateFilter.Unconstrained;
            return range.Range;
        }
    }

    public partial class ConsoleParser
    {
        public partial class RangeDayContext : IDateRange
        {
            /// <inheritdoc />
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
            /// <inheritdoc />
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
            /// <inheritdoc />
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
            /// <inheritdoc />
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
            /// <inheritdoc />
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
            /// <inheritdoc />
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
            /// <inheritdoc />
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
                    var f = new DateFilter(s, e);
                    if (Op.Text == "=")
                        f.Nullable ^= true;
                    return f;
                }
            }
        }

        public partial class RangeContext : IDateRange
        {
            /// <inheritdoc />
            public DateFilter Range { get { return rangeCore().TheRange(); } }
        }
    }
}
