﻿using System;
using System.CodeDom.Compiler;
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
        ///     将记账凭证用C#表示
        /// </summary>
        /// <param name="voucher">记账凭证</param>
        /// <returns>C#表达式</returns>
        public static string PresentVoucher(Voucher voucher)
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
            return sb.ToString();
        }

        /// <summary>
        ///     从C#表达式中取得记账凭证
        /// </summary>
        /// <param name="str">C#表达式</param>
        /// <returns>记账凭证</returns>
        public static Voucher ParseVoucher(string str)
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
        ///     将资产用C#表示
        /// </summary>
        /// <param name="asset">资产</param>
        /// <returns>C#表达式</returns>
        public static string PresentAsset(Asset asset) { throw new NotImplementedException(); }

        /// <summary>
        ///     从C#表达式中取得资产
        /// </summary>
        /// <param name="str">C#表达式</param>
        /// <returns>资产</returns>
        public static Asset ParseAsset(string str)
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
            sb.AppendLine("    public static class AssetCreator");
            sb.AppendLine("    {");
            sb.AppendLine("        public static Asset GetAsset()");
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
                (Asset)
                resultAssembly.GetType("AccountingServer.Dynamic.AssetCreator")
                              .GetMethod("GetAsset")
                              .Invoke(null, null);
        }
    }
}