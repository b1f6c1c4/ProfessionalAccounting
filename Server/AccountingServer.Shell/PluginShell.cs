using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Plugins;
using AccountingServer.Shell.Parsing;

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
        public void Add(PluginBase plugin) => m_Plugins.Add(plugin);

        /// <summary>
        ///     根据名称检索插件
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>插件</returns>
        private PluginBase GetPlugin(string name)
        {
            foreach (var plg in from plg in m_Plugins
                                from attribute in Attribute.GetCustomAttributes(plg.GetType(), typeof(PluginAttribute))
                                let attr = (PluginAttribute)attribute
                                where attr.Alias.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                                select plg)
                return plg;
            throw new ArgumentException("没有找到与之对应的插件", nameof(name));
        }

        /// <summary>
        ///     调用插件
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        public IQueryResult ExecuteAuto(ShellParser.AutoCommandContext expr)
        {
            var name = expr.DollarQuotedString().Dequotation();
            var plugin = GetPlugin(name);
            return plugin.Execute(expr.SingleQuotedString().Select(n => n.Dequotation()).ToArray());
        }

        /// <summary>
        ///     显示插件帮助
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>帮助内容</returns>
        public string GetHelp(string name) => GetPlugin(name).ListHelp();

        /// <inheritdoc />
        public IEnumerator GetEnumerator() => m_Plugins.GetEnumerator();
    }
}
