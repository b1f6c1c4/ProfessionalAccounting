using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;
using Microsoft.CSharp;

namespace AccountingServer
{
    internal class AccountingConsole
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
        public string Execute(string s)
        {
            s = s.Trim();
            if (s.EndsWith("`")) //分类汇总
            {
                var res = ExecuteQuery(s.Substring(0, s.Length - 1));
                var tsc = res.SelectMany(v => v.Details)
                             .GroupBy(
                                      d =>
                                      new Balance { Title = d.Title, SubTitle = d.SubTitle, Content = d.Content },
                                      (b, bs) =>
                                      new Balance
                                          {
                                              Title = b.Title,
                                              SubTitle = b.SubTitle,
                                              Content = b.Content,
                                              Fund = bs.Sum(d => d.Fund.Value)
                                          },
                                      new BalanceEqualityComparer()).ToList();
                var ts = tsc.GroupBy(
                                     d =>
                                     new Balance { Title = d.Title, SubTitle = d.SubTitle },
                                     (b, bs) =>
                                     new Balance
                                         {
                                             Title = b.Title,
                                             SubTitle = b.SubTitle,
                                             Fund = bs.Sum(d => d.Fund)
                                         },
                                      new BalanceEqualityComparer()).ToList();
                var t = ts.GroupBy(
                                    d => d.Title,
                                    (b, bs) =>
                                    new Balance
                                        {
                                            Title = b,
                                            Fund = bs.Sum(d => d.Fund)
                                        },
                                    EqualityComparer<int?>.Default).ToList();

                var sb = new StringBuilder();
                foreach (var balanceT in t)
                {
                    var copiedT = balanceT;
                    sb.AppendFormat("{0}-{1}:{2}", copiedT.Title.AsTitle(), Accountant.GetTitleName(copiedT.Title), copiedT.Fund.AsCurrency());
                    sb.AppendLine();
                    foreach (var balanceS in ts.Where(sx => sx.Title == copiedT.Title))
                    {
                        var copiedS = balanceS;
                        sb.AppendFormat(
                                        "  {0}-{1}:{2}",
                                        copiedS.SubTitle.HasValue ? copiedS.SubTitle.AsSubTitle() : "  ",
                                        Accountant.GetTitleName(copiedS),
                                        copiedS.Fund.AsCurrency());
                        sb.AppendLine();
                        foreach (var balanceC in tsc.Where(
                                                          cx => cx.Title == copiedS.Title
                                                                && cx.SubTitle == copiedS.SubTitle))
                        {
                            var copiedC = balanceC;
                            sb.AppendFormat(
                                            "        {0}:{1}",
                                            copiedC.Content,
                                            copiedC.Fund.AsCurrency());
                            sb.AppendLine();
                        }
                    }
                }
                return sb.ToString();
            }
            return String.Empty;
        }

        /// <summary>
        ///     执行检索表达式
        /// </summary>
        /// <param name="s">检索表达式</param>
        /// <returns>检索结果，若表达式无效则为<c>null</c></returns>
        public IEnumerable<Voucher> ExecuteQuery(string s)
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
                    dateQ = s.Substring(id2);
                }
                else
                {
                    id1 = s.IndexOf("[", StringComparison.Ordinal);
                    dateQ = s.Substring(id1);
                }
                detail.Title = Convert.ToInt32(s.Substring(1, 4));

                if (id1 > 4)
                    detail.SubTitle = Convert.ToInt32(s.Substring(5, Math.Min(2, id1 - 4)));
                else
                    detail.SubTitle = null;
            }
            else if (s.StartsWith("'"))
            {
                var id2 = s.LastIndexOf("'", StringComparison.Ordinal);
                detail.Content = s.Substring(1, id2 - 1);
                dateQ = s.Substring(id2);
            }
            else
                dateQ = s;

            DateTime? startDate, endDate;
            bool nullable;
            if (!ParseVoucherQuery(dateQ, out startDate, out endDate, out nullable))
                return null;

            if (startDate.HasValue ||
                endDate.HasValue ||
                nullable)
                return m_Accountant.SelectVouchersWithDetail(detail, startDate, endDate);
            return m_Accountant.SelectVouchersWithDetail(detail);
        }

        /// <summary>
        ///     解析日期检索表达式
        /// </summary>
        /// <param name="sOrig">日期检索表达式</param>
        /// <param name="startDate">开始日期，<c>null</c>表示不限最小日期并且包含无日期</param>
        /// <param name="endDate">截止日期，<c>null</c>表示不限最大日期</param>
        /// <param name="nullable"><paramref name="startDate" />和<paramref name="endDate" />均为<c>null</c>时，表示是否只包含无日期</param>
        /// <returns>是否是合法的检索表达式</returns>
        private static bool ParseVoucherQuery(string sOrig, out DateTime? startDate, out DateTime? endDate, out bool nullable)
        {
            nullable = false;

            if (sOrig == "[]")
            {
                startDate = null;
                endDate = null;
                return true;
            }

            var s = sOrig.Trim('[', ' ', ']');

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

        /// <summary>
        ///     将记账凭证用C#书写
        /// </summary>
        /// <param name="voucher">记账凭证</param>
        /// <returns>C#表达式</returns>
        private static string PresentVoucher(Voucher voucher)
        {
            var sb = new StringBuilder();
            sb.AppendLine("new Voucher");
            sb.AppendLine("    {");
            sb.AppendFormat("        ID = {0},", ProcessString(voucher.ID));
            sb.AppendLine();
            if (voucher.Date.HasValue)
            {
                sb.AppendFormat("        Date = DateTime.Parse(\"{0:yyyy-MM-dd}\"),",voucher.Date);
                sb.AppendLine();
            }
            else
                sb.AppendLine("        Date = null\",");
            if ((voucher.Type ?? VoucherType.Ordinal) != VoucherType.Ordinal)
            {
                sb.AppendFormat("        Type = VoucherType.{0},", voucher.Type);
                sb.AppendLine();
            }
            if (voucher.Remark != null)
            {
                sb.AppendFormat("        Remark = {0},", ProcessString(voucher.Remark));
                sb.AppendLine();
            }
            sb.AppendLine("        Details = new[]");
            sb.AppendLine("                        {");
            foreach (var detail in voucher.Details)
            {
                sb.Append("                            new VoucherDetail { ");
                sb.AppendFormat("Title = {0:0}, ", detail.Title);
                if (detail.SubTitle.HasValue)
                    sb.AppendFormat(
                                    "SubTitle = {0:00},    // {1}",
                                    detail.SubTitle,
                                    Accountant.GetTitleName(detail));
                else
                    sb.AppendFormat("                  // {0}", Accountant.GetTitleName(detail));
                sb.AppendLine();
                sb.Append("                                                ");
                if (detail.Content != null)
                    sb.AppendFormat("Content = {0}, ", ProcessString(detail.Content));
                if (detail.Fund.HasValue)
                    sb.AppendFormat("Fund = {0}", detail.Fund);
                if (detail.Remark != null)
                    sb.AppendFormat(", Remark = {0}", ProcessString(detail.Remark));
                sb.AppendLine(" },");
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.AppendLine("                        }");
            sb.AppendLine("    }");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        ///     从C#表达式中取得记账凭证
        /// </summary>
        /// <param name="str">C#表达式</param>
        /// <returns>记账凭证</returns>
        private static Voucher ParseVoucher(string str)
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
        ///     转义字符串
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
