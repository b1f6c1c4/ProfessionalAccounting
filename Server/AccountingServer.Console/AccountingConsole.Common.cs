using System;
using AccountingServer.BLL;

namespace AccountingServer.Console
{
    /// <summary>
    ///     控制台
    /// </summary>
    public partial class AccountingConsole
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public AccountingConsole(Accountant helper) { m_Accountant = helper; }

        /// <summary>
        ///     执行表达式
        /// </summary>
        /// <param name="s">表达式</param>
        /// <returns>执行结果</returns>
        public IQueryResult Execute(string s)
        {
            var result = ConsoleParser.From(s).command();
            if (result.exception != null)
                throw new Exception(result.exception.ToString());

            if (result.vouchers() != null)
                return PresentVoucherQuery(result.vouchers());
            if (result.groupedQuery() != null)
                return PresentSubtotal(result.groupedQuery());
            if (result.chart() != null)
                return ExecuteChartQuery(result.chart());
            if (result.report() != null)
                return ExecuteReportQuery(result.report());
            if (result.asset() != null)
                return ExecuteAsset(result.asset());
            if (result.amort() != null)
                return ExecuteAmort(result.amort());
            if (result.otherCommand() != null)
            {
                switch (result.GetChild(0).GetText().ToLowerInvariant())
                {
                    case "launch":
                    case "lau":
                        return LaunchServer();
                    case "connect":
                    case "con":
                        return ConnectServer();
                    case "mobile":
                    case "mob":
                        throw new NotImplementedException();
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
                    case "nq":
                        return ListNamedQueryTemplates();
                    case "exit":
                        Environment.Exit(0);
                        // ReSharper disable once HeuristicUnreachableCode
                        return new Suceed();
                }
                throw new InvalidOperationException();
            }
            throw new InvalidOperationException();
        }
    }
}
