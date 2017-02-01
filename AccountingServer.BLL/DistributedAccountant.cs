using System;
using System.Linq;
using AccountingServer.DAL;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     分期会计业务处理类
    /// </summary>
    internal abstract class DistributedAccountant
    {
        /// <summary>
        ///     数据库访问
        /// </summary>
        protected readonly IDbAdapter Db;

        protected DistributedAccountant(IDbAdapter db) { Db = db; }

        /// <summary>
        ///     获取分期的剩余
        /// </summary>
        /// <param name="dist">分期</param>
        /// <param name="dt">日期，若为<c>null</c>则返回总额</param>
        /// <returns>指定日期的总额，若早于日期则为<c>null</c></returns>
        public static double? GetBookValueOn(IDistributed dist, DateTime? dt)
        {
            if (!dt.HasValue ||
                dist.TheSchedule == null)
                return dist.Value;

            var last = dist.TheSchedule.LastOrDefault(item => DateHelper.CompareDate(item.Date, dt) <= 0);
            if (last != null)
                return last.Value;
            if (DateHelper.CompareDate(dist.Date, dt) <= 0)
                return dist.Value;

            return null;
        }

        /// <summary>
        ///     更新记账凭证的细目
        /// </summary>
        /// <param name="expected">应填细目</param>
        /// <param name="voucher">记账凭证</param>
        /// <param name="sucess">是否成功</param>
        /// <param name="modified">是否更改了记账凭证</param>
        /// <param name="editOnly">是否只允许更新</param>
        protected static void UpdateDetail(VoucherDetail expected, Voucher voucher,
            out bool sucess, out bool modified, bool editOnly = false)
        {
            sucess = false;
            modified = false;

            if (!expected.Fund.HasValue)
                throw new ArgumentException("应填细目的金额为null", nameof(expected));

            var fund = expected.Fund.Value;
            expected.Fund = null;
            var isEliminated = fund.IsZero();

            var ds = voucher.Details.Where(d => d.IsMatch(expected)).ToList();

            expected.Fund = fund;

            if (ds.Count == 0)
            {
                if (isEliminated)
                {
                    sucess = true;
                    return;
                }

                if (editOnly)
                    return;

                voucher.Details.Add(expected);
                sucess = true;
                modified = true;
                return;
            }

            if (ds.Count > 1)
                return;

            if (isEliminated)
            {
                if (editOnly)
                    return;

                voucher.Details.Remove(ds[0]);
                sucess = true;
                modified = true;
                return;
            }

            // ReSharper disable once PossibleInvalidOperationException
            if ((ds[0].Fund.Value - fund).IsZero())
            {
                sucess = true;
                return;
            }

            ds[0].Fund = fund;
            modified = true;
        }
    }
}
