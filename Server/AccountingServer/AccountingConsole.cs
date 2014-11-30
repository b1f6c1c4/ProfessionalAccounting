using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccountingServer.BLL;
using AccountingServer.Entities;
using Microsoft.CSharp;

namespace AccountingServer
{
    internal class AccountingConsole
    {
        /// <summary>
        /// 会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public AccountingConsole(Accountant helper) { m_Accountant = helper; }

        /// <summary>
        /// 解析检索表达式
        /// </summary>
        /// <param name="sOrig">检索表达式</param>
        /// <param name="startDate">开始日期，<c>null</c>表示不限最小日期</param>
        /// <param name="endDate">截止日期，<c>null</c>表示不限最大日期</param>
        /// <param name="nullable">是否包含无日期（若<paramref name="startDate"/>和<paramref name="endDate"/>均为<c>null</c>，则表示是否只包含无日期）</param>
        /// <returns>是否是合法的检索表达式</returns>
        internal bool ParseVoucherQuery(string sOrig, out DateTime? startDate, out DateTime? endDate, out bool nullable)
        {
            if (sOrig == "[]")
            {
                startDate = null;
                endDate = null;
                nullable = false;
                return true;
            }

            var s = sOrig.Trim('[', ' ', ']');

            if (s == ".")
            {
                startDate = DateTime.Now.Date;
                endDate = startDate;
                nullable = false;
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
                nullable = false;
                return true;
            }

            if (s == "0")
            {
                var date = DateTime.Now.Date;
                startDate = date.Day <= 19 ? date.AddMonths(-1).AddDays(20 - date.Day) : date.AddDays(19 - date.Day);
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
                nullable = false;
                return true;
            }

            if (s == "-1")
            {
                var date = DateTime.Now.Date.AddMonths(-1);
                startDate = date.Day <= 19 ? date.AddMonths(-1).AddDays(20 - date.Day) : date.AddDays(19 - date.Day);
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
                nullable = false;
                return true;
            }

            if (s == "@0")
            {
                var date = DateTime.Now.Date;
                startDate = date.AddDays(1 - date.Day);
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
                nullable = false;
                return true;
            }

            if (s == "@-1")
            {
                var date = DateTime.Now.Date.AddMonths(-1);
                startDate = date.AddDays(1 - date.Day);
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
                nullable = false;
                return true;
            }

            DateTime dt;
            if (DateTime.TryParseExact(s, "yyyyMMdd", null, DateTimeStyles.AssumeLocal, out dt))
            {

                startDate = DateTime.Now.Date;
                endDate = startDate;
                nullable = false;
                return true;
            }

            if (DateTime.TryParseExact(s + "19", "yyyyMMdd", null, DateTimeStyles.AssumeLocal, out dt))
            {
                startDate = dt.AddMonths(-1).AddDays(1);
                endDate = dt;
                nullable = false;
                return true;
            }

            if (DateTime.TryParseExact(s + "01", "@yyyyMMdd", null, DateTimeStyles.AssumeLocal, out dt))
            {
                startDate = dt;
                endDate = dt.AddMonths(1).AddDays(-1);
                nullable = false;
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
                    nullable = false;
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
                        nullable = false;
                    }
                    else
                    {
                        startDate = null;
                        endDate = dt;
                        nullable = true;
                    }
                    return true;
                }
                if (sp[0]=="null")
                {
                    startDate = null;
                    endDate = null;
                    nullable = true;
                    return true;
                }
            }

            startDate = null;
            endDate = null;
            nullable = false;
            return false;
        }

        /// <summary>
        /// 解析检索表达式并以此检索记账凭证
        /// </summary>
        /// <param name="s">检索表达式</param>
        /// <returns>记账凭证，若检索表达式无效则为<c>null</c></returns>
        public IEnumerable<Voucher> ParseVoucherQuery(string s)
        {
            DateTime? startDate, endDate;
            bool nullable;
            if (!ParseVoucherQuery(s, out startDate, out endDate, out nullable))
                return null;
            
            return m_Accountant.SelectVouchers(startDate, endDate);
        }

        /// <summary>
        /// 将记账凭证用C#书写
        /// </summary>
        /// <param name="voucher">记账凭证</param>
        /// <returns>C#表达式</returns>
        public string PresentVoucher(Voucher voucher)
        {
            var sb = new StringBuilder();
            sb.AppendLine("new Voucher");
            sb.AppendLine("    {");
            sb.AppendFormat("        ID = {0}," + Environment.NewLine, ProcessString(voucher.ID));
            if (voucher.Date.HasValue)
                sb.AppendFormat("        Date = DateTime.Parse(\"{0:yyyy-MM-dd}\")," + Environment.NewLine, voucher.Date);
            else
                sb.AppendLine("        Date = null\",");
            if ((voucher.Type ?? VoucherType.Ordinal) != VoucherType.Ordinal)
                sb.AppendFormat("        Type = VoucherType.{0}," + Environment.NewLine, voucher.Type);
            if (voucher.Remark != null)
                sb.AppendFormat("        Remark = {0}," + Environment.NewLine, ProcessString(voucher.Remark));
            sb.AppendLine("        Details = new[]");
            sb.AppendLine("                        {");
            var flag = false;
            foreach (var detail in voucher.Details)
            {
                if (flag)
                    sb.AppendLine(",");
                sb.Append("                            new VoucherDetail { ");
                sb.AppendFormat("Title = {0:0}, ", detail.Title);
                if (detail.SubTitle.HasValue)
                    sb.AppendFormat(
                                    "SubTitle = {0:00}, // {1}-{2}" + Environment.NewLine,
                                    detail.SubTitle,
                                    Accountant.GetTitleName(detail.Title),
                                    Accountant.GetTitleName(detail));
                sb.Append("                                                ");
                if (detail.Content != null)
                    sb.AppendFormat("Content = {0}, ", ProcessString(detail.Content));
                if (detail.Fund.HasValue)
                    sb.AppendFormat("Fund = {0}", detail.Fund);
                if (detail.Remark != null)
                    sb.AppendFormat(", Remark = {0}", ProcessString(detail.Remark));
                sb.AppendLine(" }");
                flag = true;
            }
            sb.AppendLine(Environment.NewLine + "                        }" + Environment.NewLine + "    }");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// 从C#表达式中取得记账凭证
        /// </summary>
        /// <param name="str">C#表达式</param>
        /// <returns>记账凭证</returns>
        public Voucher ParseVoucher(string str)
        {
            var provider = new CSharpCodeProvider();
            var paras = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true,
                ReferencedAssemblies = { "AccountingServer.Entities.dll" }
            };
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using AccountingServer.Entities;");
            sb.AppendLine("namespace AccountingServer.Dynamic");
            sb.AppendLine("{");
            sb.AppendLine("    public static class VoucherCreator");
            sb.AppendLine("    {");
            sb.AppendLine("        public static Voucher GetVoucher()");
            sb.AppendLine("        {");
            sb.AppendFormat("            return {0};" + Environment.NewLine, str);
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            var result = provider.CompileAssemblyFromSource(paras, sb.ToString());
            var resultAssembly = result.CompiledAssembly;
            return
                (Voucher)
                resultAssembly.GetType("AccountingServer.Dynamic.VoucherCreator")
                              .GetMethod("GetVoucher")
                              .Invoke(null, null);
        }

        /// <summary>
        /// 转义字符串
        /// </summary>
        /// <param name="s">待转义的字符串</param>
        /// <returns>转义后的字符串</returns>
        private static string ProcessString(string s)
        {
            if (s == null)
                return "null";

            s = s.Replace("\\", "\\\\");
            s = s.Replace("\"", "\\\"");
            return "\"" + s + "\"";
        }

    }
}
