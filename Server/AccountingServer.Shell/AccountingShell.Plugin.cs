using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Shell.Parsing;
using AccountingServer.Shell.Plugin;

namespace AccountingServer.Shell
{
    public partial class AccountingShell
    {
        private readonly ICollection<PluginBase> m_Plugins = new List<PluginBase>();

        /// <summary>
        ///     添加插件
        /// </summary>
        /// <param name="plg">插件</param>
        public void AddPlugin(PluginBase plg) { m_Plugins.Add(plg); }

        /// <summary>
        ///     调用插件
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteAuto(ShellParser.AutoCommandContext expr)
        {
            var name = expr.DollarQuotedString().Dequotation();
            foreach (var plg in from plg in m_Plugins
                                from attribute in Attribute.GetCustomAttributes(plg.GetType(), typeof(PluginAttribute))
                                let attr = (PluginAttribute)attribute
                                where attr.Alias.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                                select plg)
                return plg.Execute(expr.SingleQuotedString().Select(n => n.Dequotation()).ToArray());
            throw new ArgumentException("没有找到与之对应的插件", "expr");
        }
    }
}
