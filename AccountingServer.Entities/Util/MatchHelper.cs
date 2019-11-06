using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AccountingServer.Entities.Util
{
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
                        if (voucher.Type == VoucherType.Carry ||
                            voucher.Type == VoucherType.AnnualCarry)
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
        /// <param name="dir">借贷方向</param>
        /// <returns>是否符合</returns>
        public static bool IsMatch(this VoucherDetail voucherDetail, VoucherDetail filter, int dir = 0)
        {
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

            if (filter.Fund != null)
                if (!voucherDetail.Fund.HasValue ||
                    !(filter.Fund.Value - voucherDetail.Fund.Value).IsZero())
                    return false;

            if (dir != 0)
                if (!voucherDetail.Fund.HasValue ||
                    dir > 0 && voucherDetail.Fund < -VoucherDetail.Tolerance ||
                    dir < 0 && voucherDetail.Fund > +VoucherDetail.Tolerance)
                    return false;

            if (filter.Remark != null)
                if (filter.Remark == string.Empty)
                {
                    if (!string.IsNullOrEmpty(voucherDetail.Remark))
                        return false;
                }
                else if (filter.Remark != voucherDetail.Remark)
                    return false;

            return true;
        }

        public static bool IsMatch(this VoucherDetail voucherDetail, IQueryCompunded<IDetailQueryAtom> query)
            => IsMatch(query, q => IsMatch(voucherDetail, q.Filter, q.Dir));

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
        public static bool IsMatch(this Voucher voucher, IQueryCompunded<IVoucherQueryAtom> query)
            => IsMatch(query, q => IsMatch(voucher, q));

        /// <summary>
        ///     判断一般检索式是否成立
        /// </summary>
        /// <param name="query">检索式</param>
        /// <param name="atomPredictor">原子检索式成立条件</param>
        /// <returns>是否成立</returns>
        public static bool IsMatch<TAtom>(IQueryCompunded<TAtom> query, Func<TAtom, bool> atomPredictor)
            where TAtom : class => query?.Accept(new MatchHelperVisitor<TAtom>(atomPredictor)) ?? true;

        private sealed class MatchHelperVisitor<TAtom> : IQueryVisitor<TAtom, bool> where TAtom : class
        {
            private readonly Func<TAtom, bool> m_AtomPredictor;

            public MatchHelperVisitor(Func<TAtom, bool> atomPredictor) => m_AtomPredictor = atomPredictor;

            public bool Visit(TAtom query) => m_AtomPredictor(query);

            public bool Visit(IQueryAry<TAtom> query)
            {
                switch (query.Operator)
                {
                    case OperatorType.None:
                    case OperatorType.Identity:
                        return query.Filter1.Accept(this);
                    case OperatorType.Complement:
                        return !query.Filter1.Accept(this);
                    case OperatorType.Union:
                        return query.Filter1.Accept(this) || query.Filter2.Accept(this);
                    case OperatorType.Intersect:
                        return query.Filter1.Accept(this) && query.Filter2.Accept(this);
                    case OperatorType.Substract:
                        return query.Filter1.Accept(this) && !query.Filter2.Accept(this);
                    default:
                        throw new ArgumentException("运算类型未知", nameof(query));
                }
            }
        }
    }
}
