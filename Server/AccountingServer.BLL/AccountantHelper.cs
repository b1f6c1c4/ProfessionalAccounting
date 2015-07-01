using System;

namespace AccountingServer.BLL
{
    public static class AccountantHelper
    {
        /// <summary>
        ///     判断金额相等的误差
        /// </summary>
        private const double Tolerance = 1e-8;

        /// <summary>
        ///     判断是否为零
        /// </summary>
        /// <param name="value">值</param>
        /// <returns>是否为零</returns>
        public static bool IsZero(this double value) { return Math.Abs(value) < Tolerance; }

        /// <summary>
        ///     判断是否为非负
        /// </summary>
        /// <param name="value">值</param>
        /// <returns>是否非负</returns>
        public static bool IsNonNegative(this double value) { return value > -Tolerance; }

        /// <summary>
        ///     判断是否为非正
        /// </summary>
        /// <param name="value">值</param>
        /// <returns>是否非正</returns>
        public static bool IsNonPositive(this double value) { return value < Tolerance; }

        /// <summary>
        ///     获取指定月的最后一天
        /// </summary>
        /// <param name="year">年</param>
        /// <param name="month">月</param>
        /// <returns>此月最后一天</returns>
        public static DateTime LastDayOfMonth(int year, int month)
        {
            while (month > 12)
            {
                month -= 12;
                year++;
            }
            while (month < 1)
            {
                month += 12;
                year--;
            }
            return new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
        }
    }
}
