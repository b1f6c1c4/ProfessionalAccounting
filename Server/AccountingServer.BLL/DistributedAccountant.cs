using System;
using System.Linq;
using AccountingServer.DAL;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     ���ڻ��ҵ������
    /// </summary>
    internal abstract class DistributedAccountant
    {
        /// <summary>
        ///     ���ݿ����
        /// </summary>
        protected readonly IDbAdapter Db;

        protected DistributedAccountant(IDbAdapter db) { Db = db; }

        /// <summary>
        ///     ��ȡ���ڵ�ʣ��
        /// </summary>
        /// <param name="dist">����</param>
        /// <param name="dt">���ڣ���Ϊ<c>null</c>�򷵻��ܶ�</param>
        /// <returns>ָ�����ڵ��ܶ������������Ϊ<c>null</c></returns>
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
        ///     ���¼���ƾ֤��ϸĿ
        /// </summary>
        /// <param name="expected">Ӧ��ϸĿ</param>
        /// <param name="voucher">����ƾ֤</param>
        /// <param name="sucess">�Ƿ�ɹ�</param>
        /// <param name="modified">�Ƿ�����˼���ƾ֤</param>
        /// <param name="editOnly">�Ƿ�ֻ�������</param>
        protected static void UpdateDetail(VoucherDetail expected, Voucher voucher,
                                           out bool sucess, out bool modified, bool editOnly = false)
        {
            sucess = false;
            modified = false;

            if (!expected.Fund.HasValue)
                throw new ArgumentException("Ӧ��ϸĿ�Ľ��Ϊnull", "expected");
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
