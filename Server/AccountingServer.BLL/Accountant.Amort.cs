using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    public partial class Accountant
    {
        /// <summary>
        ///     获取下一个摊销日期
        /// </summary>
        /// <param name="interval">间隔类型</param>
        /// <param name="last">上一次摊销日期</param>
        /// <returns>此月最后一天</returns>
        private static DateTime NextAmortizationDate(AmortizeInterval interval, DateTime last)
        {
            switch (interval)
            {
                case AmortizeInterval.EveryDay:
                    return last.AddDays(1);
                case AmortizeInterval.SameDayOfWeek:
                    return last.AddDays(7);
                case AmortizeInterval.LastDayOfWeek:
                    return last.AddDays(last.DayOfWeek == DayOfWeek.Sunday ? 7 : 14 - (int)last.DayOfWeek);
                    // 周日的DayOfWeek是0
                case AmortizeInterval.SameDayOfMonth:
                    return last.AddMonths(1); // TODO: fix 30th
                case AmortizeInterval.LastDayOfMonth:
                    return LastDayOfMonth(last.Year, last.Month + 1);
                case AmortizeInterval.SameDayOfYear:
                    return last.AddYears(1);
                case AmortizeInterval.LastDayOfYear:
                    return new DateTime(last.Year + 2, 1, 1).AddDays(-1);
                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        ///     获取摊销的待摊数额
        /// </summary>
        /// <param name="amort">摊销</param>
        /// <param name="dt">日期，若为<c>null</c>则返回原值</param>
        /// <returns>指定日期的待摊数额，若尚未购置则为<c>null</c></returns>
        public static double? GetResidualValueOn(Amortization amort, DateTime? dt)
        {
            if (!dt.HasValue ||
                amort.Schedule == null)
                return amort.Value;

            var last = amort.Schedule.LastOrDefault(item => DateHelper.CompareDate(item.Date, dt) <= 0);
            if (last != null)
                return last.Residue;
            return null;
        }

        /// <summary>
        ///     调整摊销计算表
        /// </summary>
        /// <param name="amort">摊销</param>
        private static void InternalRegular(Amortization amort)
        {
            if (amort.Remark == Amortization.IgnoranceMark)
                return;
            if (!amort.Date.HasValue ||
                !amort.Value.HasValue)
                return;

            List<AmortItem> lst;
            if (amort.Schedule == null)
                lst = new List<AmortItem>();
            else if (amort.Schedule is List<AmortItem>)
                lst = amort.Schedule as List<AmortItem>;
            else
                lst = amort.Schedule.ToList();

            lst.Sort((item1, item2) => DateHelper.CompareDate(item1.Date, item2.Date));

            var resiValue = amort.Value.Value;
            foreach (var item in lst)
                item.Residue = (resiValue -= item.Amount);

            amort.Schedule = lst;
        }

        /// <summary>
        ///     找出未在摊销计算表中注册的凭证，并尝试建立引用
        /// </summary>
        /// <param name="amort">摊销</param>
        /// <returns>未注册的凭证</returns>
        public IEnumerable<Voucher> RegisterVouchers(Amortization amort)
        {
            if (amort.Remark == Amortization.IgnoranceMark)
                yield break;

            foreach (var voucher in m_Db.FilteredSelect(amort.Template, amort.Template.Details, useAnd: true))
            {
                if (voucher.Remark == Amortization.IgnoranceMark)
                    continue;

                if (amort.Schedule.Any(item => item.VoucherID == voucher.ID))
                    continue;

                if (voucher.Details.Zip(amort.Template.Details, MatchHelper.IsMatch).Contains(false))
                    yield return voucher;
                else
                {
                    var lst = amort.Schedule.Where(item => item.Date == voucher.Date).ToList();

                    if (lst.Count == 1)
                        lst[0].VoucherID = voucher.ID;
                    else
                        yield return voucher;
                }
            }
        }

        /// <summary>
        ///     根据摊销计算表更新账面
        /// </summary>
        /// <param name="amort">摊销</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="isCollapsed">是否压缩</param>
        /// <param name="editOnly">是否只允许更新</param>
        /// <returns>无法更新的条目</returns>
        public IEnumerable<AmortItem> Update(Amortization amort, DateFilter rng,
                                             bool isCollapsed = false, bool editOnly = false)
        {
            if (amort.Schedule == null)
                yield break;

            foreach (
                var item in
                    amort.Schedule.Where(item => item.Date.Within(rng))
                         .Where(item => !UpdateVoucher(item, isCollapsed, editOnly, amort.Template)))
                yield return item;
        }

        /// <summary>
        ///     根据摊销计算表条目更新账面
        /// </summary>
        /// <param name="item">计算表条目</param>
        /// <param name="isCollapsed">是否压缩</param>
        /// <param name="editOnly">是否只允许更新</param>
        /// <param name="template">凭证模板</param>
        /// <returns>是否成功</returns>
        private bool UpdateVoucher(AmortItem item, bool isCollapsed, bool editOnly, Voucher template)
        {
            if (item.VoucherID == null)
                return !editOnly && GenerateVoucher(item, isCollapsed, template);

            var voucher = m_Db.SelectVoucher(item.VoucherID);
            if (voucher == null)
                return !editOnly && GenerateVoucher(item, isCollapsed, template);

            if (voucher.Date != (isCollapsed ? null : item.Date) &&
                !editOnly)
                return false;

            var modified = false;

            if (voucher.Type != template.Type)
            {
                modified = true;
                voucher.Type = template.Type;
            }

            if (template.Details.Count != voucher.Details.Count)
                return !editOnly && GenerateVoucher(item, isCollapsed, template);

            foreach (var d in template.Details)
            {
                if (d.Remark == Amortization.IgnoranceMark)
                    continue;

                bool sucess;
                bool mo;
                UpdateDetail(d, voucher, out sucess, out mo, editOnly);
                if (!sucess)
                    return false;
                modified |= mo;
            }

            if (modified)
                m_Db.Upsert(voucher);

            return true;
        }

        /// <summary>
        ///     生成凭证、插入数据库并注册
        /// </summary>
        /// <param name="item">计算表条目</param>
        /// <param name="isCollapsed">是否压缩</param>
        /// <param name="template">凭证模板</param>
        /// <returns>是否成功</returns>
        private bool GenerateVoucher(AmortItem item, bool isCollapsed, Voucher template)
        {
            var voucher = new Voucher
                              {
                                  Date = isCollapsed ? null : item.Date,
                                  Remark = template.Remark ?? "automatically generated",
                                  Type = template.Type,
                                  Details = template.Details
                              };
            var res = m_Db.Upsert(voucher);
            item.VoucherID = voucher.ID;
            return res;
        }

        /// <summary>
        ///     摊销
        /// </summary>
        public static void Amortize(Amortization amort)
        {
            if (!amort.Date.HasValue ||
                !amort.Value.HasValue ||
                !amort.TotalDays.HasValue ||
                amort.Interval == null)
                return;

            switch (amort.Interval)
            {
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
