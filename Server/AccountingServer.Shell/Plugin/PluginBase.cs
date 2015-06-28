using AccountingServer.BLL;
using AccountingServer.Shell;

// ReSharper disable once CheckNamespace
namespace AccountingServer.Plugin
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
    }
}
