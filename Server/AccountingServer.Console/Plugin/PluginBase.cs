using AccountingServer.BLL;

namespace AccountingServer.Console.Plugin
{
    public abstract class PluginBase
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        protected readonly Accountant Accountant;

        protected PluginBase(Accountant accountant) { Accountant = accountant; }

        public abstract IQueryResult Execute(params string[] pars);
    }
}
