using System.Collections.Generic;
using AccountingServer.Entities;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     数据库分类汇总访问接口
    /// </summary>
    public interface IDbGrouping
    {
        /// <summary>
        ///     按过滤器查找记账凭证，并进行分类汇总
        /// </summary>
        /// <param name="vfilter">过滤器</param>
        /// <param name="filters">细目过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="dir">+1表示只考虑借方，-1表示只考虑贷方，0表示同时考虑借方和贷方</param>
        /// <param name="useAnd">各细目过滤器之间的关系为合取</param>
        /// <param name="subtotalLevel">要进行分类汇总的层级</param>
        /// <returns>匹配过滤器的细目</returns>
        IEnumerable<Balance> FilteredGroupingAllDetails(Voucher vfilter = null,
                                                        IEnumerable<VoucherDetail> filters = null,
                                                        DateFilter? rng = null,
                                                        int dir = 0,
                                                        bool useAnd = false,
                                                        SubtotalLevel subtotalLevel = SubtotalLevel.None);

        /// <summary>
        ///     按过滤器查找细目，并进行分类汇总
        /// </summary>
        /// <param name="vfilter">过滤器</param>
        /// <param name="filters">细目过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="dir">+1表示只考虑借方，-1表示只考虑贷方，0表示同时考虑借方和贷方</param>
        /// <param name="useAnd">各细目过滤器之间的关系为合取</param>
        /// <param name="subtotalLevel">要进行分类汇总的层级</param>
        /// <returns>匹配过滤器的细目</returns>
        IEnumerable<Balance> FilteredGroupingDetails(Voucher vfilter = null,
                                                     IEnumerable<VoucherDetail> filters = null,
                                                     DateFilter? rng = null,
                                                     int dir = 0,
                                                     bool useAnd = false,
                                                     SubtotalLevel subtotalLevel = SubtotalLevel.None);
    }
}
