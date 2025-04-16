/* Copyright (C) 2020-2025 b1f6c1c4
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
using AccountingServer.Entities;

namespace AccountingServer.BLL.Parsing;

internal partial class SubtotalParser
{
    public partial class RangeDayContext : IClientDependable, IDateRange
    {
        public Client Client { private get; set; }

        /// <inheritdoc />
        public DateFilter Range
        {
            get
            {
                var dt = AsDate();
                return new(dt, dt);
            }
        }

        public DateTime? AsDate() =>
            RangeDeltaDay() != null
                ? Client.Today.AddDays(1 - RangeDeltaDay().GetText().Length)
                : DateTimeParser.ParseExact(RangeADay().GetText(), "yyyyMMdd");
    }

    public partial class RangeWeekContext : IClientDependable, IDateRange
    {
        public Client Client { private get; set; }

        /// <inheritdoc />
        public DateFilter Range
        {
            get
            {
                var delta = 1 - RangeDeltaWeek().GetText().Length;
                var dt = Client.Today;
                dt = dt.AddDays(dt.DayOfWeek == DayOfWeek.Sunday ? -6 : 1 - (int)dt.DayOfWeek);
                dt = dt.AddDays(delta * 7);
                return new(dt, dt.AddDays(6));
            }
        }
    }

    public partial class RangeMonthContext : IClientDependable, IDateRange
    {
        public Client Client { private get; set; }

        /// <inheritdoc />
        public DateFilter Range
        {
            get
            {
                DateTime dt;
                if (RangeDeltaMonth() != null)
                {
                    var delta = int.Parse(RangeDeltaMonth().GetText().TrimStart('-'));
                    dt = new(
                        Client.Today.Year,
                        Client.Today.Month,
                        1,
                        0,
                        0,
                        0,
                        DateTimeKind.Utc);
                    dt = dt.AddMonths(-delta);
                }
                else
                    dt = DateTimeParser.ParseExact($"{RangeAMonth().GetText()}01", "yyyyMMdd");

                return new(dt, dt.AddMonths(1).AddDays(-1));
            }
        }
    }

    public partial class RangeQuarterContext : IClientDependable, IDateRange
    {
        public Client Client { private get; set; }

        /// <inheritdoc />
        public DateFilter Range
        {
            get
            {
                DateTime dt;
                if (RangeDeltaQuarter() != null)
                {
                    var delta = int.Parse(RangeDeltaQuarter().GetText()[1..]);
                    var q = (Client.Today.Month + 2) / 3;
                    if (delta > 0)
                        q = delta;
                    else
                        q += delta;
                    dt = new(Client.Today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    dt = dt.AddMonths(3 * q - 3);
                }
                else
                {
                    var year = int.Parse(RangeAQuarter().GetText()[0..4]);
                    var q = int.Parse(RangeAQuarter().GetText()[5..]);
                    dt = new(year, 3 * q - 2, 1, 0, 0, 0, DateTimeKind.Utc);
                }

                return new(dt, dt.AddMonths(3).AddDays(-1));
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
                return new(
                    new(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new(year, 12, 31, 0, 0, 0, DateTimeKind.Utc));
            }
        }
    }

    public partial class RangeCertainPointContext : IClientDependable, IDateRange
    {
        public Client Client { private get; set; }

        /// <inheritdoc />
        public DateFilter Range
        {
            get
            {
                if (rangeDay() != null)
                    return rangeDay().Assign(Client).Range;
                if (rangeWeek() != null)
                    return rangeWeek().Assign(Client).Range;
                if (rangeMonth() != null)
                    return rangeMonth().Assign(Client).Range;
                if (rangeQuarter() != null)
                    return rangeQuarter().Assign(Client).Range;
                if (rangeYear() != null)
                    return rangeYear().Range;

                throw new MemberAccessException("表达式错误");
            }
        }
    }

    public partial class RangeCoreContext : IClientDependable, IDateRange
    {
        public Client Client { private get; set; }

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
                    return Certain.Assign(Client).Range;

                DateTime? s = null, e = null;
                if (Begin != null)
                    s = Begin.Assign(Client).Range.StartDate;
                if (End != null)
                    e = End.Assign(Client).Range.EndDate;

                var f = new DateFilter(s, e);
                if (Tilde().GetText() == "~~")
                    f.Nullable ^= true;
                return f;
            }
        }
    }
}
