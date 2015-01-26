using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;
using Microsoft.CSharp;

namespace AccountingServer.Console
{
    internal static class CSharpHelper
    {
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

        /// <summary>
        ///     从C#表达式中取得对象
        /// </summary>
        /// <param name="str">C#表达式</param>
        /// <param name="type">对象类型</param>
        /// <returns>对象</returns>
        private static object ParseCSharp(string str, Type type)
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
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using AccountingServer.Entities;");
            sb.AppendLine("namespace AccountingServer.Console.Dynamic");
            sb.AppendLine("{");
            sb.AppendLine("    public static class ObjectCreator");
            sb.AppendLine("    {");
            sb.AppendLine("        private static DateTime D(string s)");
            sb.AppendLine("        {");
            sb.AppendLine("            return DateTime.Parse(s);");
            sb.AppendLine("        }");
            sb.AppendFormat("        public static {0} GetObject()", type.FullName);
            sb.AppendLine();
            sb.AppendLine("        {");
            sb.AppendFormat("            return {0};", str);
            sb.AppendLine();
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            var result = provider.CompileAssemblyFromSource(paras, sb.ToString());
            if (result.Errors.HasErrors)
                throw new Exception(result.Errors[0].ToString());

            var resultAssembly = result.CompiledAssembly;
            return
                resultAssembly.GetType("AccountingServer.Console.Dynamic.ObjectCreator")
                              .GetMethod("GetObject")
                              .Invoke(null, null);
        }

        #region voucher

        /// <summary>
        ///     将记账凭证用C#表示
        /// </summary>
        /// <param name="voucher">记账凭证</param>
        /// <returns>C#表达式</returns>
        public static string PresentVoucher(Voucher voucher)
        {
            if (voucher == null)
                return "@null@";

            var sb = new StringBuilder();
            sb.Append("@new Voucher {");
            sb.AppendFormat("  ID = {0},", ProcessString(voucher.ID));
            sb.AppendLine();
            if (voucher.Date.HasValue)
            {
                sb.AppendFormat("    Date = D(\"{0:yyyy-MM-dd}\"),", voucher.Date);
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
            sb.AppendLine("    Details = new List<VoucherDetail> {");
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
            sb.AppendLine("} }@");
            return sb.ToString();
        }

        /// <summary>
        ///     从C#表达式中取得记账凭证
        /// </summary>
        /// <param name="str">C#表达式</param>
        /// <returns>记账凭证</returns>
        public static Voucher ParseVoucher(string str)
        {
            return (Voucher)ParseCSharp(str, typeof(Voucher));
        }

        #endregion

        #region asset

        /// <summary>
        ///     将资产用C#表示
        /// </summary>
        /// <param name="asset">资产</param>
        /// <returns>C#表达式</returns>
        public static string PresentAsset(Asset asset)
        {
            if (asset == null)
                return "@null@";

            var sb = new StringBuilder();
            sb.Append("@new Asset {");
            sb.AppendFormat("  StringID = {0},", ProcessString(asset.StringID));
            sb.AppendLine();
            sb.AppendFormat("    Name = {0},", ProcessString(asset.Name));
            sb.AppendLine();
            if (asset.Date.HasValue)
            {
                sb.AppendFormat("    Date = D(\"{0:yyyy-MM-dd}\"),", asset.Date);
                sb.AppendLine();
            }
            else
                sb.AppendLine("    Date = null,");
            sb.AppendFormat("    Value = {0}, Salvge = {1}, Life = {2},", asset.Value, asset.Salvge, asset.Life);
            sb.AppendLine();
            sb.AppendFormat("    Title = {0}, Method = DepreciationMethod.{1},", asset.Title, asset.Method);
            sb.AppendLine();
            sb.AppendFormat(
                            "    DepreciationTitle = {0}, DepreciationExpenseTitle = {1}, DepreciationExpenseSubTitle = {2},",
                            asset.DepreciationTitle,
                            asset.DepreciationExpenseTitle,
                            asset.DepreciationExpenseSubTitle);
            sb.AppendLine();
            sb.AppendFormat(
                            "    DevaluationTitle = {0}, DevaluationExpenseTitle = {1}, DevaluationExpenseSubTitle = {2},",
                            asset.DevaluationTitle,
                            asset.DevaluationExpenseTitle,
                            asset.DevaluationExpenseSubTitle);
            sb.AppendLine();
            if (asset.Remark != null)
            {
                sb.AppendFormat("    Remark = {0},", ProcessString(asset.Remark));
                sb.AppendLine();
            }
            sb.AppendLine("    Schedule = new List<AssetItem> {");
            if (asset.Schedule != null)
            {
                Action<AssetItem, string> present =
                    (item, str) =>
                    {
                        sb.Append("        new ");
                        sb.Append(item.GetType().Name.PadRight(16));
                        sb.Append("{ ");
                        if (item.Date.HasValue)
                            sb.AppendFormat(
                                            "Date = D(\"{0:yyyy-MM-dd}\"), ",
                                            item.Date);
                        else
                            sb.Append("Date = null, ");
                        sb.AppendFormat("VoucherID = {0}", (ProcessString(item.VoucherID) + ",").PadRight(27));
                        sb.Append(str.PadRight(30));
                        sb.AppendFormat(
                                        "BookValue = {0} ",
                                        item.BookValue.ToString(CultureInfo.InvariantCulture).PadRight(16));
                        if (item.Remark != null)
                        {
                            sb.Append("".PadLeft(30));
                            sb.AppendFormat(", Remark = {0} ", ProcessString(item.Remark));
                        }
                        sb.AppendLine("},");
                    };

                foreach (var item in asset.Schedule)
                    if (item is AcquisationItem)
                        present(item, String.Format("OrigValue = {0},", (item as AcquisationItem).OrigValue));
                    else if (item is DepreciateItem)
                        present(item, String.Format("Amount    = {0},", (item as DepreciateItem).Amount));
                    else if (item is DevalueItem)
                        present(item, String.Format("FairValue = {0},", (item as DevalueItem).FairValue));
                    else if (item is DispositionItem)
                        present(item, "");
                sb.AppendLine("} }@");
            }
            else
                sb.AppendLine("}@");
            return sb.ToString();
        }

        /// <summary>
        ///     从C#表达式中取得资产
        /// </summary>
        /// <param name="str">C#表达式</param>
        /// <returns>资产</returns>
        public static Asset ParseAsset(string str)
        {
            return (Asset)ParseCSharp(str, typeof(Asset));
        }

        #endregion

        #region amort

        /// <summary>
        ///     将摊销用C#表示
        /// </summary>
        /// <param name="amort">摊销</param>
        /// <returns>C#表达式</returns>
        public static string PresentAmort(Amortization amort)
        {
            if (amort == null)
                return "@null@";

            var sb = new StringBuilder();
            sb.Append("@new Amortization {");
            sb.AppendFormat("  StringID = {0},", ProcessString(amort.StringID));
            sb.AppendLine();
            sb.AppendFormat("    Name = {0},", ProcessString(amort.Name));
            sb.AppendLine();
            if (amort.Date.HasValue)
            {
                sb.AppendFormat("    Date = D(\"{0:yyyy-MM-dd}\"),", amort.Date);
                sb.AppendLine();
            }
            else
                sb.AppendLine("    Date = null,");
            sb.AppendFormat("    Value = {0}, ", amort.Value);
            sb.AppendLine();
            sb.AppendFormat("    TotalDays = {0}, Interval = AmortizeInterval.{1},", amort.TotalDays, amort.Interval);
            sb.AppendLine();
            sb.Append("Template = ");
            sb.Append(PresentVoucher(amort.Template).Trim().Trim('@'));
            sb.AppendLine(",");
            if (amort.Remark != null)
            {
                sb.AppendFormat("    Remark = {0},", ProcessString(amort.Remark));
                sb.AppendLine();
            }
            if (amort.Schedule != null)
            {
                sb.AppendLine("    Schedule = new List<AmortItem> {");
                foreach (var item in amort.Schedule)
                {
                    sb.Append("        new AmortItem { ");
                    if (item.Date.HasValue)
                        sb.AppendFormat(
                                        "Date = D(\"{0:yyyy-MM-dd}\"), ",
                                        item.Date);
                    else
                        sb.Append("Date = null, ");
                    sb.AppendFormat("VoucherID = {0}", (ProcessString(item.VoucherID) + ",").PadRight(27));
                    sb.AppendFormat(
                                    "Amount = {0}",
                                    (item.Amount.ToString(CultureInfo.InvariantCulture) + ",").PadRight(19));
                    sb.AppendFormat(
                                    "Residue = {0} ",
                                    item.Residue.ToString(CultureInfo.InvariantCulture).PadRight(16));
                    if (item.Remark != null)
                    {
                        sb.Append("".PadLeft(30));
                        sb.AppendFormat(", Remark = {0} ", ProcessString(item.Remark));
                    }
                    sb.AppendLine("},");
                }
                sb.AppendLine("} }@");
            }
            else
            {
                sb.AppendLine("    Schedule = null");
                sb.AppendLine("}@");
            }
            return sb.ToString();
        }

        /// <summary>
        ///     从C#表达式中取得摊销
        /// </summary>
        /// <param name="str">C#表达式</param>
        /// <returns>摊销</returns>
        public static Amortization ParseAmort(string str)
        {
            return (Amortization)ParseCSharp(str, typeof(Amortization));
        }

        #endregion
    }
}
