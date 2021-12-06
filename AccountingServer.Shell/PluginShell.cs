﻿using System;
using System.Collections.Generic;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Shell.Plugins;
using AccountingServer.Shell.Plugins.AssetHelper;
using AccountingServer.Shell.Plugins.BankBalance;
using AccountingServer.Shell.Plugins.CashFlow;
using AccountingServer.Shell.Plugins.Composite;
using AccountingServer.Shell.Plugins.CreditCardConvert;
using AccountingServer.Shell.Plugins.Interest;
using AccountingServer.Shell.Plugins.Utilities;
using AccountingServer.Shell.Plugins.YieldRate;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     插件表达式解释器
    /// </summary>
    internal class PluginShell : IShellComponent
    {
        private readonly Dictionary<string, PluginBase> m_Plugins;

        public PluginShell(Accountant helper) => m_Plugins =
            new Dictionary<string, PluginBase>
                {
                    ["adb"] = new AverageDailyBalance(helper),
                    ["af"] = new AssetFactory(helper),
                    ["ad"] = new AssetDisposition(helper),
                    ["ir"] = new InterestRevenue(helper),
                    ["cf"] = new CashFlow(helper),
                    ["c"] = new Composite(helper),
                    ["ccc"] = new CreditCardConvert(helper),
                    ["u"] = new Utilities(helper),
                    ["yr"] = new YieldRate(helper)
                };

        /// <inheritdoc />
        public IQueryResult Execute(string expr, IEntitiesSerializer serializer)
        {
            var help = false;
            if (expr.StartsWith("?", StringComparison.Ordinal))
            {
                expr = expr.Substring(1);
                help = true;
            }

            var plgName = Parsing.Quoted(ref expr, '$');

            if (help)
            {
                Parsing.Eof(expr);
                return plgName == "" ? new UnEditableText(ListPlugins()) : new UnEditableText(GetHelp(plgName));
            }

            return GetPlugin(plgName).Execute(expr, serializer);
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
        private string GetHelp(string name) => GetPlugin(name).ListHelp();

        /// <summary>
        ///     列出所有插件
        /// </summary>
        /// <returns></returns>
        private string ListPlugins()
        {
            var sb = new StringBuilder();
            foreach (var info in m_Plugins)
                sb.AppendLine($"{info.Key,-8}{info.Value.GetType().FullName}");

            return sb.ToString();
        }
    }
}
