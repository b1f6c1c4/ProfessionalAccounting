using System;
using AccountingServer.Entities;

namespace AccountingServer.BLL.Parsing
{
    internal partial class SubtotalParser
    {
        public partial class RangeDayContext : IDateRange
        {
            /// <inheritdoc />
            public DateFilter Range
            {
                get
                {
                    var dt = (DateTime)this;
                    return new DateFilter(dt, dt);
                }
            }

            public static implicit operator DateTime(RangeDayContext context)
                => context.RangeDeltaDay() != null
                    ? ClientDateTime.Today.AddDays(1 - context.RangeDeltaDay().GetText().Length)
                    : ClientDateTime.ParseExact(context.RangeADay().GetText(), "yyyyMMdd");
        }

        public partial class RangeWeekContext : IDateRange
        {
            /// <inheritdoc />
            public DateFilter Range
            {
                get
                {
                    var delta = 1 - RangeDeltaWeek().GetText().Length;
                    var dt = ClientDateTime.Today;
                    dt = dt.AddDays(dt.DayOfWeek == DayOfWeek.Sunday ? -6 : 1 - (int)dt.DayOfWeek);
                    dt = dt.AddDays(delta * 7);
                    return new DateFilter(dt, dt.AddDays(6));
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
                    DateTime dt;
                    if (RangeDeltaMonth() != null)
                    {
                        var delta = int.Parse(RangeDeltaMonth().GetText().TrimStart('-'));
                        dt = new DateTime(
                            ClientDateTime.Today.Year,
                            ClientDateTime.Today.Month,
                            1,
                            0,
                            0,
                            0,
                            DateTimeKind.Utc);
                        dt = dt.AddMonths(-delta);
                    }
                    else
                        dt = ClientDateTime.ParseExact(RangeAMonth().GetText() + "01", "yyyyMMdd");

                    return new DateFilter(dt, dt.AddMonths(1).AddDays(-1));
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
                    var year = int.Parse(RangeAYear().GetText());
                    return new DateFilter(
                        new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(year, 12, 31, 0, 0, 0, DateTimeKind.Utc));
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

                    throw new MemberAccessException("表达式错误");
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
                        return DateFilter.TheNotNull;
                    if (Certain != null)
                        return Certain.Range;

                    DateTime? s = null, e = null;
                    if (Begin != null)
                        s = Begin.Range.StartDate;
                    if (End != null)
                        e = End.Range.EndDate;
                    var f = new DateFilter(s, e);
                    if (Tilde().GetText() == "~~")
                        f.Nullable ^= true;
                    return f;
                }
            }
        }
    }
}
