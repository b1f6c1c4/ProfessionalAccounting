using System;
using System.Globalization;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class AccountingConsole
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
            if (s.StartsWith("T", StringComparison.OrdinalIgnoreCase))
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
                    id1 = s.IndexOf(" ", StringComparison.Ordinal);
                    dateQ = id1 < 0 ? String.Empty : s.Substring(id1);
                }
                detail.Title = Convert.ToInt32(s.Substring(1, 4));

                if (s.Length >= 7)
                {
                    int val;
                    if (Int32.TryParse(s.Substring(5, 2), out val))
                        detail.SubTitle = val;
                }
            }
            else if (s.StartsWith("'"))
            {
                var id2 = s.LastIndexOf("'", StringComparison.Ordinal);
                detail.Content = s.Substring(1, id2 - 1);
                dateQ = s.Substring(id2 + 1);
            }
            else
                dateQ = s;

            dateQuery = dateQ.Trim();
            return detail;
        }

        /// <summary>
        ///     解析日期表达式
        /// </summary>
        /// <param name="sOrig">日期表达式</param>
        /// <param name="reverseMonthType">是否反转@的意义</param>
        /// <returns>日期过滤器</returns>
        private static DateFilter ParseDateQuery(string sOrig, bool reverseMonthType = false)
        {
            sOrig = sOrig.Trim(' ');
            var s = sOrig.Trim('[', ']', ' ');

            if (s == String.Empty)
                return DateFilter.Unconstrained;

            if (s == ".")
                return new DateFilter(DateTime.Now.Date, DateTime.Now.Date);

            if (s == "..")
            {
                var date = DateTime.Now.Date;
                var dayOfWeek = (int)date.DayOfWeek;
                if (dayOfWeek == 0) // Sunday
                    dayOfWeek = 7;
                var startDate = date.AddDays(1 - dayOfWeek);
                return new DateFilter(startDate, startDate.AddDays(6));
            }

            if (s == (reverseMonthType ? "@0" : "0"))
            {
                var date = DateTime.Now.Date;
                var startDate = date.Day <= 19 ? date.AddMonths(-1).AddDays(20 - date.Day) : date.AddDays(19 - date.Day);
                return new DateFilter(startDate, startDate.AddMonths(1).AddDays(-1));
            }

            if (s == (reverseMonthType ? "@-1" : "-1"))
            {
                var date = DateTime.Now.Date.AddMonths(-1);
                var startDate = date.Day <= 19 ? date.AddMonths(-1).AddDays(20 - date.Day) : date.AddDays(19 - date.Day);
                return new DateFilter(startDate, startDate.AddMonths(1).AddDays(-1));
            }

            if (s == (reverseMonthType ? "0" : "@0"))
            {
                var date = DateTime.Now.Date;
                var startDate = date.AddDays(1 - date.Day);
                return new DateFilter(startDate, startDate.AddMonths(1).AddDays(-1));
            }

            if (s == (reverseMonthType ? "-1" : "@-1"))
            {
                var date = DateTime.Now.Date.AddMonths(-1);
                var startDate = date.AddDays(1 - date.Day);
                return new DateFilter(startDate, startDate.AddMonths(1).AddDays(-1));
            }

            DateTime dt;

            if (sOrig.StartsWith("[") &&
                sOrig.EndsWith("]"))
            {
                var sp = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (sp.Length == 2)
                {
                    if (DateTime.TryParseExact(
                                               sp[0],
                                               "yyyyMMdd",
                                               null,
                                               DateTimeStyles.AssumeLocal,
                                               out dt)) { }
                    else if (DateTime.TryParseExact(
                                                    sp[0] + "20",
                                                    reverseMonthType ? "@yyyyMMdd" : "yyyyMMdd",
                                                    null,
                                                    DateTimeStyles.AssumeLocal,
                                                    out dt))
                        dt = dt.AddMonths(-1);
                    else if (DateTime.TryParseExact(
                                                    sp[0] + "01",
                                                    reverseMonthType ? "yyyyMMdd" : "@yyyyMMdd",
                                                    null,
                                                    DateTimeStyles.AssumeLocal,
                                                    out dt)) { }
                    else
                        throw new InvalidOperationException("日期表达式无效");


                    DateTime dt2;
                    if (DateTime.TryParseExact(
                                               sp[1],
                                               "yyyyMMdd",
                                               null,
                                               DateTimeStyles.AssumeLocal,
                                               out dt2)) { }
                    else if (DateTime.TryParseExact(
                                                    sp[1] + "19",
                                                    reverseMonthType ? "@yyyyMMdd" : "yyyyMMdd",
                                                    null,
                                                    DateTimeStyles.AssumeLocal,
                                                    out dt2)) { }
                    else if (DateTime.TryParseExact(
                                                    sp[1] + "01",
                                                    reverseMonthType ? "yyyyMMdd" : "@yyyyMMdd",
                                                    null,
                                                    DateTimeStyles.AssumeLocal,
                                                    out dt2))
                        dt2 = dt2.AddMonths(1).AddDays(-1);
                    else
                        throw new InvalidOperationException("日期表达式无效");
                    return new DateFilter(dt, dt2);
                }
                if (sp.Length == 1)
                {
                    if (sOrig[1] != ' ' ^ sOrig[sOrig.Length - 2] != ' ')
                    {
                        if (sOrig[sOrig.Length - 2] != ' ')
                        {
                            if (DateTime.TryParseExact(
                                                       sp[0],
                                                       "yyyyMMdd",
                                                       null,
                                                       DateTimeStyles.AssumeLocal,
                                                       out dt)) { }
                            else if (DateTime.TryParseExact(
                                                            sp[0] + "19",
                                                            reverseMonthType ? "@yyyyMMdd" : "yyyyMMdd",
                                                            null,
                                                            DateTimeStyles.AssumeLocal,
                                                            out dt)) { }
                            else if (DateTime.TryParseExact(
                                                            sp[0] + "01",
                                                            reverseMonthType ? "yyyyMMdd" : "@yyyyMMdd",
                                                            null,
                                                            DateTimeStyles.AssumeLocal,
                                                            out dt))
                                dt = dt.AddMonths(1).AddDays(-1);
                            else
                                throw new InvalidOperationException("日期表达式无效");
                            return new DateFilter(null, dt);
                        }
                        //else
                        {
                            if (DateTime.TryParseExact(
                                                       sp[0],
                                                       "yyyyMMdd",
                                                       null,
                                                       DateTimeStyles.AssumeLocal,
                                                       out dt)) { }
                            else if (DateTime.TryParseExact(
                                                            sp[0] + "20",
                                                            reverseMonthType ? "@yyyyMMdd" : "yyyyMMdd",
                                                            null,
                                                            DateTimeStyles.AssumeLocal,
                                                            out dt))
                                dt = dt.AddMonths(-1);
                            else if (DateTime.TryParseExact(
                                                            sp[0] + "01",
                                                            reverseMonthType ? "yyyyMMdd" : "@yyyyMMdd",
                                                            null,
                                                            DateTimeStyles.AssumeLocal,
                                                            out dt)) { }
                            else
                                throw new InvalidOperationException("日期表达式无效");

                            return new DateFilter(dt, null);
                        }
                    }
                    if (sp[0] == "null")
                        return DateFilter.TheNullOnly;
                }
            }

            if (DateTime.TryParseExact(s, "yyyyMMdd", null, DateTimeStyles.AssumeLocal, out dt))
                return new DateFilter(dt, dt);

            if (DateTime.TryParseExact(
                                       s + "19",
                                       reverseMonthType ? "@yyyyMMdd" : "yyyyMMdd",
                                       null,
                                       DateTimeStyles.AssumeLocal,
                                       out dt))
            {
                var startDate = dt.AddMonths(-1).AddDays(1);
                return new DateFilter(startDate, dt);
            }

            if (DateTime.TryParseExact(
                                       s + "01",
                                       reverseMonthType ? "yyyyMMdd" : "@yyyyMMdd",
                                       null,
                                       DateTimeStyles.AssumeLocal,
                                       out dt))
            {
                var startDate = dt;
                return new DateFilter(startDate, dt.AddMonths(1).AddDays(-1));
            }


            throw new InvalidOperationException("日期表达式无效");
        }

        /// <summary>
        ///     解析资产检索表达式
        /// </summary>
        /// <param name="s">资产检索表达式</param>
        /// <returns>过滤器</returns>
        private static Asset ParseAssetQuery(string s)
        {
            var begin = s.IndexOf('\'');
            if (begin < 0)
                return new Asset();

            var end = s.LastIndexOf('\'');
            if (begin >= end)
                throw new InvalidOperationException("资产检索表达式无效");

            var sx = s.Substring(begin + 1, end - begin - 1);

            Guid g;
            return Guid.TryParse(sx, out g) ? new Asset { ID = g } : new Asset { Name = sx };
        }
    }
}
