using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Shell.Parsing;
using AccountingServer.Shell.Plugin;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     插件表达式解释器
    /// </summary>
    public class PluginShell : IEnumerable
    {
        private readonly ICollection<PluginBase> m_Plugins = new List<PluginBase>();

        /// <summary>
        ///     添加插件
        /// </summary>
        /// <param name="plugin">插件</param>
        public void Add(PluginBase plugin) { m_Plugins.Add(plugin); }

        /// <summary>
        ///     调用插件
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        public IQueryResult ExecuteAuto(ShellParser.AutoCommandContext expr)
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

        /// <inheritdoc />
        public IEnumerator GetEnumerator() { return m_Plugins.GetEnumerator(); }
    }
}
