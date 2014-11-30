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
        private readonly BHelper m_BHelper;

        public AccountingConsole(BHelper helper) { m_BHelper = helper; }

        internal IEnumerable<Voucher> ParseVoucherQuery(string sOrig)
        {
            if (sOrig == "[]")
                return m_BHelper.SelectVouchers(new Voucher());

            var s = sOrig.Trim('[', ' ', ']');

            if (s == ".")
                return m_BHelper.SelectVouchers(new Voucher { Date = DateTime.Now.Date });

            if (s == "..")
            {
                var date = DateTime.Now.Date;
                var mon = date.AddDays(-(int)date.DayOfWeek);
                var sun = mon.AddDays(6);
                return m_BHelper.SelectVouchers(mon, sun);
            }

            if (s == "0")
            {
                var date = DateTime.Now.Date;
                var d20th = date.Day <= 19 ? date.AddMonths(-1).AddDays(20 - date.Day) : date.AddDays(19 - date.Day);
                var d19th = d20th.AddMonths(1).AddDays(-1);
                return m_BHelper.SelectVouchers(d20th, d19th);
            }

            if (s == "-1")
            {
                var date = DateTime.Now.Date.AddMonths(-1);
                var d20th = date.Day <= 19 ? date.AddMonths(-1).AddDays(20 - date.Day) : date.AddDays(19 - date.Day);
                var d19th = d20th.AddMonths(1).AddDays(-1);
                return m_BHelper.SelectVouchers(d20th, d19th);
            }

            if (s == "@0")
            {
                var date = DateTime.Now.Date;
                var d1st = date.AddDays(1 - date.Day);
                var d99th = d1st.AddMonths(1).AddDays(-1);
                return m_BHelper.SelectVouchers(d1st, d99th);
            }

            if (s == "@-1")
            {
                var date = DateTime.Now.Date.AddMonths(-1);
                var d1st = date.AddDays(1 - date.Day);
                var d99th = d1st.AddMonths(1).AddDays(-1);
                return m_BHelper.SelectVouchers(d1st, d99th);
            }

            DateTime dt;
            if (DateTime.TryParseExact(s, "yyyyMMdd", null, DateTimeStyles.AssumeLocal, out dt))
                return m_BHelper.SelectVouchers(new Voucher { Date = dt });

            if (DateTime.TryParseExact(s + "19", "yyyyMMdd", null, DateTimeStyles.AssumeLocal, out dt))
            {
                var d20th = dt.AddMonths(-1).AddDays(1);
                return m_BHelper.SelectVouchers(d20th, dt);
            }

            if (DateTime.TryParseExact(s + "01", "@yyyyMMdd", null, DateTimeStyles.AssumeLocal, out dt))
            {
                var d99th = dt.AddMonths(1).AddDays(-1);
                return m_BHelper.SelectVouchers(dt, d99th);
            }

            var sp = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (sp.Length == 2)
            {
                DateTime dt2;
                if (DateTime.TryParseExact(sp[0], "yyyyMMdd", null, DateTimeStyles.AssumeLocal, out dt)
                    &&
                    DateTime.TryParseExact(sp[1], "yyyyMMdd", null, DateTimeStyles.AssumeLocal, out dt2))
                    return m_BHelper.SelectVouchers(dt, dt2);
            }
            else if (sp.Length == 1)
            {
                if (DateTime.TryParseExact(sp[0], "yyyyMMdd", null, DateTimeStyles.AssumeLocal, out dt))
                    return sOrig.TrimStart('[')[0] != ' '
                               ? m_BHelper.SelectVouchers(dt, null)
                               : m_BHelper.SelectVouchers(null, dt);
                return m_BHelper.SelectVouchers(null, null);
            }

            return null;
        }

        private void PresentVoucher(Voucher voucher, StringBuilder sb)
        {
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
                sb.AppendFormat("Title = {0:0}", detail.Title);
                if (detail.SubTitle.HasValue)
                    sb.AppendFormat(", SubTitle = {0:00}", detail.SubTitle);
                if (detail.Content != null)
                    sb.AppendFormat(", Content = {0}", ProcessString(detail.Content));
                if (detail.Fund.HasValue)
                    sb.AppendFormat(", Fund = {0}", detail.Fund);
                if (detail.Remark != null)
                    sb.AppendFormat(", Remark = {0}", ProcessString(detail.Remark));
                sb.Append(" }");
                flag = true;
            }
            sb.AppendLine(Environment.NewLine + "                        }" + Environment.NewLine + "    }");
            sb.AppendLine();
        }

        private Voucher ParseVoucher(string str)
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

        private static string ProcessString(string s)
        {
            s = s.Replace("\\", "\\\\");
            s = s.Replace("\"", "\\\"");
            return "\"" + s + "\"";
        }
    }
}
