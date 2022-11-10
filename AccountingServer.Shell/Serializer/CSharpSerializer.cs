/* Copyright (C) 2020-2022 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.Loader;
using System.Text;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AccountingServer.Shell.Serializer;

public class CSharpSerializer : IEntitySerializer
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

        s = s.Replace("\"", "\"\"");
        return $"@\"{s}\"";
    }

    /// <summary>
    ///     从C#表达式中取得对象
    /// </summary>
    /// <param name="str">C#表达式</param>
    /// <param name="type">对象类型</param>
    /// <returns>对象</returns>
    private static object ParseCSharp(string str, Type type)
    {
        var sb = new StringBuilder();
        sb.Append("using System;\n");
        sb.Append("using System.Collections.Generic;\n");
        sb.Append("using AccountingServer.Entities;\n");
        sb.Append("namespace AccountingServer.Shell.Dynamic\n");
        sb.Append("{\n");
        sb.Append("    public static class ObjectCreator\n");
        sb.Append("    {\n");
        sb.Append("        private static DateTime D(string s)\n");
        sb.Append("        {\n");
        sb.Append("            return DateTime.Parse(s+\"Z\").ToUniversalTime();\n");
        sb.Append("        }\n");
        sb.Append("        private static string G()\n");
        sb.Append("        {\n");
        sb.Append("            return Guid.NewGuid().ToString().ToUpperInvariant();\n");
        sb.Append("        }\n");
        sb.Append($"        public static {type.FullName} GetObject()\n");
        sb.Append("        {\n");
        sb.Append($"            return {str};\n");
        sb.Append("        }\n");
        sb.Append("    }\n");
        sb.Append("}\n");

        var syntaxTree = CSharpSyntaxTree.ParseText(sb.ToString());
        var assemblyName = Path.GetRandomFileName();

        var refPaths = new[]
            {
                typeof(object).GetTypeInfo().Assembly.Location,
                typeof(Console).GetTypeInfo().Assembly.Location,
                Path.Combine(
                    Path.GetDirectoryName(typeof(GCSettings).GetTypeInfo().Assembly.Location)!,
                    "System.Runtime.dll"),
                Path.Combine(Path.GetDirectoryName(typeof(IList).GetTypeInfo().Assembly.Location)!,
                    "System.Collections.dll"),
                Path.Combine(Path.GetDirectoryName(typeof(Voucher).GetTypeInfo().Assembly.Location)!,
                    "AccountingServer.Entities.dll"),
            };
        var references = refPaths.Select(static r => MetadataReference.CreateFromFile(r)).ToArray();
        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { syntaxTree },
            references,
            new(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();

        var result = compilation.Emit(ms);
        if (!result.Success)
        {
            var failure = result.Diagnostics.First(static diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error);
            throw new(failure.GetMessage());
        }

        ms.Seek(0, SeekOrigin.Begin);

        var resultAssembly = AssemblyLoadContext.Default.LoadFromStream(ms);
        return
            resultAssembly.GetType("AccountingServer.Shell.Dynamic.ObjectCreator")!
                .GetMethod("GetObject")
                ?.Invoke(null, null);
    }

    #region Voucher

    /// <inheritdoc />
    public string PresentVoucherDetail(VoucherDetail detail)
    {
        if (detail == null)
            return string.Empty;

        var sb = new StringBuilder();
        sb.Append("        new VoucherDetail { ");
        sb.Append($"User = {ProcessString(detail.User)}, ");
        sb.Append($"Currency = {ProcessString(detail.Currency)},\n");
        sb.Append("                            ");
        sb.Append($"Title = {detail.Title:0}, ");
        sb.Append(
            detail.SubTitle.HasValue
                ? $"SubTitle = {detail.SubTitle:00},    // {TitleManager.GetTitleName(detail)}"
                : $"                  // {TitleManager.GetTitleName(detail)}");
        sb.Append("\n");
        sb.Append("                            ");
        if (detail.Content != null)
            sb.Append($"Content = {ProcessString(detail.Content)}, ");
        sb.Append(detail.Fund.HasValue ? $"Fund = {detail.Fund}" : "Fund = null");
        if (detail.Remark != null)
            sb.Append($", Remark = {ProcessString(detail.Remark)}");
        sb.Append(" },\n");
        sb.Append("\n");
        return sb.ToString();
    }

    /// <inheritdoc />
    public string PresentVoucherDetail(VoucherDetailR detail)
        => PresentVoucherDetail((VoucherDetail)detail);

    /// <inheritdoc />
    public string PresentVoucher(Voucher voucher)
    {
        if (voucher == null)
            return "new Voucher {\n\n}";

        var sb = new StringBuilder();
        sb.Append("new Voucher {");
        sb.Append($"  ID = {ProcessString(voucher.ID)},");
        sb.Append("\n");
        sb.Append(voucher.Date.HasValue ? $"    Date = D(\"{voucher.Date:yyyy-MM-dd}\"),\n" : "    Date = null,\n");
        if ((voucher.Type ?? VoucherType.Ordinary) != VoucherType.Ordinary)
            sb.Append($"    Type = VoucherType.{voucher.Type},\n");
        if (voucher.Remark != null)
            sb.Append($"    Remark = {ProcessString(voucher.Remark)},\n");
        sb.Append("    Details = new() {\n");
        foreach (var detail in voucher.Details)
            sb.Append(PresentVoucherDetail(detail));

        sb.Append("} }");
        return sb.ToString();
    }

    /// <inheritdoc />
    public Voucher ParseVoucher(string str)
    {
        try
        {
            return (Voucher)ParseCSharp(str, typeof(Voucher));
        }
        catch (Exception e)
        {
            throw new FormatException("格式错误", e);
        }
    }

    /// <inheritdoc />
    public VoucherDetail ParseVoucherDetail(string str)
    {
        try
        {
            return (VoucherDetail)ParseCSharp(str, typeof(VoucherDetail));
        }
        catch (Exception e)
        {
            throw new FormatException("格式错误", e);
        }
    }

    #endregion

    #region Asset

    /// <inheritdoc />
    public string PresentAsset(Asset asset)
    {
        if (asset == null)
            return "null";

        var sb = new StringBuilder();
        sb.Append("new Asset {");
        sb.Append($"  StringID = {ProcessString(asset.StringID)},\n");
        sb.Append($"    User = {ProcessString(asset.User)}, \n");
        sb.Append($"    Name = {ProcessString(asset.Name)},\n");
        sb.Append(asset.Date.HasValue ? $"    Date = D(\"{asset.Date:yyyy-MM-dd}\"),\n" : "    Date = null,\n");
        sb.Append($"    Currency = {ProcessString(asset.Currency)},\n");
        sb.Append($"    Value = {asset.Value}, Salvage = {asset.Salvage}, Life = {asset.Life},\n");
        sb.Append($"    Title = {asset.Title}, Method = DepreciationMethod.{asset.Method},\n");
        sb.Append(
            $"    DepreciationTitle = {asset.DepreciationTitle}, DepreciationExpenseTitle = {asset.DepreciationExpenseTitle}, DepreciationExpenseSubTitle = {asset.DepreciationExpenseSubTitle},\n");
        sb.Append(
            $"    DevaluationTitle = {asset.DevaluationTitle}, DevaluationExpenseTitle = {asset.DevaluationExpenseTitle}, DevaluationExpenseSubTitle = {asset.DevaluationExpenseSubTitle},\n");
        if (asset.Remark != null)
            sb.Append($"    Remark = {ProcessString(asset.Remark)},\n");
        sb.Append("    Schedule = new() {\n");
        if (asset.Schedule != null)
        {
            void Present(AssetItem item, string str)
            {
                sb.Append($"        new {item.GetType().Name.CPadRight(16)} {{ ");
                sb.Append(item.Date.HasValue ? $"Date = D(\"{item.Date:yyyy-MM-dd}\"), " : "Date = null, ");
                sb.Append($"VoucherID = {(ProcessString(item.VoucherID) + ",").CPadRight(28)}");
                sb.Append(str.CPadRight(30));
                sb.Append($"Value = {item.Value.ToString(CultureInfo.InvariantCulture).CPadRight(16)} ");
                if (item.Remark != null)
                {
                    sb.Append("".CPadLeft(30));
                    sb.Append($", Remark = {ProcessString(item.Remark)} ");
                }

                sb.Append("},\n");
            }

            foreach (var item in asset.Schedule)
                Present(item, item switch
                    {
                        AcquisitionItem acq => $"OrigValue = {acq.OrigValue},",
                        DepreciateItem dep => $"Amount    = {dep.Amount},",
                        DevalueItem dev => $"FairValue = {dev.FairValue}, Amount = {dev.Amount},",
                        DispositionItem => "",
                        _ => throw new InvalidOperationException(),
                    });

            sb.Append("} }");
        }
        else
            sb.Append('}');

        return sb.ToString();
    }

    /// <inheritdoc />
    public Asset ParseAsset(string str)
    {
        try
        {
            return (Asset)ParseCSharp(str, typeof(Asset));
        }
        catch (Exception e)
        {
            throw new FormatException("格式错误", e);
        }
    }

    #endregion

    #region Amort

    /// <inheritdoc />
    public string PresentAmort(Amortization amort)
    {
        if (amort == null)
            return "null";

        var sb = new StringBuilder();
        sb.Append("new Amortization {");
        sb.Append($"  StringID = {ProcessString(amort.StringID)},\n");
        sb.Append($"    User = {ProcessString(amort.User)}, \n");
        sb.Append($"    Name = {ProcessString(amort.Name)},\n");
        if (amort.Date.HasValue)
        {
            sb.Append($"    Date = D(\"{amort.Date:yyyy-MM-dd}\"),");
            sb.Append("\n");
        }
        else
            sb.Append("    Date = null,\n");

        sb.Append($"    Value = {amort.Value}, \n");
        sb.Append($"    TotalDays = {amort.TotalDays}, Interval = AmortizeInterval.{amort.Interval},\n");
        sb.Append("Template = ");
        sb.Append(PresentVoucher(amort.Template));
        sb.Append(",\n");
        if (amort.Remark != null)
            sb.Append($"    Remark = {ProcessString(amort.Remark)},\n");

        if (amort.Schedule != null)
        {
            sb.Append("    Schedule = new() {\n");
            foreach (var item in amort.Schedule)
            {
                sb.Append("        new AmortItem { ");
                sb.Append(item.Date.HasValue ? $"Date = D(\"{item.Date:yyyy-MM-dd}\"), " : "Date = null, ");
                sb.Append($"VoucherID = {(ProcessString(item.VoucherID) + ",").CPadRight(28)}");
                sb.Append($"Amount = {(item.Amount.ToString(CultureInfo.InvariantCulture) + ",").CPadRight(19)}");
                sb.Append($"Value = {item.Value.ToString(CultureInfo.InvariantCulture).CPadRight(16)} ");
                if (item.Remark != null)
                {
                    sb.Append("".CPadLeft(30));
                    sb.Append($", Remark = {ProcessString(item.Remark)} ");
                }

                sb.Append("},\n");
            }

            sb.Append("} }");
        }
        else
        {
            sb.Append("    Schedule = null\n");
            sb.Append('}');
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public Amortization ParseAmort(string str)
    {
        try
        {
            return (Amortization)ParseCSharp(str, typeof(Amortization));
        }
        catch (Exception e)
        {
            throw new FormatException("格式错误", e);
        }
    }

    #endregion
}
