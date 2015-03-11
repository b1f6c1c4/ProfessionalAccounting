using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class AccountingConsole
    {
        /// <summary>
        ///     更新或添加摊销
        /// </summary>
        /// <param name="code">摊销的C#代码</param>
        /// <returns>新摊销的C#代码</returns>
        public string ExecuteAmortUpsert(string code)
        {
            var amort = CSharpHelper.ParseAmort(code);

            if (!m_Accountant.Upsert(amort))
                throw new Exception();

            return CSharpHelper.PresentAmort(amort);
        }

        /// <summary>
        ///     删除摊销
        /// </summary>
        /// <param name="code">摊销的C#代码</param>
        /// <returns>是否成功</returns>
        public bool ExecuteAmortRemoval(string code)
        {
            var amort = CSharpHelper.ParseAmort(code);

            if (!amort.ID.HasValue)
                throw new Exception();

            return m_Accountant.DeleteAmortization(amort.ID.Value);
        }

        /// <summary>
        ///     执行摊销表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteAmort(ConsoleParser.AmortContext expr)
        {
            var amortListContext = expr.amortList();
            if (amortListContext != null)
            {
                var dt = amortListContext.AOAll() != null
                             ? null
                             : amortListContext.rangePoint() != null
                                   ? amortListContext.rangePoint().Range.EndDate
                                   : DateTime.Now.Date;

                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAmortizations(amortListContext.distributedQ())))
                    sb.Append(ListAmort(a, dt, amortListContext.AOList() != null));

                if (amortListContext.AOList() != null)
                    return new EditableText(sb.ToString());
                return new UnEditableText(sb.ToString());
            }
            var amortQueryContext = expr.amortQuery();
            if (amortQueryContext != null)
            {
                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAmortizations(amortQueryContext.distributedQ())))
                    sb.Append(CSharpHelper.PresentAmort(a));

                return new EditableText(sb.ToString());
            }
            var amortRegisterContext = expr.amortRegister();
            if (amortRegisterContext != null)
            {
                var rng = amortRegisterContext.range() != null
                              ? amortRegisterContext.range().Range
                              : DateFilter.Unconstrained;
                var query = amortRegisterContext.vouchers();

                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAmortizations(amortRegisterContext.distributedQ())))
                {
                    foreach (var voucher in m_Accountant.RegisterVouchers(a, rng, query))
                        sb.Append(CSharpHelper.PresentVoucher(voucher));

                    m_Accountant.Upsert(a);
                }
                if (sb.Length > 0)
                    return new EditableText(sb.ToString());
                return new Suceed();
            }
            var amortUnregisterContext = expr.amortUnregister();
            if (amortUnregisterContext != null)
            {
                var rng = amortUnregisterContext.range() != null
                              ? amortUnregisterContext.range().Range
                              : DateFilter.Unconstrained;
                var query = amortRegisterContext.vouchers();

                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAmortizations(amortUnregisterContext.distributedQ())))
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
            var amortReamoContext = expr.amortReamo();
            if (amortReamoContext != null)
            {
                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAmortizations(amortReamoContext.distributedQ())))
                {
                    Accountant.Amortize(a);
                    sb.Append(CSharpHelper.PresentAmort(a));
                    m_Accountant.Upsert(a);
                }
                return new EditableText(sb.ToString());
            }
            var amortResetSoftContext = expr.amortResetSoft();
            if (amortResetSoftContext != null)
            {
                var rng = amortResetSoftContext.range() != null
                              ? amortResetSoftContext.range().Range
                              : DateFilter.Unconstrained;

                var cnt = 0L;
                foreach (var a in m_Accountant.SelectAmortizations(amortResetSoftContext.distributedQ()))
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
            var amortResetMixedContext = expr.amortResetMixed();
            if (amortResetMixedContext != null)
            {
                var rng = amortResetMixedContext.range() != null
                              ? amortResetMixedContext.range().Range
                              : DateFilter.Unconstrained;

                var cnt = 0L;
                foreach (var a in m_Accountant.SelectAmortizations(amortResetMixedContext.distributedQ()))
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
                        else
                        {
                            if (m_Accountant.DeleteVoucher(voucher.ID))
                            {
                                item.VoucherID = null;
                                cnt++;
                                flag = true;
                            }
                        }
                    }
                    if (flag)
                        m_Accountant.Upsert(a);
                }
                return new NumberAffected(cnt);
            }
            var amortApplyContext = expr.amortApply();
            if (amortApplyContext != null)
            {
                var isCollapsed = amortApplyContext.AOCollapse() != null;

                var rng = amortApplyContext.range() != null
                              ? amortApplyContext.range().Range
                              : DateFilter.Unconstrained;

                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAmortizations(amortApplyContext.distributedQ())))
                {
                    foreach (var item in m_Accountant.Update(a, rng, isCollapsed))
                        sb.AppendLine(ListAmortItem(item));

                    m_Accountant.Upsert(a);
                }
                if (sb.Length > 0)
                    return new EditableText(sb.ToString());
                return new Suceed();
            }
            var amortCheckContext = expr.amortCheck();
            if (amortCheckContext != null)
            {
                var rng = new DateFilter(null, DateTime.Now.Date);

                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAmortizations(amortCheckContext.distributedQ())))
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

            throw new InvalidOperationException("摊销表达式无效");
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
                (!bookValue.HasValue || Accountant.IsZero(bookValue.Value)))
                return null;
            sb.AppendFormat(
                            "{0} {1}{2:yyyyMMdd}{3}{4}{5}{6}",
                            amort.StringID,
                            amort.Name.CPadRight(35),
                            amort.Date,
                            amort.Value.AsCurrency().CPadLeft(13),
                            dt.HasValue ? bookValue.AsCurrency().CPadLeft(13) : "-".CPadLeft(13),
                            (amort.TotalDays.HasValue
                                 ? amort.TotalDays.Value.ToString(CultureInfo.InvariantCulture)
                                 : "-").CPadLeft(4),
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
        {
            return String.Format(
                                 "   {0:yyyMMdd} AMO:{1} ={3} ({2})",
                                 amortItem.Date,
                                 amortItem.Amount.AsCurrency().CPadLeft(13),
                                 amortItem.VoucherID,
                                 amortItem.Value.AsCurrency().CPadLeft(13));
        }

        /// <summary>
        ///     对摊销进行排序
        /// </summary>
        /// <param name="enumerable">摊销</param>
        /// <returns>排序后的摊销</returns>
        private static IEnumerable<Amortization> Sort(IEnumerable<Amortization> enumerable)
        {
            return enumerable.OrderBy(o => o.Date, new DateComparer()).ThenBy(o => o.Name).ThenBy(o => o.ID);
        }
    }
}
