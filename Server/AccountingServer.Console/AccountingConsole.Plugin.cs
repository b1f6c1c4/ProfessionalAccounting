using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Console.Plugin;

namespace AccountingServer.Console
{
    public partial class AccountingConsole
    {
        private readonly ICollection<PluginBase> m_Plugins = new List<PluginBase>();

        public void AddPlugin(PluginBase plg) { m_Plugins.Add(plg); }

        /// <summary>
        ///     调用插件
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteAuto(ConsoleParser.AutoCommandContext expr)
        {
            var name = expr.DollarQuotedString().Dequotation();
            foreach (var plg in from plg in m_Plugins
                                from attribute in Attribute.GetCustomAttributes(plg.GetType(), typeof(PluginAttribute))
                                let attr = (PluginAttribute)attribute
                                where attr.Alias.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                                select plg)
                return plg.Execute(expr.SingleQuotedString().Select(n => n.Dequotation()).ToArray());
            throw new Exception();
        }
    }
}
