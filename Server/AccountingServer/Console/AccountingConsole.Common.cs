using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    internal partial class AccountingConsole
    {
        /// <summary>
        ///     会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public AccountingConsole(Accountant helper) { m_Accountant = helper; }

        /// <summary>
        ///     执行表达式
        /// </summary>
        /// <param name="s">表达式</param>
        /// <param name="editable">执行结果是否可编辑</param>
        /// <returns>执行结果</returns>
        public string Execute(string s, out bool editable)
        {
            s = s.Trim();
            switch (s)
            {
                case "lc":
                    editable = false;
                    {
                        var res = LaunchServer();
                        if (!res.StartsWith("OK"))
                            return res;

                        Thread.Sleep(100);
                        res += Environment.NewLine;
                        res += ConnectServer();
                        return res;
                    }
                case "ue":
                    editable = false;
                    {
                        var res = ShutdownServer();
                        if (res != "OK")
                            return res;

                        Thread.Sleep(100);
                        Environment.Exit(0);
                        // ReSharper disable once HeuristicUnreachableCode
                        return "OK";
                    }
                case "launch":
                case "lau":
                    editable = false;
                    return LaunchServer();
                case "connect":
                case "con":
                    editable = false;
                    return ConnectServer();
                case "shutdown":
                case "shu":
                    editable = false;
                    return ShutdownServer();
                case "mobile":
                case "mob":
                    editable = false;
                    ToggleMobile();
                    return null;
                case "fetch":
                    editable = true;
                    return FetchInfo();
                case "Titles":
                case "T":
                case "t":
                    editable = false;
                    return ListTitles();
                case "help":
                case "?":
                    editable = false;
                    return ListHelp();
                case "chk1":
                    editable = true;
                    return BasicCheck();
                case "chk2":
                    editable = false;
                    return AdvancedCheck();
                case "exit":
                    editable = false;
                    Environment.Exit(0);
                    // ReSharper disable once HeuristicUnreachableCode
                    return "OK";
            }

            if (s.EndsWith("`"))
            {
                editable = false;
                var sx = s.TrimEnd('`', '!');
                return s.EndsWith("!`")
                           ? SubtotalWith2Levels(sx, s.EndsWith("!``"))
                           : SubtotalWith3Levels(sx, s.EndsWith("``"));
            }
            if (s.EndsWith("D", StringComparison.OrdinalIgnoreCase))
            {
                editable = false;
                var sx = s.TrimEnd('`', '!', 'D', 'd');
                return s.EndsWith("!`D")
                           ? DailySubtotalWith3Levels(
                                                      sx,
                                                      s.EndsWith("!``D", StringComparison.OrdinalIgnoreCase),
                                                      s.EndsWith("D", StringComparison.Ordinal),
                                                      false)
                           : DailySubtotalWith4Levels(
                                                      sx,
                                                      s.EndsWith("``D", StringComparison.OrdinalIgnoreCase),
                                                      s.EndsWith("D", StringComparison.Ordinal),
                                                      false);
            }
            if (s.EndsWith("A", StringComparison.OrdinalIgnoreCase))
            {
                editable = false;
                var sx = s.TrimEnd('`', '!', 'A', 'a');
                return s.EndsWith("!`A")
                           ? DailySubtotalWith3Levels(
                                                      sx,
                                                      s.EndsWith("!``A", StringComparison.OrdinalIgnoreCase),
                                                      s.EndsWith("A", StringComparison.Ordinal),
                                                      true)
                           : DailySubtotalWith4Levels(
                                                      sx,
                                                      s.EndsWith("``A", StringComparison.OrdinalIgnoreCase),
                                                      s.EndsWith("A", StringComparison.Ordinal),
                                                      true);
            }

            if (s.StartsWith("R", StringComparison.OrdinalIgnoreCase))
            {
                editable = false;
                return GenerateReport(s);
            }

            if (s.StartsWith("C", StringComparison.OrdinalIgnoreCase))
            {
                editable = false;
                DateTime? startDate, endDate;
                bool nullable;
                if (!ParseDateQuery(s.Substring(1), out startDate, out endDate, out nullable) ||
                    nullable ||
                    !startDate.HasValue ||
                    !endDate.HasValue)
                    throw new InvalidOperationException("日期表达式无效");

                if (!m_Accountant.Connected)
                    throw new InvalidOperationException("尚未连接到数据库");

                return String.Format("CHART {0:s} {1:s}", startDate, endDate);
            }

            if (s.StartsWith("a"))
                return ExecuteAsset(s, out editable);

            {
                editable = true;
                return VouchersQuery(s);
            }
        }

        /// <summary>
        ///     对记账凭证执行检索表达式
        /// </summary>
        /// <param name="s">检索表达式</param>
        /// <returns>检索结果，若表达式无效则为<c>null</c></returns>
        private IEnumerable<Voucher> ExecuteQuery(string s)
        {
            string dateQ;
            var detail = ParseQuery(s, out dateQ);

            DateTime? startDate, endDate;
            bool nullable;
            if (!ParseDateQuery(dateQ, out startDate, out endDate, out nullable))
                return null;

            if (!m_Accountant.Connected)
                throw new InvalidOperationException("尚未连接到数据库");

            if (startDate.HasValue ||
                endDate.HasValue ||
                nullable)
                return m_Accountant.FilteredSelect(detail, startDate, endDate);
            return m_Accountant.FilteredSelect(detail);
        }

        /// <summary>
        ///     对细目执行检索表达式
        /// </summary>
        /// <param name="s">检索表达式</param>
        /// <returns>检索结果，若表达式无效则为<c>null</c></returns>
        private IEnumerable<VoucherDetail> ExecuteDetailQuery(string s)
        {
            string dateQ;
            var detail = ParseQuery(s, out dateQ);

            DateTime? startDate, endDate;
            bool nullable;
            if (!ParseDateQuery(dateQ, out startDate, out endDate, out nullable))
                return null;

            if (!m_Accountant.Connected)
                throw new InvalidOperationException("尚未连接到数据库");

            if (startDate.HasValue ||
                endDate.HasValue ||
                nullable)
                return m_Accountant.SelectDetails(detail, startDate, endDate);
            return m_Accountant.SelectDetails(detail);
        }

        /// <summary>
        ///     直接检索记账凭证
        /// </summary>
        /// <param name="s">检索表达式</param>
        /// <returns>记账凭证表达式</returns>
        private string VouchersQuery(string s)
        {
            var sb = new StringBuilder();
            var query = ExecuteQuery(s);
            if (query == null)
                throw new InvalidOperationException("日期表达式无效");

            foreach (var voucher in query)
                sb.Append(PresentVoucher(voucher));
            return sb.ToString();
        }

        /// <summary>
        ///     检索记账凭证并生成报销报表
        /// </summary>
        /// <param name="s">检索表达式</param>
        /// <returns>报销报表</returns>
        private string GenerateReport(string s)
        {
            var isExp = s.EndsWith("-exp");
            s = isExp ? s.Substring(1, s.Length - 5) : s.Substring(1);

            DateTime? startDate, endDate;
            bool nullable;
            if (!ParseDateQuery(s, out startDate, out endDate, out nullable))
                return null;

            if (!m_Accountant.Connected)
                throw new InvalidOperationException("尚未连接到数据库");

            var report = new ReimbursementReport(m_Accountant, startDate, endDate);

            return isExp ? report.ExportString() : report.Preview();
        }
    }
}
