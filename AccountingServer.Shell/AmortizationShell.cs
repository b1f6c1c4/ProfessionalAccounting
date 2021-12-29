/* Copyright (C) 2020-2021 b1f6c1c4
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
using System.Globalization;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;

namespace AccountingServer.Shell;

/// <summary>
///     摊销表达式解释器
/// </summary>
internal class AmortizationShell : DistributedShell
{
    /// <inheritdoc />
    protected override string Initial => "o";

    /// <summary>
    ///     执行列表表达式
    /// </summary>
    /// <param name="distQuery">分期检索式</param>
    /// <param name="dt">计算账面价值的时间</param>
    /// <param name="showSchedule">是否显示折旧计算表</param>
    /// <param name="session">客户端会话</param>
    /// <returns>执行结果</returns>
    protected override IQueryResult ExecuteList(IQueryCompounded<IDistributedQueryAtom> distQuery, DateTime? dt,
        bool showSchedule, Session session)
    {
        var sb = new StringBuilder();
        foreach (var a in Sort(session.Accountant.SelectAmortizations(distQuery)))
            sb.Append(ListAmort(a, session, dt, showSchedule));

        return new PlainText(sb.ToString());
    }

    /// <inheritdoc />
    protected override IQueryResult ExecuteQuery(IQueryCompounded<IDistributedQueryAtom> distQuery, Session session)
        => new PlainText(session.Serializer.PresentAmorts(Sort(session.Accountant.SelectAmortizations(distQuery))));

    /// <inheritdoc />
    protected override IQueryResult ExecuteRegister(IQueryCompounded<IDistributedQueryAtom> distQuery,
        DateFilter rng,
        IQueryCompounded<IVoucherQueryAtom> query, Session session)
    {
        var sb = new StringBuilder();
        foreach (var a in Sort(session.Accountant.SelectAmortizations(distQuery)))
        {
            sb.Append(session.Serializer.PresentVouchers(session.Accountant.RegisterVouchers(a, rng, query)));
            session.Accountant.Upsert(a);
        }

        if (sb.Length > 0)
            return new DirtyText(sb.ToString());

        return new PlainSucceed();
    }

    /// <inheritdoc />
    protected override IQueryResult ExecuteUnregister(IQueryCompounded<IDistributedQueryAtom> distQuery, DateFilter rng,
        IQueryCompounded<IVoucherQueryAtom> query, Session session)
    {
        var sb = new StringBuilder();
        foreach (var a in Sort(session.Accountant.SelectAmortizations(distQuery)))
        {
            foreach (var item in a.Schedule.Where(item => item.Date.Within(rng)))
            {
                if (query != null)
                {
                    if (item.VoucherID == null)
                        continue;

                    var voucher = session.Accountant.SelectVoucher(item.VoucherID);
                    if (voucher != null)
                        if (!MatchHelper.IsMatch(query, voucher.IsMatch))
                            continue;
                }

                item.VoucherID = null;
            }

            sb.Append(ListAmort(a, session));
            session.Accountant.Upsert(a);
        }

        if (sb.Length > 0)
            return new DirtyText(sb.ToString());

        return new PlainSucceed();
    }

    /// <inheritdoc />
    protected override IQueryResult ExecuteRecal(IQueryCompounded<IDistributedQueryAtom> distQuery, Session session)
    {
        var lst = new List<Amortization>();
        foreach (var a in Sort(session.Accountant.SelectAmortizations(distQuery)))
        {
            Accountant.Amortize(a);
            session.Accountant.Upsert(a);
            lst.Add(a);
        }

        return new DirtyText(session.Serializer.PresentAmorts(lst));
    }

    /// <inheritdoc />
    protected override IQueryResult ExecuteResetSoft(IQueryCompounded<IDistributedQueryAtom> distQuery, DateFilter rng,
        Session session)
    {
        var cnt = 0L;
        foreach (var a in session.Accountant.SelectAmortizations(distQuery))
        {
            if (a.Schedule == null)
                continue;

            var flag = false;
            foreach (var item in a.Schedule.Where(item => item.Date.Within(rng))
                         .Where(item => item.VoucherID != null)
                         .Where(item => session.Accountant.SelectVoucher(item.VoucherID) == null))
            {
                item.VoucherID = null;
                cnt++;
                flag = true;
            }

            if (flag)
                session.Accountant.Upsert(a);
        }

        return new NumberAffected(cnt);
    }

    /// <inheritdoc />
    protected override IQueryResult ExecuteResetMixed(IQueryCompounded<IDistributedQueryAtom> distQuery, DateFilter rng,
        Session session)
    {
        var cnt = 0L;
        foreach (var a in session.Accountant.SelectAmortizations(distQuery))
        {
            if (a.Schedule == null)
                continue;

            var flag = false;
            foreach (var item in a.Schedule.Where(item => item.Date.Within(rng))
                         .Where(item => item.VoucherID != null))
            {
                var voucher = session.Accountant.SelectVoucher(item.VoucherID);
                if (voucher == null)
                {
                    item.VoucherID = null;
                    cnt++;
                    flag = true;
                }
                else if (session.Accountant.DeleteVoucher(voucher.ID))
                {
                    item.VoucherID = null;
                    cnt++;
                    flag = true;
                }
            }

            if (flag)
                session.Accountant.Upsert(a);
        }

        return new NumberAffected(cnt);
    }

    /// <inheritdoc />
    protected override IQueryResult ExecuteResetHard(IQueryCompounded<IDistributedQueryAtom> distQuery,
        IQueryCompounded<IVoucherQueryAtom> query, Session session) => throw new InvalidOperationException();

    /// <inheritdoc />
    protected override IQueryResult ExecuteApply(IQueryCompounded<IDistributedQueryAtom> distQuery, DateFilter rng,
        bool isCollapsed, Session session)
    {
        var sb = new StringBuilder();
        foreach (var a in Sort(session.Accountant.SelectAmortizations(distQuery)))
        {
            foreach (var item in session.Accountant.Update(a, rng, isCollapsed))
                sb.AppendLine(ListAmortItem(item));

            session.Accountant.Upsert(a);
        }

        if (sb.Length > 0)
            return new DirtyText(sb.ToString());

        return new PlainSucceed();
    }

    /// <inheritdoc />
    protected override IQueryResult ExecuteCheck(IQueryCompounded<IDistributedQueryAtom> distQuery, DateFilter rng,
        Session session)
    {
        var sb = new StringBuilder();
        foreach (var a in Sort(session.Accountant.SelectAmortizations(distQuery)))
        {
            var sbi = new StringBuilder();
            foreach (var item in session.Accountant.Update(a, rng, false, true))
                sbi.AppendLine(ListAmortItem(item));

            if (sbi.Length != 0)
            {
                sb.AppendLine(ListAmort(a, session, null, false));
                sb.AppendLine(sbi.ToString());
            }

            session.Accountant.Upsert(a);
        }

        if (sb.Length > 0)
            return new DirtyText(sb.ToString());

        return new PlainSucceed();
    }

    /// <summary>
    ///     显示摊销及其计算表
    /// </summary>
    /// <param name="amort">摊销</param>
    /// <param name="session">客户端会话</param>
    /// <param name="dt">计算账面价值的时间</param>
    /// <param name="showSchedule">是否显示计算表</param>
    /// <returns>格式化的信息</returns>
    private string ListAmort(Amortization amort, Session session, DateTime? dt = null, bool showSchedule = true)
    {
        var sb = new StringBuilder();

        var bookValue = Accountant.GetBookValueOn(amort, dt);
        if (dt.HasValue &&
            !bookValue?.IsZero() != true)
            return null;

        sb.AppendLine(
            $"{amort.StringID} {amort.Name.CPadRight(35)}{amort.Date:yyyyMMdd}" +
            $"U{amort.User.AsUser().CPadRight(5)} " +
            $"{amort.Value.AsCurrency().CPadLeft(13)}{(dt.HasValue ? bookValue.AsCurrency().CPadLeft(13) : "-".CPadLeft(13))}" +
            $"{(amort.TotalDays?.ToString(CultureInfo.InvariantCulture) ?? "-").CPadLeft(4)}{amort.Interval.ToString().CPadLeft(20)}");
        if (showSchedule && amort.Schedule != null)
            foreach (var amortItem in amort.Schedule)
            {
                sb.AppendLine(ListAmortItem(amortItem));
                if (amortItem.VoucherID != null)
                    sb.AppendLine(session.Serializer
                        .PresentVoucher(session.Accountant.SelectVoucher(amortItem.VoucherID)).Wrap());
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
