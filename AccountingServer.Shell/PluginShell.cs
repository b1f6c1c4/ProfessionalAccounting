/* Copyright (C) 2020-2025 b1f6c1c4
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
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Shell.Plugins;
using AccountingServer.Shell.Plugins.AssetHelper;
using AccountingServer.Shell.Plugins.BankBalance;
using AccountingServer.Shell.Plugins.CashFlow;
using AccountingServer.Shell.Plugins.Cheque;
using AccountingServer.Shell.Plugins.Composite;
using AccountingServer.Shell.Plugins.Coupling;
using AccountingServer.Shell.Plugins.CreditCardConvert;
using AccountingServer.Shell.Plugins.Interest;
using AccountingServer.Shell.Plugins.Pivot;
using AccountingServer.Shell.Plugins.SpreadSheet;
using AccountingServer.Shell.Plugins.Statement;
using AccountingServer.Shell.Plugins.Utilities;
using AccountingServer.Shell.Plugins.YieldRate;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell;

/// <summary>
///     插件表达式解释器
/// </summary>
internal class PluginShell : IShellComponent
{
    private readonly Dictionary<string, PluginBase> m_Plugins;

    public PluginShell() => m_Plugins =
        new()
            {
                ["adb"] = new AverageDailyBalance(),
                ["af"] = new AssetFactory(),
                ["ad"] = new AssetDisposition(),
                ["ir"] = new InterestRevenue(),
                ["ie"] = new InterestExpense(),
                ["cf"] = new CashFlow(),
                ["c"] = new Composite(),
                ["ccc"] = new CreditCardConvert(),
                ["chq"] = new Cheque(),
                ["cp"] = new Coupling(),
                ["pvt"] = new Pivot(),
                ["ss"] = new SpreadSheet(),
                ["stmt"] = new Statement(),
                ["u"] = new Utilities(),
                ["yr"] = new YieldRate(),
            };

    /// <inheritdoc />
    public IAsyncEnumerable<string> Execute(string expr, Context ctx, string term)
    {
        var help = false;
        if (expr.StartsWith("?", StringComparison.Ordinal))
        {
            expr = expr[1..];
            help = true;
        }

        var plgName = Parsing.Quoted(ref expr, '$');

        if (help)
        {
            Parsing.Eof(expr);
            if (plgName == "")
                return ListPlugins().ToAsyncEnumerable();
            return GetHelp(plgName);
        }

        ctx.Identity.WillInvoke($"${plgName}$");
        return GetPlugin(plgName).Execute(expr, ctx);
    }

    /// <inheritdoc />
    public bool IsExecutable(string expr)
        => expr.StartsWith("$", StringComparison.Ordinal)
            || expr.StartsWith("?$", StringComparison.Ordinal);

    /// <summary>
    ///     根据名称检索插件
    /// </summary>
    /// <param name="name">名称</param>
    /// <returns>插件</returns>
    private PluginBase GetPlugin(string name) => m_Plugins[name];

    /// <summary>
    ///     显示插件帮助
    /// </summary>
    /// <param name="name">名称</param>
    /// <returns>帮助内容</returns>
    private IAsyncEnumerable<string> GetHelp(string name) => GetPlugin(name).ListHelp();

    /// <summary>
    ///     列出所有插件
    /// </summary>
    /// <returns>插件</returns>
    private IEnumerable<string> ListPlugins()
    {
        foreach (var (key, value) in m_Plugins)
            yield return $"{key,-8}{value.GetType().FullName}\n";
    }
}
