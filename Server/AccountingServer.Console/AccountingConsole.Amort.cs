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

            if (!amort.ID.HasValue)
            {
                if (!m_Accountant.Upsert(amort))
                    throw new Exception();
            }
            else if (!m_Accountant.Update(amort))
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
        ///     执行表达式（摊销）
        /// </summary>
        /// <param name="s">表达式</param>
        /// <returns>执行结果</returns>
        public IQueryResult ExecuteAmort(string s)
        {
            AutoConnect();

            var sp = s.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var query = sp.Length == 1 ? String.Empty : sp[1];

            if (sp[0].Equals("o", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                var filter = ParseAmortQuery(query);
                foreach (var o in Sort(m_Accountant.FilteredSelect(filter)))
                    sb.Append(ListAmort(o, DateTime.Now.Date, false));

                return new UnEditableText(sb.ToString());
            }
            if (sp[0].StartsWith("o@", StringComparison.OrdinalIgnoreCase))
            {
                var dt = DateTime.ParseExact(sp[0] + "01", "o@yyyyMMdd", null).AddMonths(1).AddDays(-1);

                var sb = new StringBuilder();
                var filter = ParseAmortQuery(query);
                foreach (var o in Sort(m_Accountant.FilteredSelect(filter)))
                    sb.Append(ListAmort(o, dt, false));

                return new UnEditableText(sb.ToString());
            }
            if (sp[0].Equals("o-all", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                var filter = ParseAmortQuery(query);
                foreach (var o in Sort(m_Accountant.FilteredSelect(filter)))
                    sb.Append(ListAmort(o, null, false));

                return new UnEditableText(sb.ToString());
            }
            if (sp[0].StartsWith("o-li", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                var filter = ParseAmortQuery(query);
                foreach (var o in Sort(m_Accountant.FilteredSelect(filter)))
                    sb.Append(ListAmort(o));

                return new UnEditableText(sb.ToString());
            }
            if (sp[0].StartsWith("o-q", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                var filter = ParseAmortQuery(query);
                foreach (var o in Sort(m_Accountant.FilteredSelect(filter)))
                    sb.Append(CSharpHelper.PresentAmort(o));

                return new EditableText(sb.ToString());
            }
            if (sp[0].StartsWith("o-reg", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                var filter = ParseAmortQuery(query);
                foreach (var o in Sort(m_Accountant.FilteredSelect(filter)))
                {
                    foreach (var voucher in m_Accountant.RegisterVouchers(o))
                        sb.Append(CSharpHelper.PresentVoucher(voucher));

                    m_Accountant.Update(o);
                }
                if (sb.Length > 0)
                    return new EditableText(sb.ToString());
                return new Suceed();
            }
            if (sp[0].StartsWith("o-unr", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                var filter = ParseAmortQuery(query);
                foreach (var o in Sort(m_Accountant.FilteredSelect(filter)))
                {
                    foreach (var item in o.Schedule)
                        item.VoucherID = null;

                    sb.Append(ListAmort(o));
                    m_Accountant.Update(o);
                }
                return new EditableText(sb.ToString());
            }
            if (sp[0].Equals("o-rd", StringComparison.OrdinalIgnoreCase) ||
                sp[0].Equals("o-reamo", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                var filter = ParseAmortQuery(query);
                foreach (var o in m_Accountant.FilteredSelect(filter))
                {
                    Accountant.Amortize(o);
                    sb.Append(CSharpHelper.PresentAmort(o));
                    m_Accountant.Update(o);
                }
                return new EditableText(sb.ToString());
            }
            if (sp[0].StartsWith("o-ap", StringComparison.OrdinalIgnoreCase))
            {
                var isCollapsed = sp[0].EndsWith("-collapse", StringComparison.OrdinalIgnoreCase) ||
                                  sp[0].EndsWith("-co", StringComparison.OrdinalIgnoreCase);

                string dq;
                Amortization filter = null;
                if (query.LastIndexOf('\'') >= 0)
                {
                    var spx = query.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    dq = spx.Length <= 1 ? String.Empty : spx[1];
                    filter = ParseAmortQuery(spx[0]);
                }
                else
                    dq = query;

                var rng = String.IsNullOrWhiteSpace(dq) ? ParseDateQuery("[0]", true) : ParseDateQuery(dq, true);

                var sb = new StringBuilder();
                foreach (var o in m_Accountant.FilteredSelect(filter))
                {
                    foreach (var item in m_Accountant.Update(o, rng, isCollapsed))
                        sb.AppendLine(ListAmortItem(item));

                    m_Accountant.Update(o);
                }
                if (sb.Length > 0)
                    return new EditableText(sb.ToString());
                return new Suceed();
            }
            if (sp[0].StartsWith("o-chk", StringComparison.OrdinalIgnoreCase))
            {
                var filter = ParseAmortQuery(query);

                var rng = new DateFilter(null, DateTime.Now.Date);

                var sb = new StringBuilder();
                foreach (var o in m_Accountant.FilteredSelect(filter))
                {
                    var sbi = new StringBuilder();
                    foreach (var item in m_Accountant.Update(o, rng, false, true))
                        sbi.AppendLine(ListAmortItem(item));

                    if (sbi.Length != 0)
                    {
                        sb.AppendLine(ListAmort(o, null, false));
                        sb.AppendLine(sbi.ToString());
                    }

                    m_Accountant.Update(o);
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
                            "{0} {1}{2:yyyyMMdd}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}{13}{14}",
                            amort.StringID,
                            amort.Name.CPadRight(35),
                            amort.Date,
                            amort.Value.AsCurrency().CPadLeft(13),
                            dt.HasValue ? bookValue.AsCurrency().CPadLeft(13) : "-".CPadLeft(13),
                            (amort.TotalDays.HasValue ? amort.TotalDays.Value.ToString(CultureInfo.InvariantCulture):"-").CPadLeft(4),
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

        private static string ListAmortItem(AmortItem amortItem)
        {
                return String.Format(
                                     "   {0:yyyMMdd} AMO:{1} ={3} ({2})",
                                     amortItem.Date,
                                     amortItem.Amount.AsCurrency().CPadLeft(13),
                                     amortItem.VoucherID,
                                     amortItem.Residue.AsCurrency().CPadLeft(13));
        }

        private static IEnumerable<Amortization> Sort(IEnumerable<Amortization> enumerable)
        {
            return enumerable.OrderBy(o => o.Date, new DateComparer()).ThenBy(o => o.Name).ThenBy(o => o.ID);
        }
    }
}
