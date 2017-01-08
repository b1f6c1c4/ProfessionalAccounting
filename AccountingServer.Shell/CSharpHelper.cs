﻿using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;
using Microsoft.CSharp;

namespace AccountingServer.Shell
{
    public static class CSharpHelper
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
            sb.AppendLine("namespace AccountingServer.Shell.Dynamic");
            sb.AppendLine("{");
            sb.AppendLine("    public static class ObjectCreator");
            sb.AppendLine("    {");
            sb.AppendLine("        private static DateTime D(string s)");
            sb.AppendLine("        {");
            sb.AppendLine("            return DateTime.Parse(s);");
            sb.AppendLine("        }");
            sb.AppendLine("        private static string G()");
            sb.AppendLine("        {");
            sb.AppendLine("            return Guid.NewGuid().ToString().ToUpper();");
            sb.AppendLine("        }");
            sb.AppendLine($"        public static {type.FullName} GetObject()");
            sb.AppendLine("        {");
            sb.AppendLine($"            return {str};");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            var result = provider.CompileAssemblyFromSource(paras, sb.ToString());
            if (result.Errors.HasErrors)
                throw new Exception(result.Errors[0].ToString());

            var resultAssembly = result.CompiledAssembly;
            return
                resultAssembly.GetType("AccountingServer.Shell.Dynamic.ObjectCreator")
                              .GetMethod("GetObject")
                              .Invoke(null, null);
        }

        #region Voucher

        /// <summary>
        ///     将细目用C#表示
        /// </summary>
        /// <param name="detail">细目</param>
        /// <returns>C#表达式</returns>
        public static string PresentVoucherDetail(VoucherDetail detail)
        {
            if (detail == null)
                return string.Empty;

            var sb = new StringBuilder();
            sb.Append("        new VoucherDetail { ");
            sb.Append($"Title = {detail.Title:0}, ");
            sb.Append(
                      detail.SubTitle.HasValue
                          ? $"SubTitle = {detail.SubTitle:00},    // {TitleManager.GetTitleName(detail)}"
                          : $"                  // {TitleManager.GetTitleName(detail)}");
            sb.AppendLine();
            sb.Append("                            ");
            if (detail.Content != null)
                sb.Append($"Content = {ProcessString(detail.Content)}, ");
            sb.Append(detail.Fund.HasValue ? $"Fund = {detail.Fund}" : "Fund = null");
            if (detail.Remark != null)
                sb.Append($", Remark = {ProcessString(detail.Remark)}");
            sb.AppendLine(" },");
            sb.AppendLine();
            return sb.ToString();
        }

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
            sb.Append($"  ID = {ProcessString(voucher.ID)},");
            sb.AppendLine();
            sb.AppendLine(voucher.Date.HasValue ? $"    Date = D(\"{voucher.Date:yyyy-MM-dd}\")," : "    Date = null,");
            if ((voucher.Type ?? VoucherType.Ordinary) != VoucherType.Ordinary)
                sb.AppendLine($"    Type = VoucherType.{voucher.Type},");
            if (voucher.Remark != null)
                sb.AppendLine($"    Remark = {ProcessString(voucher.Remark)},");
            if (voucher.Currency != Voucher.BaseCurrency)
                sb.AppendLine($"    Currency = {ProcessString(voucher.Currency)},");
            sb.AppendLine("    Details = new List<VoucherDetail> {");
            foreach (var detail in voucher.Details)
                sb.Append(PresentVoucherDetail(detail));
            sb.AppendLine("} }@");
            return sb.ToString();
        }

        /// <summary>
        ///     从C#表达式中取得记账凭证
        /// </summary>
        /// <param name="str">C#表达式</param>
        /// <returns>记账凭证</returns>
        public static Voucher ParseVoucher(string str) => (Voucher)ParseCSharp(str, typeof(Voucher));

        #endregion

        #region Asset

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
            sb.AppendLine($"  StringID = {ProcessString(asset.StringID)},");
            sb.AppendLine($"    Name = {ProcessString(asset.Name)},");
            sb.AppendLine(asset.Date.HasValue ? $"    Date = D(\"{asset.Date:yyyy-MM-dd}\")," : "    Date = null,");
            sb.AppendLine($"    Value = {asset.Value}, Salvge = {asset.Salvge}, Life = {asset.Life},");
            sb.AppendLine($"    Title = {asset.Title}, Method = DepreciationMethod.{asset.Method},");
            sb.AppendLine(
                          $"    DepreciationTitle = {asset.DepreciationTitle}, DepreciationExpenseTitle = {asset.DepreciationExpenseTitle}, DepreciationExpenseSubTitle = {asset.DepreciationExpenseSubTitle},");
            sb.AppendLine(
                          $"    DevaluationTitle = {asset.DevaluationTitle}, DevaluationExpenseTitle = {asset.DevaluationExpenseTitle}, DevaluationExpenseSubTitle = {asset.DevaluationExpenseSubTitle},");
            if (asset.Remark != null)
                sb.AppendLine($"    Remark = {ProcessString(asset.Remark)},");
            sb.AppendLine("    Schedule = new List<AssetItem> {");
            if (asset.Schedule != null)
            {
                Action<AssetItem, string> present =
                    (item, str) =>
                    {
                        sb.Append($"        new {item.GetType().Name.CPadRight(16)} {{ ");
                        sb.Append(item.Date.HasValue ? $"Date = D(\"{item.Date:yyyy-MM-dd}\"), " : "Date = null, ");
                        sb.Append($"VoucherID = {(ProcessString(item.VoucherID) + ",").CPadRight(27)}");
                        sb.Append(str.CPadRight(30));
                        sb.Append($"Value = {item.Value.ToString(CultureInfo.InvariantCulture).CPadRight(16)} ");
                        if (item.Remark != null)
                        {
                            sb.Append("".CPadLeft(30));
                            sb.Append($", Remark = {ProcessString(item.Remark)} ");
                        }
                        sb.AppendLine("},");
                    };

                foreach (var item in asset.Schedule)
                    if (item is AcquisationItem)
                        present(item, $"OrigValue = {(item as AcquisationItem).OrigValue},");
                    else if (item is DepreciateItem)
                        present(item, $"Amount    = {(item as DepreciateItem).Amount},");
                    else if (item is DevalueItem)
                        present(item, $"FairValue = {(item as DevalueItem).FairValue},");
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
        public static Asset ParseAsset(string str) => (Asset)ParseCSharp(str, typeof(Asset));

        #endregion

        #region Amort

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
            sb.Append($"  StringID = {ProcessString(amort.StringID)},");
            sb.AppendLine();
            sb.Append($"    Name = {ProcessString(amort.Name)},");
            sb.AppendLine();
            if (amort.Date.HasValue)
            {
                sb.Append($"    Date = D(\"{amort.Date:yyyy-MM-dd}\"),");
                sb.AppendLine();
            }
            else
                sb.AppendLine("    Date = null,");
            sb.Append($"    Value = {amort.Value}, ");
            sb.AppendLine();
            sb.Append($"    TotalDays = {amort.TotalDays}, Interval = AmortizeInterval.{amort.Interval},");
            sb.AppendLine();
            sb.Append("Template = ");
            sb.Append(PresentVoucher(amort.Template).Trim().Trim('@'));
            sb.AppendLine(",");
            if (amort.Remark != null)
            {
                sb.Append($"    Remark = {ProcessString(amort.Remark)},");
                sb.AppendLine();
            }
            if (amort.Schedule != null)
            {
                sb.AppendLine("    Schedule = new List<AmortItem> {");
                foreach (var item in amort.Schedule)
                {
                    sb.Append("        new AmortItem { ");
                    sb.Append(item.Date.HasValue ? $"Date = D(\"{item.Date:yyyy-MM-dd}\"), " : "Date = null, ");
                    sb.Append($"VoucherID = {(ProcessString(item.VoucherID) + ",").CPadRight(27)}");
                    sb.Append($"Amount = {(item.Amount.ToString(CultureInfo.InvariantCulture) + ",").CPadRight(19)}");
                    sb.Append($"Value = {item.Value.ToString(CultureInfo.InvariantCulture).CPadRight(16)} ");
                    if (item.Remark != null)
                    {
                        sb.Append("".CPadLeft(30));
                        sb.Append($", Remark = {ProcessString(item.Remark)} ");
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
        public static Amortization ParseAmort(string str) => (Amortization)ParseCSharp(str, typeof(Amortization));

        #endregion
    }
}