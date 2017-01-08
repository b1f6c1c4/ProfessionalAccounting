using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell;
using static AccountingServer.BLL.Parsing.FacadeF;

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
        public virtual string ListHelp()
        {
            var type = GetType();
            var resName = $"{type.Namespace}.Resources.Document.txt";
            using (var stream = type.Assembly.GetManifestResourceStream(resName))
            {
                if (stream == null)
                    throw new MissingManifestResourceException();
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        /// <summary>
        ///     执行值分类汇总检索式
        /// </summary>
        /// <param name="query">值分类汇总检索式</param>
        /// <returns>汇总结果</returns>
        protected double GetV(string query) =>
            Accountant.SelectVoucherDetailsGrouped(ParsingF.GroupedQuery(query)).SingleOrDefault()?.Fund ?? 0;

        /// <summary>
        ///     执行记账凭证检索式
        /// </summary>
        /// <param name="query">记账凭证检索式</param>
        /// <returns>记账凭证</returns>
        protected IEnumerable<Voucher> Gets(string query) =>
            Accountant.SelectVouchers(ParsingF.VoucherQuery(query));
    }
}
