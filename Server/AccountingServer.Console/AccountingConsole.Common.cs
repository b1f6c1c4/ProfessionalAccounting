using System;
using System.Collections.Generic;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Console.Chart;
using AccountingServer.Entities;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

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
        /// <returns>执行结果</returns>
        public IQueryResult Execute(string s)
        {
            var lexer = new ConsoleLexer(new AntlrInputStream(s));
            var parser = new ConsoleParser(new CommonTokenStream(lexer));
            var result = parser.command();
            if (result.exception != null)
                throw new Exception(result.exception.ToString());

            if (result.GetChild(0) is ConsoleParser.OtherCommandContext)
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
                    case "exit":
                        Environment.Exit(0);
                        // ReSharper disable once HeuristicUnreachableCode
                        return new Suceed();
                }
                throw new InvalidOperationException();
            }
            if (result.GetChild(0) is ConsoleParser.VouchersContext)
            {
                AutoConnect();

                var sb = new StringBuilder();
                foreach (var voucher in m_Accountant.FilteredSelect(result.GetChild(0) as IVoucherQueryCompounded))
                    sb.Append(CSharpHelper.PresentVoucher(voucher));
                return new EditableText(sb.ToString());
            }
        }


        /// <summary>
        ///     直接检索记账凭证
        /// </summary>
        /// <param name="s">检索表达式</param>
        /// <param name="dir">+1表示只考虑借方，-1表示只考虑贷方，0表示同时考虑借方和贷方</param>
        /// <returns>记账凭证表达式</returns>
        private IQueryResult VouchersQuery(string s, int dir)
        {
            var sb = new StringBuilder();
            var query = ExecuteQuery(s, dir);
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
