using System;
using System.Globalization;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    internal partial class AccountingConsole
    {
        /// <summary>
        ///     解析检索表达式
        /// </summary>
        /// <param name="s">检索表达式</param>
        /// <param name="dateQuery">日期表达式</param>
        /// <returns>过滤器</returns>
        private static VoucherDetail ParseQuery(string s, out string dateQuery)
        {
            var detail = new VoucherDetail();
            s = s.Trim();
            string dateQ;
            if (s.StartsWith("T"))
            {
                var id1 = s.IndexOf("'", StringComparison.Ordinal);
                if (id1 > 0)
                {
                    var id2 = s.LastIndexOf("'", StringComparison.Ordinal);
                    detail.Content = s.Substring(id1 + 1, id2 - id1 - 1);
                    dateQ = s.Substring(id2 + 1);
                }
                else
                {
                    id1 = s.IndexOf("[", StringComparison.Ordinal);
                    dateQ = s.Substring(id1);
                }
                detail.Title = Convert.ToInt32(s.Substring(1, 4));

                if (id1 > 4)
                {
                    int val;
                    if (Int32.TryParse(s.Substring(5, Math.Min(2, id1 - 4)), out val))
                        detail.SubTitle = val;
                }
                else
                    detail.SubTitle = null;
            }
            else if (s.StartsWith("'"))
            {
                var id2 = s.LastIndexOf("'", StringComparison.Ordinal);
                detail.Content = s.Substring(1, id2 - 1);
                dateQ = s.Substring(id2 + 1);
            }
            else
                dateQ = s;

            dateQuery = dateQ;
            return detail;
        }

        /// <summary>
        ///     解析日期表达式
        /// </summary>
        /// <param name="sOrig">日期表达式</param>
        /// <param name="startDate">开始日期，<c>null</c>表示不限最小日期并且包含无日期</param>
        /// <param name="endDate">截止日期，<c>null</c>表示不限最大日期</param>
        /// <param name="nullable"><paramref name="startDate" />和<paramref name="endDate" />均为<c>null</c>时，表示是否只包含无日期</param>
        /// <returns>是否是合法的检索表达式</returns>
        private static bool ParseVoucherQuery(string sOrig, out DateTime? startDate, out DateTime? endDate,
                                              out bool nullable)
        {
            nullable = false;

            sOrig = sOrig.Trim(' ');

            if (sOrig == "[]")
            {
                startDate = null;
                endDate = null;
                return true;
            }

            var s = sOrig.Trim('[', ']');

            if (s == ".")
            {
                startDate = DateTime.Now.Date;
                endDate = startDate;
                return true;
            }

            if (s == "..")
            {
                var date = DateTime.Now.Date;
                var dayOfWeek = (int)date.DayOfWeek;
                if (dayOfWeek == 0) // Sunday
                    dayOfWeek = 7;
                startDate = date.AddDays(1 - dayOfWeek);
                endDate = startDate.Value.AddDays(6);
                return true;
            }

            if (s == "0")
            {
                var date = DateTime.Now.Date;
                startDate = date.Day <= 19 ? date.AddMonths(-1).AddDays(20 - date.Day) : date.AddDays(19 - date.Day);
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
                return true;
            }

            if (s == "-1")
            {
                var date = DateTime.Now.Date.AddMonths(-1);
                startDate = date.Day <= 19 ? date.AddMonths(-1).AddDays(20 - date.Day) : date.AddDays(19 - date.Day);
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
                return true;
            }

            if (s == "@0")
            {
                var date = DateTime.Now.Date;
                startDate = date.AddDays(1 - date.Day);
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
                return true;
            }

            if (s == "@-1")
            {
                var date = DateTime.Now.Date.AddMonths(-1);
                startDate = date.AddDays(1 - date.Day);
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
                return true;
            }

            DateTime dt;
            if (DateTime.TryParseExact(s, "yyyyMMdd", null, DateTimeStyles.AssumeLocal, out dt))
            {
                startDate = dt;
                endDate = startDate;
                return true;
            }

            if (DateTime.TryParseExact(s + "19", "yyyyMMdd", null, DateTimeStyles.AssumeLocal, out dt))
            {
                startDate = dt.AddMonths(-1).AddDays(1);
                endDate = dt;
                return true;
            }

            if (DateTime.TryParseExact(s + "01", "@yyyyMMdd", null, DateTimeStyles.AssumeLocal, out dt))
            {
                startDate = dt;
                endDate = dt.AddMonths(1).AddDays(-1);
                return true;
            }

            var sp = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (sp.Length == 2)
            {
                DateTime dt2;
                if (DateTime.TryParseExact(sp[0], "yyyyMMdd", null, DateTimeStyles.AssumeLocal, out dt)
                    &&
                    DateTime.TryParseExact(sp[1], "yyyyMMdd", null, DateTimeStyles.AssumeLocal, out dt2))
                {
                    startDate = dt;
                    endDate = dt2;
                    return true;
                }
            }
            else if (sp.Length == 1)
            {
                if (DateTime.TryParseExact(sp[0], "yyyyMMdd", null, DateTimeStyles.AssumeLocal, out dt))
                {
                    if (sOrig.TrimStart('[')[0] != ' ')
                    {
                        startDate = dt;
                        endDate = null;
                    }
                    else
                    {
                        startDate = null;
                        endDate = dt;
                    }
                    return true;
                }
                if (sp[0] == "null")
                {
                    startDate = null;
                    endDate = null;
                    nullable = true;
                    return true;
                }
            }

            startDate = null;
            endDate = null;
            return false;
        }
    }
}
