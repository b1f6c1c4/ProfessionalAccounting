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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AccountingServer.Entities.Util;

/// <summary>
///     判断实体是否符合各过滤器
/// </summary>
public static class MatchHelper
{
    /// <summary>
    ///     判断记账凭证是否符合记账凭证过滤器
    /// </summary>
    /// <param name="voucher">记账凭证</param>
    /// <param name="filter">记账凭证过滤器</param>
    /// <returns>是否符合</returns>
    public static bool IsMatch(this Voucher voucher, Voucher filter)
    {
        if (filter?.ID != null)
            if (filter.ID != voucher.ID)
                return false;

        if (filter?.Date != null)
            if (filter.Date != voucher.Date)
                return false;

        if (filter?.Type != null)
            switch (filter.Type)
            {
                case VoucherType.Ordinary:
                case VoucherType.Amortization:
                case VoucherType.AnnualCarry:
                case VoucherType.Carry:
                case VoucherType.Depreciation:
                case VoucherType.Devalue:
                case VoucherType.Uncertain:
                    if (filter.Type != voucher.Type)
                        return false;

                    break;
                case VoucherType.General:
                    if (voucher.Type is VoucherType.Carry or VoucherType.AnnualCarry)
                        return false;

                    break;
            }

        if (filter?.Remark != null)
            if (filter.Remark == string.Empty)
            {
                if (!string.IsNullOrEmpty(voucher.Remark))
                    return false;
            }
            else if (filter.Remark != voucher.Remark)
                return false;

        return true;
    }

    /// <summary>
    ///     判断细目是否符合细目过滤器
    /// </summary>
    /// <param name="voucherDetail">细目</param>
    /// <param name="filter">细目过滤器</param>
    /// <param name="kind">科目类型</param>
    /// <param name="dir">借贷方向</param>
    /// <param name="contentPrefix">内容前缀</param>
    /// <param name="remarkPrefix">备注前缀</param>
    /// <returns>是否符合</returns>
    public static bool IsMatch(this VoucherDetail voucherDetail, VoucherDetail filter, TitleKind? kind = null,
        int dir = 0, string contentPrefix = null, string remarkPrefix = null)
    {
        switch (kind)
        {
            case TitleKind.Asset when voucherDetail.Title is not (>= 1000 and < 2000):
            case TitleKind.Liability when voucherDetail.Title is not (>= 2000 and < 3000):
            case TitleKind.Equity when voucherDetail.Title is not (>= 4000 and < 5000):
            case TitleKind.Revenue when voucherDetail.Title is not (>= 6000 and < 6400):
            case TitleKind.Expense when voucherDetail.Title is not (>= 6400 and < 7000):
                return false;
        }

        if (filter == null)
            return true;

        if (filter.User != null)
            if (filter.User != voucherDetail.User)
                return false;

        if (filter.Currency != null)
            if (filter.Currency != voucherDetail.Currency)
                return false;

        if (filter.Title != null)
            if (filter.Title != voucherDetail.Title)
                return false;

        if (filter.SubTitle != null)
            if (filter.SubTitle == 00)
            {
                if (voucherDetail.SubTitle != null)
                    return false;
            }
            else if (filter.SubTitle != voucherDetail.SubTitle)
                return false;

        if (filter.Content != null)
            if (filter.Content == string.Empty)
            {
                if (!string.IsNullOrEmpty(voucherDetail.Content))
                    return false;
            }
            else if (filter.Content != voucherDetail.Content)
                return false;

        if (contentPrefix != null)
            if (voucherDetail.Content?.StartsWith(contentPrefix, StringComparison.InvariantCultureIgnoreCase) != true)
                return false;

        if (filter.Fund != null)
            if (!voucherDetail.Fund.HasValue ||
                !(filter.Fund.Value - voucherDetail.Fund.Value).IsZero())
                return false;

        if (dir != 0)
            if (!voucherDetail.Fund.HasValue ||
                (dir > 0 && voucherDetail.Fund < -VoucherDetail.Tolerance) ||
                (dir < 0 && voucherDetail.Fund > +VoucherDetail.Tolerance))
                return false;

        if (filter.Remark != null)
            if (filter.Remark == string.Empty)
            {
                if (!string.IsNullOrEmpty(voucherDetail.Remark))
                    return false;
            }
            else if (filter.Remark != voucherDetail.Remark)
                return false;

        if (remarkPrefix != null)
            if (voucherDetail.Remark?.StartsWith(remarkPrefix, StringComparison.InvariantCultureIgnoreCase) != true)
                return false;

        return true;
    }

    public static bool IsMatch(this VoucherDetail voucherDetail, IQueryCompounded<IDetailQueryAtom> query)
        => IsMatch(query, q => IsMatch(voucherDetail, q.Filter, q.Kind, q.Dir, q.ContentPrefix, q.RemarkPrefix));

    /// <summary>
    ///     判断记账凭证是否符合记账凭证检索式
    /// </summary>
    /// <param name="voucher">记账凭证</param>
    /// <param name="query">记账凭证检索式</param>
    /// <returns>是否符合</returns>
    public static bool IsMatch(this Voucher voucher, IVoucherQueryAtom query)
    {
        if (!voucher.IsMatch(query.VoucherFilter))
            return false;
        if (!voucher.Date.Within(query.Range))
            return false;

        return query.ForAll
            ? voucher.Details.All(d => d.IsMatch(query.DetailFilter))
            : voucher.Details.Any(d => d.IsMatch(query.DetailFilter));
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static bool IsMatch(this Voucher voucher, IQueryCompounded<IVoucherQueryAtom> query)
        => IsMatch(query, q => IsMatch(voucher, q));

    /// <summary>
    ///     判断一般检索式是否成立
    /// </summary>
    /// <param name="query">检索式</param>
    /// <param name="atomPredictor">原子检索式成立条件</param>
    /// <returns>是否成立</returns>
    public static bool IsMatch<TAtom>(IQueryCompounded<TAtom> query, Func<TAtom, bool> atomPredictor)
        where TAtom : class => query?.Accept(new MatchHelperVisitor<TAtom>(atomPredictor)) ?? true;

    /// <summary>
    ///     获取记账凭证细目映射检索式中的实际细目检索式
    /// </summary>
    /// <param name="query">记账凭证细目映射检索式</param>
    /// <returns>细目检索式</returns>
    public static IQueryCompounded<IDetailQueryAtom> ActualDetailFilter(this IVoucherDetailQuery query)
        => query.DetailEmitFilter != null
            ? query.DetailEmitFilter.DetailFilter
            : query.VoucherQuery is IVoucherQueryAtom dQuery
                ? dQuery.DetailFilter
                : throw new ArgumentException("不指定细目映射检索式时记账凭证检索式为复合检索式", nameof(query));

    private sealed class MatchHelperVisitor<TAtom> : IQueryVisitor<TAtom, bool> where TAtom : class
    {
        private readonly Func<TAtom, bool> m_AtomPredictor;

        public MatchHelperVisitor(Func<TAtom, bool> atomPredictor) => m_AtomPredictor = atomPredictor;

        public bool Visit(TAtom query) => m_AtomPredictor(query);

        public bool Visit(IQueryAry<TAtom> query)
            => query.Operator switch
                {
                    OperatorType.None => query.Filter1.Accept(this),
                    OperatorType.Identity => query.Filter1.Accept(this),
                    OperatorType.Complement => !query.Filter1.Accept(this),
                    OperatorType.Union => query.Filter1.Accept(this) || query.Filter2.Accept(this),
                    OperatorType.Intersect => query.Filter1.Accept(this) && query.Filter2.Accept(this),
                    OperatorType.Subtract => query.Filter1.Accept(this) && !query.Filter2.Accept(this),
                    _ => throw new ArgumentException("运算类型未知", nameof(query)),
                };
    }
}
