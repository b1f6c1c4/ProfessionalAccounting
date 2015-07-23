using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell.Parsing;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     摊销表达式解释器
    /// </summary>
    internal class AmortizationShell
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public AmortizationShell(Accountant helper) { m_Accountant = helper; }

        /// <summary>
        ///     执行摊销表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        public IQueryResult ExecuteAmort(ShellParser.AmortContext expr)
        {
            var amortListContext = expr.amortList();
            if (amortListContext != null)
            {
                var showSchedule = amortListContext.AOList() != null;
                var dt = !showSchedule
                             ? amortListContext.rangePoint()?.Range.EndDate ?? DateTime.Now.Date
                             : (DateTime?)null;

                return ExecuteList(amortListContext.distributedQ(), dt, showSchedule);
            }
            var amortQueryContext = expr.amortQuery();
            if (amortQueryContext != null)
                return ExecuteQuery(amortQueryContext.distributedQ());
            var amortRegisterContext = expr.amortRegister();
            if (amortRegisterContext != null)
                return ExecuteRegister(
                                       amortRegisterContext.distributedQ(),
                                       amortRegisterContext.range().TheRange(),
                                       amortRegisterContext.vouchers());
            var amortUnregisterContext = expr.amortUnregister();
            if (amortUnregisterContext != null)
                return ExecuteUnregister(
                                         amortUnregisterContext.distributedQ(),
                                         amortUnregisterContext.range().TheRange(),
                                         amortUnregisterContext.vouchers());
            var amortReamoContext = expr.amortReamo();
            if (amortReamoContext != null)
                return ExecuteReamo(amortReamoContext.distributedQ());
            var amortResetSoftContext = expr.amortResetSoft();
            if (amortResetSoftContext != null)
                return ExecuteResetSoft(
                                        amortResetSoftContext.distributedQ(),
                                        amortResetSoftContext.range().TheRange());
            var amortResetMixedContext = expr.amortResetMixed();
            if (amortResetMixedContext != null)
                return ExcuteResetMixed(
                                        amortResetMixedContext.distributedQ(),
                                        amortResetMixedContext.range().TheRange());
            var amortApplyContext = expr.amortApply();
            if (amortApplyContext != null)
                return ExecuteApply(
                                    amortApplyContext.distributedQ(),
                                    amortApplyContext.range().TheRange(),
                                    amortApplyContext.AOCollapse() != null);
            var amortCheckContext = expr.amortCheck();
            if (amortCheckContext != null)
                return ExecuteCheck(amortCheckContext.distributedQ(), new DateFilter(null, DateTime.Now.Date));

            throw new InvalidOperationException("摊销表达式无效");
        }

        /// <summary>
        ///     执行列表表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="dt">计算账面价值的时间</param>
        /// <param name="showSchedule">是否显示折旧计算表</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteList(IQueryCompunded<IDistributedQueryAtom> distQuery, DateTime? dt,
                                         bool showSchedule)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(m_Accountant.SelectAmortizations(distQuery)))
                sb.Append(ListAmort(a, dt, showSchedule));

            if (showSchedule)
                return new EditableText(sb.ToString());
            return new UnEditableText(sb.ToString());
        }

        /// <summary>
        ///     执行查询表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteQuery(IQueryCompunded<IDistributedQueryAtom> distQuery)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(m_Accountant.SelectAmortizations(distQuery)))
                sb.Append(CSharpHelper.PresentAmort(a));

            return new EditableText(sb.ToString());
        }

        /// <summary>
        ///     执行注册表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="query">记账凭证检索式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteRegister(IQueryCompunded<IDistributedQueryAtom> distQuery, DateFilter rng,
                                             IQueryCompunded<IVoucherQueryAtom> query)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(m_Accountant.SelectAmortizations(distQuery)))
            {
                foreach (var voucher in m_Accountant.RegisterVouchers(a, rng, query))
                    sb.Append(CSharpHelper.PresentVoucher(voucher));

                m_Accountant.Upsert(a);
            }
            if (sb.Length > 0)
                return new EditableText(sb.ToString());
            return new Suceed();
        }

        /// <summary>
        ///     执行解除注册表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="query">记账凭证检索式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteUnregister(IQueryCompunded<IDistributedQueryAtom> distQuery, DateFilter rng,
                                               IQueryCompunded<IVoucherQueryAtom> query)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(m_Accountant.SelectAmortizations(distQuery)))
            {
                foreach (var item in a.Schedule.Where(item => item.Date.Within(rng)))
                {
                    if (query != null)
                    {
                        if (item.VoucherID == null)
                            continue;

                        var voucher = m_Accountant.SelectVoucher(item.VoucherID);
                        if (voucher != null)
                            if (!MatchHelper.IsMatch(query, voucher.IsMatch))
                                continue;
                    }
                    item.VoucherID = null;
                }

                sb.Append(ListAmort(a));
                m_Accountant.Upsert(a);
            }
            return new EditableText(sb.ToString());
        }

        /// <summary>
        ///     执行重新计算表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteReamo(IQueryCompunded<IDistributedQueryAtom> distQuery)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(m_Accountant.SelectAmortizations(distQuery)))
            {
                Accountant.Amortize(a);
                sb.Append(CSharpHelper.PresentAmort(a));
                m_Accountant.Upsert(a);
            }
            return new EditableText(sb.ToString());
        }

        /// <summary>
        ///     执行软重置表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteResetSoft(IQueryCompunded<IDistributedQueryAtom> distQuery, DateFilter rng)
        {
            var cnt = 0L;
            foreach (var a in m_Accountant.SelectAmortizations(distQuery))
            {
                if (a.Schedule == null)
                    continue;
                var flag = false;
                foreach (var item in a.Schedule.Where(item => item.Date.Within(rng))
                                      .Where(item => item.VoucherID != null)
                                      .Where(item => m_Accountant.SelectVoucher(item.VoucherID) == null))
                {
                    item.VoucherID = null;
                    cnt++;
                    flag = true;
                }
                if (flag)
                    m_Accountant.Upsert(a);
            }
            return new NumberAffected(cnt);
        }

        /// <summary>
        ///     执行混合重置表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExcuteResetMixed(IQueryCompunded<IDistributedQueryAtom> distQuery, DateFilter rng)
        {
            var cnt = 0L;
            foreach (var a in m_Accountant.SelectAmortizations(distQuery))
            {
                if (a.Schedule == null)
                    continue;
                var flag = false;
                foreach (var item in a.Schedule.Where(item => item.Date.Within(rng))
                                      .Where(item => item.VoucherID != null))
                {
                    var voucher = m_Accountant.SelectVoucher(item.VoucherID);
                    if (voucher == null)
                    {
                        item.VoucherID = null;
                        cnt++;
                        flag = true;
                    }
                    else if (m_Accountant.DeleteVoucher(voucher.ID))
                    {
                        item.VoucherID = null;
                        cnt++;
                        flag = true;
                    }
                }
                if (flag)
                    m_Accountant.Upsert(a);
            }
            return new NumberAffected(cnt);
        }

        /// <summary>
        ///     执行应用表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="isCollapsed">是否压缩</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteApply(IQueryCompunded<IDistributedQueryAtom> distQuery, DateFilter rng,
                                          bool isCollapsed)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(m_Accountant.SelectAmortizations(distQuery)))
            {
                foreach (var item in m_Accountant.Update(a, rng, isCollapsed))
                    sb.AppendLine(ListAmortItem(item));

                m_Accountant.Upsert(a);
            }
            if (sb.Length > 0)
                return new EditableText(sb.ToString());
            return new Suceed();
        }

        /// <summary>
        ///     执行检查表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteCheck(IQueryCompunded<IDistributedQueryAtom> distQuery, DateFilter rng)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(m_Accountant.SelectAmortizations(distQuery)))
            {
                var sbi = new StringBuilder();
                foreach (var item in m_Accountant.Update(a, rng, false, true))
                    sbi.AppendLine(ListAmortItem(item));

                if (sbi.Length != 0)
                {
                    sb.AppendLine(ListAmort(a, null, false));
                    sb.AppendLine(sbi.ToString());
                }

                m_Accountant.Upsert(a);
            }
            if (sb.Length > 0)
                return new EditableText(sb.ToString());
            return new Suceed();
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
            sb.AppendFormat(
                            "{0} {1}{2:yyyyMMdd}{3}{4}{5}{6}",
                            amort.StringID,
                            amort.Name.CPadRight(35),
                            amort.Date,
                            amort.Value.AsCurrency().CPadLeft(13),
                            dt.HasValue ? bookValue.AsCurrency().CPadLeft(13) : "-".CPadLeft(13),
                            (amort.TotalDays?.ToString(CultureInfo.InvariantCulture) ?? "-").CPadLeft(4),
                            amort.Interval.ToString().CPadLeft(20));
            sb.AppendLine();
            if (showSchedule && amort.Schedule != null)
                foreach (var amortItem in amort.Schedule)
                {
                    sb.AppendLine(ListAmortItem(amortItem));
                    if (amortItem.VoucherID != null)
                        sb.AppendLine(CSharpHelper.PresentVoucher(m_Accountant.SelectVoucher(amortItem.VoucherID)));
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
