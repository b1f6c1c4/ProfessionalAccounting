/* Copyright (C) 2020-2022 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL;

/// <summary>
///     摊销会计业务处理类
/// </summary>
internal class AmortAccountant : DistributedAccountant
{
    public AmortAccountant(DbSession db, Client client) : base(db, client) { }

    /// <summary>
    ///     获取本次摊销日期
    /// </summary>
    /// <param name="interval">间隔类型</param>
    /// <param name="the">日期</param>
    /// <returns>这次摊销日期</returns>
    private static DateTime ThisAmortizationDate(AmortizeInterval interval, DateTime the)
        => interval switch
            {
                AmortizeInterval.EveryDay => the,
                AmortizeInterval.SameDayOfWeek => the,
                AmortizeInterval.SameDayOfYear => the.Month == 2 && the.Day == 29 ? the.AddDays(1) : the,
                AmortizeInterval.SameDayOfMonth => the.Day > 28 ? the.AddDays(1 - the.Day).AddMonths(1) : the,
                AmortizeInterval.LastDayOfWeek =>
                    the.DayOfWeek == DayOfWeek.Sunday ? the : the.AddDays(7 - (int)the.DayOfWeek),
                AmortizeInterval.LastDayOfMonth => DateHelper.LastDayOfMonth(the.Year, the.Month),
                AmortizeInterval.LastDayOfYear => new DateTime(the.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddDays(-1),
                _ => throw new ArgumentException("间隔类型未知", nameof(interval)),
            };

    /// <summary>
    ///     获取下一个摊销日期
    /// </summary>
    /// <param name="interval">间隔类型</param>
    /// <param name="last">上一次摊销日期</param>
    /// <returns>下一个摊销日期</returns>
    private static DateTime NextAmortizationDate(AmortizeInterval interval, DateTime last)
        => interval switch
            {
                AmortizeInterval.EveryDay => last.AddDays(1),
                AmortizeInterval.SameDayOfWeek => last.AddDays(7),
                AmortizeInterval.LastDayOfWeek => last.DayOfWeek == DayOfWeek.Sunday
                    ? last.AddDays(7)
                    : last.AddDays(14 - (int)last.DayOfWeek),
                AmortizeInterval.SameDayOfMonth => last.AddMonths(1),
                AmortizeInterval.LastDayOfMonth => DateHelper.LastDayOfMonth(last.Year, last.Month + 1),
                AmortizeInterval.SameDayOfYear => last.AddYears(1),
                AmortizeInterval.LastDayOfYear => new DateTime(last.Year + 2, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddDays(-1),
                _ => throw new ArgumentException("间隔类型未知", nameof(interval)),
            };

    /// <summary>
    ///     调整摊销计算表
    /// </summary>
    /// <param name="amort">摊销</param>
    public static Amortization InternalRegular(Amortization amort)
    {
        if (amort.Remark == Amortization.IgnoranceMark)
            return amort;
        if (!amort.Date.HasValue ||
            !amort.Value.HasValue)
            return amort;

        var lst = amort.Schedule?.ToList() ?? new();

        lst.Sort(static (item1, item2) => DateHelper.CompareDate(item1.Date, item2.Date));

        var resiValue = amort.Value.Value;
        foreach (var item in lst)
            item.Value = resiValue -= item.Amount;

        amort.Schedule = lst;

        return amort;
    }

    /// <summary>
    ///     找出未在摊销计算表中注册的记账凭证，并尝试建立引用
    /// </summary>
    /// <param name="amort">摊销</param>
    /// <param name="rng">日期过滤器</param>
    /// <param name="query">检索式</param>
    /// <returns>未注册的记账凭证</returns>
    public async IAsyncEnumerable<Voucher> RegisterVouchers(Amortization amort, DateFilter rng,
        IQueryCompounded<IVoucherQueryAtom> query)
    {
        if (amort.Remark == Amortization.IgnoranceMark)
            yield break;

        var queryT = new RegisteringQuery(amort);
        await foreach (var voucher in Db.SelectVouchers(new IntersectQueries<IVoucherQueryAtom>(query, queryT))
                           .Where(static voucher => voucher.Remark != Amortization.IgnoranceMark)
                           .Where(voucher => amort.Schedule.All(item => item.VoucherID != voucher.ID)))
            if (voucher.Details.Zip(amort.Template.Details, static (d1, d2) => d1.IsMatch(d2)).Contains(false))
                yield return voucher;
            else
            {
                var lst = amort.Schedule
                    .Where(item => item.Date.Within(rng))
                    .Where(item => item.Date == voucher.Date)
                    .ToList();

                if (lst.Count == 1)
                    lst[0].VoucherID = voucher.ID;
                else
                    yield return voucher;
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
    public async IAsyncEnumerable<AmortItem> Update(Amortization amort, DateFilter rng,
        bool isCollapsed = false, bool editOnly = false)
    {
        if (amort.Schedule == null)
            yield break;

        foreach (var item in amort.Schedule.Where(item => item.Date.Within(rng)))
            if (!await UpdateVoucher(item, isCollapsed, editOnly, amort.Template))
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
    private async ValueTask<bool> UpdateVoucher(AmortItem item, bool isCollapsed, bool editOnly, Voucher template)
    {
        if (item.VoucherID == null)
            return !editOnly && await GenerateVoucher(item, isCollapsed, template);

        var voucher = await Db.SelectVoucher(item.VoucherID);
        if (voucher == null)
            return !editOnly && await GenerateVoucher(item, isCollapsed, template);

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
            return !editOnly && await GenerateVoucher(item, isCollapsed, template);

        foreach (var d in template.Details)
        {
            if (d.Remark == Amortization.IgnoranceMark)
                continue;

            UpdateDetail(d, voucher, out var success, out var mo, editOnly);
            if (!success)
                return false;

            modified |= mo;
        }

        if (modified)
            await Db.Upsert(voucher);

        return true;
    }

    /// <summary>
    ///     生成记账凭证、插入数据库并注册
    /// </summary>
    /// <param name="item">计算表条目</param>
    /// <param name="isCollapsed">是否压缩</param>
    /// <param name="template">记账凭证模板</param>
    /// <returns>是否成功</returns>
    private async ValueTask<bool> GenerateVoucher(AmortItem item, bool isCollapsed, Voucher template)
    {
        var lst = template.Details.Select(
                detail => new VoucherDetail
                    {
                        User = detail.User,
                        Currency = detail.Currency,
                        Title = detail.Title,
                        SubTitle = detail.SubTitle,
                        Content = detail.Content,
                        Fund =
                            detail.Remark == AmortItem.IgnoranceMark
                                ? detail.Fund
                                : item.Amount * detail.Fund,
                        Remark = item.Remark,
                    })
            .ToList();
        var voucher = new Voucher
            {
                Date = isCollapsed ? null : item.Date,
                Type = template.Type,
                Remark = template.Remark ?? "automatically generated",
                Details = lst,
            };
        var res = await Db.Upsert(voucher);
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
                lst.Add(new() { Date = dtNxt, Amount = residue });
                break;
            }

            var n = dtNxt.Subtract(dtCur).TotalDays + (flag ? 1 : 0);

            flag = false;

            var amount = a * n;
            residue -= amount;
            if (!amount.IsZero())
                lst.Add(new() { Date = dtNxt, Amount = amount });
            dtCur = dtNxt;
        }

        amort.Schedule = lst;
    }

    private sealed class RegisteringDetailQuery : IDetailQueryAtom
    {
        public RegisteringDetailQuery(VoucherDetail filter) => Filter = filter;

        public TitleKind? Kind => null;

        public VoucherDetail Filter { get; }

        public bool IsFundBidirectional => false;

        public int Dir => 0;

        public string ContentPrefix => null;

        public string RemarkPrefix => null;

        public bool IsDangerous() => Filter.IsDangerous();

        public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
    }

    private sealed class RegisteringQuery : IVoucherQueryAtom
    {
        public RegisteringQuery(Amortization amort)
        {
            VoucherFilter = amort.Template;
            DetailFilter = amort.Template.Details.Aggregate(
                (IQueryCompounded<IDetailQueryAtom>)DetailQueryUnconstrained.Instance, static (query, filter)
                    => new IntersectQueries<IDetailQueryAtom>(
                        query,
                        new RegisteringDetailQuery(filter)));
        }

        public bool ForAll => true;

        public Voucher VoucherFilter { get; }

        public DateFilter Range => DateFilter.Unconstrained;

        public IQueryCompounded<IDetailQueryAtom> DetailFilter { get; }

        public bool IsDangerous() => VoucherFilter.IsDangerous() && DetailFilter.IsDangerous();

        public T Accept<T>(IQueryVisitor<IVoucherQueryAtom, T> visitor) => visitor.Visit(this);
    }
}
