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
            AutoConnect();

            if (expr.amortList() != null)
            {
                var dt = expr.amortList().AOAll() != null
                             ? null
                             : expr.amortList().rangePoint() != null
                                   ? expr.amortList().rangePoint().Range.EndDate
                                   : DateTime.Now.Date;

                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAmortizations(expr.amortList().distributedQ())))
                    sb.Append(ListAmort(a, dt, expr.amortList().AOList() != null));

                return new UnEditableText(sb.ToString());
            }
            if (expr.amortQuery() != null)
            {
                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAmortizations(expr.amortQuery().distributedQ())))
                    sb.Append(CSharpHelper.PresentAmort(a));

                return new EditableText(sb.ToString());
            }
            if (expr.amortRegister() != null)
            {
                var rng = expr.amortRegister().range() != null
                              ? expr.amortRegister().range().Range
                              : DateFilter.Unconstrained;
                var query = expr.amortRegister().vouchers();

                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAmortizations(expr.amortRegister().distributedQ())))
                {
                    foreach (var voucher in m_Accountant.RegisterVouchers(a, rng, query))
                        sb.Append(CSharpHelper.PresentVoucher(voucher));

                    m_Accountant.Upsert(a);
                }
                if (sb.Length > 0)
                    return new EditableText(sb.ToString());
                return new Suceed();
            }
            if (expr.amortUnregister() != null)
            {
                var rng = expr.amortUnregister().range() != null
                              ? expr.amortUnregister().range().Range
                              : DateFilter.Unconstrained;
                var query = expr.amortRegister().vouchers();

                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAmortizations(expr.amortUnregister().distributedQ())))
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
            if (expr.amortReamo() != null)
            {
                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAmortizations(expr.amortReamo().distributedQ())))
                {
                    Accountant.Amortize(a);
                    sb.Append(CSharpHelper.PresentAmort(a));
                    m_Accountant.Upsert(a);
                }
                return new EditableText(sb.ToString());
            }
            if (expr.amortResetSoft() != null)
            {
                var rng = expr.amortResetSoft().range() != null
                              ? expr.amortResetSoft().range().Range
                              : DateFilter.Unconstrained;

                var cnt = 0L;
                foreach (var a in m_Accountant.SelectAmortizations(expr.amortResetSoft().distributedQ()))
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
            if (expr.amortApply() != null)
            {
                var isCollapsed = expr.amortApply().AOCollapse() != null;

                var rng = expr.amortUnregister().range() != null
                              ? expr.amortUnregister().range().Range
                              : DateFilter.Unconstrained;

                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAmortizations(expr.amortApply().distributedQ())))
                {
                    foreach (var item in m_Accountant.Update(a, rng, isCollapsed))
                        sb.AppendLine(ListAmortItem(item));

                    m_Accountant.Upsert(a);
                }
                if (sb.Length > 0)
                    return new EditableText(sb.ToString());
                return new Suceed();
            }
            if (expr.amortCheck() != null)
            {
                var rng = new DateFilter(null, DateTime.Now.Date);

                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAmortizations(expr.amortCheck().distributedQ())))
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
                (!bookValue.HasValue || bookValue < Accountant.Tolerance))
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
                                 amortItem.Residue.AsCurrency().CPadLeft(13));
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
