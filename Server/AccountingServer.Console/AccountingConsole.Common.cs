using System;
using System.Collections.Generic;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class AccountingConsole
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
        public IQueryResult Execute(string s)
        {
            try
            {
                s = s.Trim();
                switch (s.ToLowerInvariant())
                {
                    case "launch":
                    case "lau":
                        return LaunchServer();
                    case "connect":
                    case "con":
                        return ConnectServer();
                    //case "mobile":
                    //case "mob":
                    //    ToggleMobile();
                    //    return null;
                    case "backup":
                        return Backup();
                    case "fetch":
                        return FetchInfo();
                    case "titles":
                    case "t":
                        return ListTitles();
                    case "help":
                    case "?":
                        return ListHelp();
                    case "chk1":
                        return BasicCheck();
                    case "chk2":
                        return AdvancedCheck();
                    case "exit":
                        Environment.Exit(0);
                        // ReSharper disable once HeuristicUnreachableCode
                        return new Suceed();
                }

                if (s.StartsWith("a", StringComparison.OrdinalIgnoreCase))
                    return ExecuteAsset(s);
                if (s.StartsWith("o", StringComparison.OrdinalIgnoreCase))
                    return ExecuteAmort(s);

                if (s.EndsWith("`"))
                {
                    var sx = s.TrimEnd('`', '!');
                    return s.EndsWith("!`")
                               ? SubtotalWith2Levels(sx, s.EndsWith("!``"))
                               : SubtotalWith3Levels(sx, s.EndsWith("``"));
                }
                if (s.EndsWith("D", StringComparison.OrdinalIgnoreCase))
                {
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
                    return GenerateReport(s);

                if (s.StartsWith("C", StringComparison.OrdinalIgnoreCase))
                {
                    var rng = ParseDateQuery(s.Substring(1));
                    if (!rng.Constrained)
                        throw new InvalidOperationException("日期表达式无效");

                    AutoConnect();

                    // ReSharper disable PossibleInvalidOperationException
                    return new DefaultChart(rng.StartDate.Value, rng.EndDate.Value);
                    // ReSharper restore PossibleInvalidOperationException
                }

                return VouchersQuery(s);
            }
            catch (Exception e)
            {
                return new Failed(e);
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

            var rng = ParseDateQuery(dateQ);

            AutoConnect();

            return m_Accountant.FilteredSelect(filter: detail, rng: rng);
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

            var rng = ParseDateQuery(dateQ);

            AutoConnect();

            return m_Accountant.FilteredSelectDetails(filter: detail, rng: rng);
        }

        /// <summary>
        ///     直接检索记账凭证
        /// </summary>
        /// <param name="s">检索表达式</param>
        /// <returns>记账凭证表达式</returns>
        private IQueryResult VouchersQuery(string s)
        {
            var sb = new StringBuilder();
            var query = ExecuteQuery(s);
            if (query == null)
                throw new InvalidOperationException("日期表达式无效");

            foreach (var voucher in query)
                sb.Append(CSharpHelper.PresentVoucher(voucher));
            return new EditableText(sb.ToString());
        }

        /// <summary>
        ///     检索记账凭证并生成报销报表
        /// </summary>
        /// <param name="s">检索表达式</param>
        /// <returns>报销报表</returns>
        private IQueryResult GenerateReport(string s)
        {
            var isExp = s.EndsWith("-exp");
            s = isExp ? s.Substring(1, s.Length - 5) : s.Substring(1);

            var rng = ParseDateQuery(s);

            AutoConnect();

            var report = new ReimbursementReport(m_Accountant, rng);

            return new UnEditableText(isExp ? report.ExportString() : report.Preview());
        }
    }
}
