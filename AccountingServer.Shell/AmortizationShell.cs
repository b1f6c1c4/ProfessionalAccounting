using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     摊销表达式解释器
    /// </summary>
    internal class AmortizationShell : DistributedShell
    {
        public AmortizationShell(Accountant helper, IEntitySerializer serializer) : base(helper, serializer) { }

        /// <inheritdoc />
        protected override string Initial => "o";

        /// <summary>
        ///     执行列表表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="dt">计算账面价值的时间</param>
        /// <param name="showSchedule">是否显示折旧计算表</param>
        /// <returns>执行结果</returns>
        protected override IQueryResult ExecuteList(IQueryCompunded<IDistributedQueryAtom> distQuery, DateTime? dt,
            bool showSchedule)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(Accountant.SelectAmortizations(distQuery)))
                sb.Append(ListAmort(a, dt, showSchedule));

            if (showSchedule)
                return new EditableText(sb.ToString());

            return new UnEditableText(sb.ToString());
        }

        /// <inheritdoc />
        protected override IQueryResult ExecuteQuery(IQueryCompunded<IDistributedQueryAtom> distQuery)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(Accountant.SelectAmortizations(distQuery)))
                sb.Append(Serializer.PresentAmort(a));

            return new EditableText(sb.ToString());
        }

        /// <inheritdoc />
        protected override IQueryResult ExecuteRegister(IQueryCompunded<IDistributedQueryAtom> distQuery, DateFilter rng,
            IQueryCompunded<IVoucherQueryAtom> query)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(Accountant.SelectAmortizations(distQuery)))
            {
                foreach (var voucher in Accountant.RegisterVouchers(a, rng, query))
                    sb.Append(Serializer.PresentVoucher(voucher));

                Accountant.Upsert(a);
            }

            if (sb.Length > 0)
                return new EditableText(sb.ToString());

            return new Succeed();
        }

        /// <inheritdoc />
        protected override IQueryResult ExecuteUnregister(IQueryCompunded<IDistributedQueryAtom> distQuery,
            DateFilter rng,
            IQueryCompunded<IVoucherQueryAtom> query)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(Accountant.SelectAmortizations(distQuery)))
            {
                foreach (var item in a.Schedule.Where(item => item.Date.Within(rng)))
                {
                    if (query != null)
                    {
                        if (item.VoucherID == null)
                            continue;

                        var voucher = Accountant.SelectVoucher(item.VoucherID);
                        if (voucher != null)
                            if (!MatchHelper.IsMatch(query, voucher.IsMatch))
                                continue;
                    }

                    item.VoucherID = null;
                }

                sb.Append(ListAmort(a));
                Accountant.Upsert(a);
            }

            return new EditableText(sb.ToString());
        }

        /// <inheritdoc />
        protected override IQueryResult ExecuteRecal(IQueryCompunded<IDistributedQueryAtom> distQuery)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(Accountant.SelectAmortizations(distQuery)))
            {
                Accountant.Amortize(a);
                sb.Append(Serializer.PresentAmort(a));
                Accountant.Upsert(a);
            }

            return new EditableText(sb.ToString());
        }

        /// <inheritdoc />
        protected override IQueryResult ExecuteResetSoft(IQueryCompunded<IDistributedQueryAtom> distQuery,
            DateFilter rng)
        {
            var cnt = 0L;
            foreach (var a in Accountant.SelectAmortizations(distQuery))
            {
                if (a.Schedule == null)
                    continue;

                var flag = false;
                foreach (var item in a.Schedule.Where(item => item.Date.Within(rng))
                    .Where(item => item.VoucherID != null)
                    .Where(item => Accountant.SelectVoucher(item.VoucherID) == null))
                {
                    item.VoucherID = null;
                    cnt++;
                    flag = true;
                }

                if (flag)
                    Accountant.Upsert(a);
            }

            return new NumberAffected(cnt);
        }

        /// <inheritdoc />
        protected override IQueryResult ExcuteResetMixed(IQueryCompunded<IDistributedQueryAtom> distQuery,
            DateFilter rng)
        {
            var cnt = 0L;
            foreach (var a in Accountant.SelectAmortizations(distQuery))
            {
                if (a.Schedule == null)
                    continue;

                var flag = false;
                foreach (var item in a.Schedule.Where(item => item.Date.Within(rng))
                    .Where(item => item.VoucherID != null))
                {
                    var voucher = Accountant.SelectVoucher(item.VoucherID);
                    if (voucher == null)
                    {
                        item.VoucherID = null;
                        cnt++;
                        flag = true;
                    }
                    else if (Accountant.DeleteVoucher(voucher.ID))
                    {
                        item.VoucherID = null;
                        cnt++;
                        flag = true;
                    }
                }

                if (flag)
                    Accountant.Upsert(a);
            }

            return new NumberAffected(cnt);
        }

        /// <inheritdoc />
        protected override IQueryResult ExecuteResetHard(IQueryCompunded<IDistributedQueryAtom> distQuery,
            IQueryCompunded<IVoucherQueryAtom> query) { throw new InvalidOperationException(); }

        /// <inheritdoc />
        protected override IQueryResult ExecuteApply(IQueryCompunded<IDistributedQueryAtom> distQuery, DateFilter rng,
            bool isCollapsed)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(Accountant.SelectAmortizations(distQuery)))
            {
                foreach (var item in Accountant.Update(a, rng, isCollapsed))
                    sb.AppendLine(ListAmortItem(item));

                Accountant.Upsert(a);
            }

            if (sb.Length > 0)
                return new EditableText(sb.ToString());

            return new Succeed();
        }

        /// <inheritdoc />
        protected override IQueryResult ExecuteCheck(IQueryCompunded<IDistributedQueryAtom> distQuery, DateFilter rng)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(Accountant.SelectAmortizations(distQuery)))
            {
                var sbi = new StringBuilder();
                foreach (var item in Accountant.Update(a, rng, false, true))
                    sbi.AppendLine(ListAmortItem(item));

                if (sbi.Length != 0)
                {
                    sb.AppendLine(ListAmort(a, null, false));
                    sb.AppendLine(sbi.ToString());
                }

                Accountant.Upsert(a);
            }

            if (sb.Length > 0)
                return new EditableText(sb.ToString());

            return new Succeed();
        }

        /// <summary>
        ///     显示摊销及其计算表
        /// </summary>
        /// <param name="amort">摊销</param>
        /// <param name="dt">计算账面价值的时间</param>
        /// <param name="showSchedule">是否显示计算表</param>
        /// <returns>格式化的信息</returns>
        private string ListAmort(Amortization amort, DateTime? dt = null, bool showSchedule = true)
        {
            var sb = new StringBuilder();

            var bookValue = Accountant.GetBookValueOn(amort, dt);
            if (dt.HasValue &&
                (!bookValue.HasValue || bookValue.Value.IsZero()))
                return null;

            sb.AppendLine(
                $"{amort.StringID} {amort.Name.CPadRight(35)}{amort.Date:yyyyMMdd}" +
                $"{amort.Value.AsCurrency().CPadLeft(13)}{(dt.HasValue ? bookValue.AsCurrency().CPadLeft(13) : "-".CPadLeft(13))}" +
                $"{((amort.TotalDays?.ToString(CultureInfo.InvariantCulture) ?? "-").CPadLeft(4))}{amort.Interval.ToString().CPadLeft(20)}");
            if (showSchedule && amort.Schedule != null)
                foreach (var amortItem in amort.Schedule)
                {
                    sb.AppendLine(ListAmortItem(amortItem));
                    if (amortItem.VoucherID != null)
                        sb.AppendLine(Serializer.PresentVoucher(Accountant.SelectVoucher(amortItem.VoucherID)));
                }

            return sb.ToString();
        }

        /// <summary>
        ///     显示摊销计算表条目
        /// </summary>
        /// <param name="amortItem">摊销计算表条目</param>
        /// <returns>格式化的信息</returns>
        private static string ListAmortItem(AmortItem amortItem)
            => string.Format(
                "   {0:yyyMMdd} AMO:{1} ={3} ({2})",
                amortItem.Date,
                amortItem.Amount.AsCurrency()
                    .CPadLeft(13),
                amortItem.VoucherID,
                amortItem.Value.AsCurrency()
                    .CPadLeft(13));

        /// <summary>
        ///     对摊销进行排序
        /// </summary>
        /// <param name="enumerable">摊销</param>
        /// <returns>排序后的摊销</returns>
        private static IEnumerable<Amortization> Sort(IEnumerable<Amortization> enumerable)
            => enumerable.OrderBy(o => o.Date, new DateComparer()).ThenBy(o => o.Name).ThenBy(o => o.ID);
    }
}
