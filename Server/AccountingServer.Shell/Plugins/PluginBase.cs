using System;
using System.IO;
using System.Resources;
using AccountingServer.BLL;
using AccountingServer.Shell;

// ReSharper disable once CheckNamespace

namespace AccountingServer.Plugins
{
    /// <summary>
    ///     插件基类
    /// </summary>
    public abstract class PluginBase
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        protected readonly Accountant Accountant;

        protected PluginBase(Accountant accountant) { Accountant = accountant; }

        /// <summary>
        ///     执行插件表达式
        /// </summary>
        /// <param name="pars">参数</param>
        /// <returns>执行结果</returns>
        public abstract IQueryResult Execute(params string[] pars);

        /// <summary>
        ///     显示插件帮助
        /// </summary>
        /// <returns>帮助内容</returns>
        public string ListHelp()
        {
            var type = GetType();
            var resName = String.Format("{0}.Resources.Document.txt", type.Namespace);
            using (var stream = type.Assembly.GetManifestResourceStream(resName))
            {
                if (stream == null)
                    throw new MissingManifestResourceException();
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }
    }
}
