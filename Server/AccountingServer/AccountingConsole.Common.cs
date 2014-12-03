using System;
using System.Collections.Generic;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer
{
    internal partial class AccountingConsole
    {
        /// <summary>
        ///     会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public AccountingConsole(Accountant helper) { m_Accountant = helper; }

        /// <summary>
        ///     更新或添加记账凭证
        /// </summary>
        /// <param name="code">记账凭证的C#代码</param>
        /// <returns>新记账凭证的C#代码</returns>
        public string ExecuteUpsert(string code)
        {
            var voucher = ParseVoucher(code);

            if (voucher.ID == null)
            {
                if (!m_Accountant.InsertVoucher(voucher))
                    throw new Exception();
            }
            else if (!m_Accountant.UpdateVoucher(voucher))
                throw new Exception();

            return PresentVoucher(voucher);
        }

        /// <summary>
        ///     删除记账凭证
        /// </summary>
        /// <param name="code">记账凭证的C#代码</param>
        /// <returns>是否成功</returns>
        public bool ExecuteRemoval(string code)
        {
            var voucher = ParseVoucher(code);
            if (voucher.ID == null)
                throw new Exception();

            return m_Accountant.DeleteVoucher(voucher.ID);
        }

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
                    editable = false;
                    return FetchInfo();
                case "Titles":
                case "T":
                    editable = false;
                    return ListTitles();
                case "help":
                case "?":
                    editable = false;
                    return ListHelp();
                case "c1":
                    editable = true;
                    return BasicCheck();
                case "c2":
                    editable = false;
                    return AdvancedCheck();
            }

            if (s.EndsWith("`"))
            {
                editable = false;
                var sx = s.TrimEnd('`', '!');
                if (s.EndsWith("!`"))
                {
                    var withZero = s.EndsWith("!``");

                    return SubtotalWith2Levels(sx, withZero);
                }
                else
                {
                    var withZero = s.EndsWith("``");

                    return SubtotalWith3Levels(sx, withZero);
                }
            }

            if (s.StartsWith("R"))
            {
                editable = false;
                return GenerateReport(s);
            }

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
            if (!ParseVoucherQuery(dateQ, out startDate, out endDate, out nullable))
                return null;

            if (!m_Accountant.Connected)
                throw new InvalidOperationException("尚未连接到数据库");

            if (startDate.HasValue ||
                endDate.HasValue ||
                nullable)
                return m_Accountant.SelectVouchersWithDetail(detail, startDate, endDate);
            return m_Accountant.SelectVouchersWithDetail(detail);
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
            if (!ParseVoucherQuery(dateQ, out startDate, out endDate, out nullable))
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
            DateTime? startDate, endDate;
            bool nullable;
            if (!ParseVoucherQuery(s.Substring(1), out startDate, out endDate, out nullable))
                return null;

            var report = new ReimbursementReport(m_Accountant, startDate, endDate);

            return report.Preview();
        }
    }
}
