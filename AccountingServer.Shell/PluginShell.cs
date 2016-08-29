using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Plugins;
using AccountingServer.Shell.Parsing;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     插件表达式解释器
    /// </summary>
    public class PluginShell
    {
        private readonly Dictionary<string, PluginBase> m_Plugins;

        private readonly CustomManager<PluginInfos> m_Infos;

        public PluginShell(Accountant helper)
        {
            m_Infos = new CustomManager<PluginInfos>("Plugins.xml");
            m_Plugins = new Dictionary<string, PluginBase>();
            foreach (var info in m_Infos.Config.Infos)
            {
                var asm = AppDomain.CurrentDomain.Load(info.AssemblyName);
                var type = asm.GetType(info.ClassName);
                if (type == null)
                    throw new ApplicationException($"无法从{info.AssemblyName}中加载{info.ClassName}");
                m_Plugins.Add(
                              info.Alias,
                              (PluginBase)Activator.CreateInstance(type, helper));
            }
        }

        /// <summary>
        ///     根据名称检索插件
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>插件</returns>
        private PluginBase GetPlugin(string name) => m_Plugins[name];

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

        /// <summary>
        ///     列出所有插件
        /// </summary>
        /// <returns></returns>
        public string ListPlugins()
        {
            var sb = new StringBuilder();
            foreach (var info in m_Infos.Config.Infos)
                sb.AppendLine($"{info.Alias}\t{info.ClassName}\t{info.AssemblyName}");
            return sb.ToString();
        }
    }
}
