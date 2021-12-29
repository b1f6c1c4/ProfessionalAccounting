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
using AccountingServer.Entities;

namespace AccountingServer.BLL.Parsing;

internal partial class QueryParser
{
    public partial class RangeDayContext : IClientDependable, IDate, IDateRange
    {
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
                ? Client.ClientDateTime.Today.AddDays(1 - RangeDeltaDay().GetText().Length)
                : ClientDateTime.ParseExact(RangeADay().GetText(), "yyyyMMdd");

        public Client Client { private get; set; }
    }

    public partial class RangeWeekContext : IClientDependable, IDateRange
    {
        /// <inheritdoc />
        public DateFilter Range
        {
            get
            {
                var delta = 1 - RangeDeltaWeek().GetText().Length;
                var dt = Client.ClientDateTime.Today;
                dt = dt.AddDays(dt.DayOfWeek == DayOfWeek.Sunday ? -6 : 1 - (int)dt.DayOfWeek);
                dt = dt.AddDays(delta * 7);
                return new(dt, dt.AddDays(6));
            }
        }

        public Client Client { private get; set; }
    }

    public partial class RangeMonthContext : IClientDependable, IDateRange
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
                    dt = new(
                        Client.ClientDateTime.Today.Year,
                        Client.ClientDateTime.Today.Month,
                        1,
                        0,
                        0,
                        0,
                        DateTimeKind.Utc);
                    dt = dt.AddMonths(-delta);
                }
                else
                    dt = ClientDateTime.ParseExact(RangeAMonth().GetText() + "01", "yyyyMMdd");

                return new(dt, dt.AddMonths(1).AddDays(-1));
            }
        }

        public Client Client { private get; set; }
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
        /// <inheritdoc />
        public DateFilter Range
        {
            get
            {
                if (rangeDay() != null)
                {
                    rangeDay().Client = Client;
                    return rangeDay().Range;
                }

                if (rangeWeek() != null)
                {
                    rangeWeek().Client = Client;
                    return rangeWeek().Range;
                }

                if (rangeMonth() != null)
                {
                    rangeMonth().Client = Client;
                    return rangeMonth().Range;
                }

                if (rangeYear() != null)
                    return rangeYear().Range;

                throw new MemberAccessException("表达式错误");
            }
        }

        public Client Client { private get; set; }
    }

    public partial class RangePointContext : IDateRange
    {
        /// <inheritdoc />
        public DateFilter Range
        {
            get
            {
                if (AllDate() != null)
                    return DateFilter.Unconstrained;
                if (RangeNull() != null)
                    return DateFilter.TheNullOnly;

                return rangeCertainPoint().Range;
            }
        }
    }

    public partial class UniqueTimeCoreContext : IClientDependable, IDate
    {
        public DateTime? AsDate()
        {
            if (RangeNull() != null)
                return null;

            rangeDay().Client = Client;
            return rangeDay().AsDate();
        }

        public Client Client { private get; set; }
    }

    public partial class RangeCoreContext : IClientDependable, IDateRange
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
                {
                    Certain.Client = Client;
                    return Certain.Range;
                }

                DateTime? s = null, e = null;
                if (Begin != null)
                {
                    Begin.Client = Client;
                    s = Begin.Range.StartDate;
                }

                if (End != null)
                {
                    End.Client = Client;
                    e = End.Range.EndDate;
                }

                var f = new DateFilter(s, e);
                if (Tilde().GetText() == "~~")
                    f.Nullable ^= true;
                return f;
            }
        }

        public Client Client { private get; set; }
    }

    public partial class UniqueTimeContext : IClientDependable, IDate
    {
        public DateTime? AsDate()
        {
            Core.Client = Client;
            return Core.AsDate();
        }

        public Client Client { private get; set; }
    }

    public partial class RangeContext : IClientDependable, IDateRange
    {
        /// <inheritdoc />
        public DateFilter Range
        {
            get
            {
                if (rangeCore() == null)
                    return DateFilter.Unconstrained;

                rangeCore().Client = Client;
                return rangeCore().Range;
            }
        }

        public Client Client { private get; set; }
    }
}
