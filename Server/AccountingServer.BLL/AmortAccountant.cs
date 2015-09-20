using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.DAL;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     摊销会计业务处理类
    /// </summary>
    internal class AmortAccountant : DistributedAccountant
    {
        public AmortAccountant(IDbAdapter db) : base(db) { }

        /// <summary>
        ///     获取本次摊销日期
        /// </summary>
        /// <param name="interval">间隔类型</param>
        /// <param name="the">日期</param>
        /// <returns>这次摊销日期</returns>
        private static DateTime ThisAmortizationDate(AmortizeInterval interval, DateTime the)
        {
            switch (interval)
            {
                case AmortizeInterval.EveryDay:
                case AmortizeInterval.SameDayOfWeek:
                    return the;
                case AmortizeInterval.SameDayOfYear:
                    if (the.Month == 2 &&
                        the.Day == 29)
                        return the.AddDays(1);
                    return the;
                case AmortizeInterval.SameDayOfMonth:
                    return the.Day > 28 ? the.AddDays(1 - the.Day).AddMonths(1) : the;
                case AmortizeInterval.LastDayOfWeek:
                    return the.DayOfWeek == DayOfWeek.Sunday ? the : the.AddDays(7 - (int)the.DayOfWeek);
                case AmortizeInterval.LastDayOfMonth:
                    return AccountantHelper.LastDayOfMonth(the.Year, the.Month);
                case AmortizeInterval.LastDayOfYear:
                    return new DateTime(the.Year + 1, 1, 1).AddDays(-1);
                default:
                    throw new ArgumentException("间隔类型未知", nameof(interval));
            }
        }

        /// <summary>
        ///     获取下一个摊销日期
        /// </summary>
        /// <param name="interval">间隔类型</param>
        /// <param name="last">上一次摊销日期</param>
        /// <returns>下一个摊销日期</returns>
        private static DateTime NextAmortizationDate(AmortizeInterval interval, DateTime last)
        {
            switch (interval)
            {
                case AmortizeInterval.EveryDay:
                    return last.AddDays(1);
                case AmortizeInterval.SameDayOfWeek:
                    return last.AddDays(7);
                case AmortizeInterval.LastDayOfWeek:
                    return last.DayOfWeek == DayOfWeek.Sunday ? last.AddDays(7) : last.AddDays(14 - (int)last.DayOfWeek);
                case AmortizeInterval.SameDayOfMonth:
                    return last.AddMonths(1);
                case AmortizeInterval.LastDayOfMonth:
                    return AccountantHelper.LastDayOfMonth(last.Year, last.Month + 1);
                case AmortizeInterval.SameDayOfYear:
                    return last.AddYears(1);
                case AmortizeInterval.LastDayOfYear:
                    return new DateTime(last.Year + 2, 1, 1).AddDays(-1);
                default:
                    throw new ArgumentException("间隔类型未知", nameof(interval));
            }
        }

        /// <summary>
        ///     调整摊销计算表
        /// </summary>
        /// <param name="amort">摊销</param>
        public static void InternalRegular(Amortization amort)
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
                item.Value = (resiValue -= item.Amount);

            amort.Schedule = lst;
        }

        /// <summary>
        ///     找出未在摊销计算表中注册的记账凭证，并尝试建立引用
        /// </summary>
        /// <param name="amort">摊销</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="query">检索式</param>
        /// <returns>未注册的记账凭证</returns>
        public IEnumerable<Voucher> RegisterVouchers(Amortization amort, DateFilter rng,
                                                     IQueryCompunded<IVoucherQueryAtom> query)
        {
            if (amort.Remark == Amortization.IgnoranceMark)
                yield break;

            foreach (
                var voucher in
                    Db.SelectVouchers(
                                      new VoucherQueryAryBase(
                                          OperatorType.Intersect,
                                          new[]
                                              {
                                                  query,
                                                  new VoucherQueryAtomBase(amort.Template, amort.Template.Details)
                                                      {
                                                          ForAll
                                                              =
                                                              true
                                                      }
                                              }))
                )
            {
                if (voucher.Remark == Amortization.IgnoranceMark)
                    continue;

                if (amort.Schedule.Any(item => item.VoucherID == voucher.ID))
                    continue;

                if (voucher.Details.Zip(amort.Template.Details, (d1, d2) => d1.IsMatch(d2)).Contains(false))
                    yield return voucher;
                else
                {
                    var lst = amort.Schedule
                                   .Where(item => item.Date.Within(rng))
                                   .Where(item => item.Date == voucher.Date).ToList();

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
        /// <param name="template">记账凭证模板</param>
        /// <returns>是否成功</returns>
        private bool UpdateVoucher(AmortItem item, bool isCollapsed, bool editOnly, Voucher template)
        {
            if (item.VoucherID == null)
                return !editOnly && GenerateVoucher(item, isCollapsed, template);

            var voucher = Db.SelectVoucher(item.VoucherID);
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
                Db.Upsert(voucher);

            return true;
        }

        /// <summary>
        ///     生成记账凭证、插入数据库并注册
        /// </summary>
        /// <param name="item">计算表条目</param>
        /// <param name="isCollapsed">是否压缩</param>
        /// <param name="template">记账凭证模板</param>
        /// <returns>是否成功</returns>
        private bool GenerateVoucher(AmortItem item, bool isCollapsed, Voucher template)
        {
            var lst = template.Details.Select(
                                              detail => new VoucherDetail
                                                            {
                                                                Title = detail.Title,
                                                                SubTitle = detail.SubTitle,
                                                                Content = detail.Content,
                                                                Fund =
                                                                    detail.Remark == AmortItem.IgnoranceMark
                                                                        ? detail.Fund
                                                                        : item.Amount * detail.Fund,
                                                                Remark = item.Remark
                                                            }).ToList();
            var voucher = new Voucher
                              {
                                  Date = isCollapsed ? null : item.Date,
                                  Remark = template.Remark ?? "automatically generated",
                                  Type = template.Type,
                                  Details = lst
                              };
            var res = Db.Upsert(voucher);
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

            var lst = new List<AmortItem>();

            var a = amort.Value.Value / amort.TotalDays.Value;
            var residue = amort.Value.Value;

            var dtCur = amort.Date.Value;
            var dtEnd = amort.Date.Value.AddDays(amort.TotalDays.Value - 1);
            var flag = true;
            while (true)
            {
                var dtNxt = flag
                                ? ThisAmortizationDate(amort.Interval.Value, amort.Date.Value)
                                : NextAmortizationDate(amort.Interval.Value, dtCur);

                if (dtNxt >= dtEnd)
                {
                    lst.Add(new AmortItem { Date = dtNxt, Amount = residue });
                    break;
                }
                var n = dtNxt.Subtract(dtCur).TotalDays + (flag ? 1 : 0);

                flag = false;

                var amount = a * n;
                residue -= amount;
                if (!amount.IsZero())
                    lst.Add(new AmortItem { Date = dtNxt, Amount = amount });
                dtCur = dtNxt;
            }

            amort.Schedule = lst;
        }
    }
}
