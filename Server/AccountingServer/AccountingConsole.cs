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
            if (s == "con")
                try
                {
                    m_Accountant.Connect(true);
                    editable = false;
                    return "OK";
                }
                catch (Exception e)
                {
                    editable = false;
                    return e.ToString();
                }
            if (s == "shu")
                try
                {
                    m_Accountant.Shutdown();
                    m_Accountant.Disconnect();
                    editable = false;
                    return "OK";
                }
                catch (Exception e)
                {
                    editable = false;
                    return e.ToString();
                }
            if (s == "mobile")
            {
                editable = false;
                if (m_Mobile == null)
                {
                    m_Mobile = new MobileComm();

                    m_Mobile.Connect(m_Accountant);

                    PresentQRCode(m_Mobile.GetQRCode(256, 256));
                }
                else
                {
                    m_Mobile.Dispose();
                    m_Mobile = null;

                    PresentQRCode(null);
                }
                return null;
            }
            if (s == "fetch")
            {
                editable = false;
                string res;
                THUInfo.FetchFromInfo(out res);
                return res;
            }
            if (s == "T")
            {
                editable = false;
                var sb = new StringBuilder();
                foreach (var title in TitleManager.GetTitles())
                {
                    sb.AppendFormat("{0}{1}\t\t{2}", title.Item1.AsTitle(), title.Item2.AsSubTitle(), title.Item3);
                    sb.AppendLine();
                }
                return sb.ToString();
            }
            if (s.EndsWith("`"))
            {
                editable = false;
                var sx = s.TrimEnd('`', '!');
                if (s.EndsWith("!`"))
                {
                    var res = ExecuteDetailQuery(sx);
                    var tscX = res.GroupBy(
                                           d =>
                                           new Balance { Title = d.Title, Content = d.Content },
                                           (b, bs) =>
                                           new Balance
                                               {
                                                   Title = b.Title,
                                                   Content = b.Content,
                                                   Fund = bs.Sum(d => d.Fund.Value)
                                               },
                                           new BalanceEqualityComparer());
                    var tsc = !s.EndsWith("!``") ? tscX.Where(d => Math.Abs(d.Fund) > 1e-12).ToList() : tscX.ToList();
                    tsc.Sort(new BalanceComparer());
                    var tX = tsc.GroupBy(
                                         d => d.Title,
                                         (b, bs) =>
                                         new Balance
                                             {
                                                 Title = b,
                                                 Fund = bs.Sum(d => d.Fund)
                                             },
                                         EqualityComparer<int?>.Default);
                    var t = !s.EndsWith("``") ? tX.Where(d => Math.Abs(d.Fund) > 1e-12).ToList() : tX.ToList();

                    return PresentSubtotal(t, tsc);
                }
                else
                {
                    var res = ExecuteDetailQuery(sx);
                    var tscX = res.GroupBy(
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
                                           new BalanceEqualityComparer());
                    var tsc = !s.EndsWith("``") ? tscX.Where(d => Math.Abs(d.Fund) > 1e-12).ToList() : tscX.ToList();
                    tsc.Sort(new BalanceComparer());
                    var tsX = tsc.GroupBy(
                                          d =>
                                          new Balance { Title = d.Title, SubTitle = d.SubTitle },
                                          (b, bs) =>
                                          new Balance
                                              {
                                                  Title = b.Title,
                                                  SubTitle = b.SubTitle,
                                                  Fund = bs.Sum(d => d.Fund)
                                              },
                                          new BalanceEqualityComparer());
                    var ts = !s.EndsWith("``") ? tsX.Where(d => Math.Abs(d.Fund) > 1e-12).ToList() : tsX.ToList();
                    var tX = ts.GroupBy(
                                        d => d.Title,
                                        (b, bs) =>
                                        new Balance
                                            {
                                                Title = b,
                                                Fund = bs.Sum(d => d.Fund)
                                            },
                                        EqualityComparer<int?>.Default);
                    var t = !s.EndsWith("``") ? tX.Where(d => Math.Abs(d.Fund) > 1e-12).ToList() : tX.ToList();

                    return PresentSubtotal(t, ts, tsc);
                }
            }

            if (s.StartsWith("R"))
            {
                editable = false;
                DateTime? startDate, endDate;
                bool nullable;
                if (!ParseVoucherQuery(s.Substring(1), out startDate, out endDate, out nullable))
                    return null;

                var report = new ReimbursementReport(m_Accountant, startDate, endDate);

                return report.Preview();
            }

            {
                editable = true;
                var sb = new StringBuilder();
                foreach (var voucher in ExecuteQuery(s))
                    sb.Append(PresentVoucher(voucher));
                return sb.ToString();
            }
        }

        /// <summary>
        ///     显示分类汇总的结果
        /// </summary>
        /// <param name="t">按一级科目的汇总</param>
        /// <param name="ts">按二级科目的汇总</param>
        /// <param name="tsc">按内容的汇总</param>
        /// <returns></returns>
        private static string PresentSubtotal(List<Balance> t, List<Balance> ts, List<Balance> tsc)
        {
            var sb = new StringBuilder();
            foreach (var balanceT in t)
            {
                var copiedT = balanceT;
                sb.AppendFormat(
                                "{0}-{1}:{2}",
                                copiedT.Title.AsTitle(),
                                TitleManager.GetTitleName(copiedT.Title).CPadRight(28),
                                copiedT.Fund.AsCurrency().CPadLeft(15));
                sb.AppendLine();
                foreach (var balanceS in ts.Where(sx => sx.Title == copiedT.Title))
                {
                    var copiedS = balanceS;
                    sb.AppendFormat(
                                    "  {0}-{1}:{2}   ({3:00.0%})",
                                    copiedS.SubTitle.HasValue ? copiedS.SubTitle.AsSubTitle() : "  ",
                                    TitleManager.GetTitleName(copiedS).CPadRight(28),
                                    copiedS.Fund.AsCurrency().CPadLeft(15),
                                    copiedS.Fund / copiedT.Fund);
                    sb.AppendLine();
                    foreach (var balanceC in tsc.Where(
                                                       cx => cx.Title == copiedS.Title
                                                             && cx.SubTitle == copiedS.SubTitle))
                    {
                        var copiedC = balanceC;
                        sb.AppendFormat(
                                        "        {0}:{1}   ({2:00.0%}, {3:00.0%})",
                                        copiedC.Content.CPadRight(25),
                                        copiedC.Fund.AsCurrency().CPadLeft(15),
                                        copiedC.Fund / copiedS.Fund,
                                        copiedC.Fund / copiedT.Fund);
                        sb.AppendLine();
                    }
                }
            }
            return sb.ToString();
        }

        /// <summary>
        ///     显示分类汇总的结果
        /// </summary>
        /// <param name="t">按一级科目的汇总</param>
        /// <param name="tsc">按内容的汇总</param>
        /// <returns></returns>
        private static string PresentSubtotal(List<Balance> t, List<Balance> tsc)
        {
            var sb = new StringBuilder();
            foreach (var balanceT in t)
            {
                var copiedT = balanceT;
                sb.AppendFormat(
                                "{0}-{1}:{2}",
                                copiedT.Title.AsTitle(),
                                TitleManager.GetTitleName(copiedT.Title).CPadRight(28),
                                copiedT.Fund.AsCurrency().CPadLeft(15));
                sb.AppendLine();
                foreach (var balanceC in tsc.Where(cx => cx.Title == copiedT.Title))
                {
                    var copiedC = balanceC;
                    sb.AppendFormat(
                                    "        {0}:{1}   ({2:00.0%})",
                                    copiedC.Content.CPadRight(25),
                                    copiedC.Fund.AsCurrency().CPadLeft(15),
                                    copiedC.Fund / copiedT.Fund);
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        /// <summary>
        ///     对记账凭证执行检索表达式
        /// </summary>
        /// <param name="s">检索表达式</param>
        /// <returns>检索结果，若表达式无效则为<c>null</c></returns>
        public IEnumerable<Voucher> ExecuteQuery(string s)
        {
            string dateQ;
            var detail = ParseQuery(s, out dateQ);

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

            if (startDate.HasValue ||
                endDate.HasValue ||
                nullable)
                return m_Accountant.SelectDetails(detail, startDate, endDate);
            return m_Accountant.SelectDetails(detail);
        }

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

        /// <summary>
        ///     将记账凭证用C#书写
        /// </summary>
        /// <param name="voucher">记账凭证</param>
        /// <returns>C#表达式</returns>
        private static string PresentVoucher(Voucher voucher)
        {
            var sb = new StringBuilder();
            sb.Append("new Voucher {");
            sb.AppendFormat("  ID = {0},", ProcessString(voucher.ID));
            sb.AppendLine();
            if (voucher.Date.HasValue)
            {
                sb.AppendFormat("    Date = DateTime.Parse(\"{0:yyyy-MM-dd}\"),", voucher.Date);
                sb.AppendLine();
            }
            else
                sb.AppendLine("    Date = null,");
            if ((voucher.Type ?? VoucherType.Ordinal) != VoucherType.Ordinal)
            {
                sb.AppendFormat("    Type = VoucherType.{0},", voucher.Type);
                sb.AppendLine();
            }
            if (voucher.Remark != null)
            {
                sb.AppendFormat("    Remark = {0},", ProcessString(voucher.Remark));
                sb.AppendLine();
            }
            sb.AppendLine("    Details = new[] {");
            foreach (var detail in voucher.Details)
            {
                sb.Append("        new VoucherDetail { ");
                sb.AppendFormat("Title = {0:0}, ", detail.Title);
                if (detail.SubTitle.HasValue)
                    sb.AppendFormat(
                                    "SubTitle = {0:00},    // {1}",
                                    detail.SubTitle,
                                    TitleManager.GetTitleName(detail));
                else
                    sb.AppendFormat("                  // {0}", TitleManager.GetTitleName(detail));
                sb.AppendLine();
                sb.Append("                            ");
                if (detail.Content != null)
                    sb.AppendFormat("Content = {0}, ", ProcessString(detail.Content));
                if (detail.Fund.HasValue)
                    sb.AppendFormat("Fund = {0}", detail.Fund);
                if (detail.Remark != null)
                    sb.AppendFormat(", Remark = {0}", ProcessString(detail.Remark));
                sb.AppendLine(" },");
                sb.AppendLine();
            }
            sb.AppendLine("   } }");
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
            if (result.Errors.HasErrors)
                throw new Exception(result.Errors[0].ToString());

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
