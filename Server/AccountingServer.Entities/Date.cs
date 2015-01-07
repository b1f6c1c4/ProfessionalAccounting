using System;

namespace AccountingServer.Entities
{
    /// <summary>
    ///     日期过滤器
    /// </summary>
    public struct DateFilter
    {
        /// <summary>
        ///     是否只允许无日期（若为<c>true</c>，则无须考虑<c>Nullable</c>）
        /// </summary>
        public bool NullOnly;

        /// <summary>
        ///     是否允许无日期
        /// </summary>
        public bool Nullable;

        /// <summary>
        ///     开始日期（含）
        /// </summary>
        public DateTime? StartDate;

        /// <summary>
        ///     截止日期（含）
        /// </summary>
        public DateTime? EndDate;

        public static DateFilter Unconstrained = new DateFilter
                                                     {
                                                         NullOnly = false,
                                                         Nullable = true,
                                                         StartDate = null,
                                                         EndDate = null
                                                     };

        public static DateFilter TheNullOnly = new DateFilter
                                                   {
                                                       NullOnly = true,
                                                       Nullable = true,
                                                       StartDate = null,
                                                       EndDate = null
                                                   };

        public DateFilter(DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue &&
                endDate.HasValue)
            {
                NullOnly = false;
                Nullable = false;
                StartDate = startDate;
                EndDate = endDate;
                return;
            }

            if (startDate.HasValue)
            {
                NullOnly = false;
                Nullable = false;
                StartDate = startDate;
                EndDate = null;
                return;
            }

            if (endDate.HasValue)
            {
                NullOnly = false;
                Nullable = true;
                StartDate = null;
                EndDate = endDate;
                return;
            }

            {
                NullOnly = false;
                Nullable = true;
                StartDate = null;
                EndDate = null;
            }
        }

        public bool Constrained { get { return StartDate.HasValue && EndDate.HasValue; } }
    }

    public static class DateHelper
    {
        /// <summary>
        ///     比较两日期（可以为无日期）的早晚
        /// </summary>
        /// <param name="b1Date">第一个日期</param>
        /// <param name="b2Date">第二个日期</param>
        /// <returns>相等为0，第一个早为-1，第二个早为1（无日期按无穷长时间以前考虑）</returns>
        public static int CompareDate(DateTime? b1Date, DateTime? b2Date)
        {
            if (b1Date.HasValue &&
                b2Date.HasValue)
                return b1Date.Value.CompareTo(b2Date.Value);
            if (b1Date.HasValue)
                return 1;
            if (b2Date.HasValue)
                return -1;
            return 0;
        }

        public static bool Within(this DateTime? dt, DateFilter rng)
        {
            if (rng.NullOnly)
                return dt == null;

            if (!dt.HasValue)
                return rng.Nullable;

            if (rng.StartDate.HasValue)
                if (dt < rng.StartDate.Value)
                    return false;

            if (rng.EndDate.HasValue)
                if (dt > rng.EndDate.Value)
                    return false;

            return true;
        }
    }
}
