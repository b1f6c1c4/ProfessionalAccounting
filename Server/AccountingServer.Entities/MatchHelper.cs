using System;
using System.Linq;

namespace AccountingServer.Entities
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
            if (filter == null)
                return true;
            if (filter.ID != null)
                if (filter.ID != voucher.ID)
                    return false;
            if (filter.Date != null)
                if (filter.Date != voucher.Date)
                    return false;
            if (filter.Type != null)
                if (filter.Type != voucher.Type)
                    return false;
            if (filter.Remark != null)
                if (filter.Remark == String.Empty)
                {
                    if (!String.IsNullOrEmpty(voucher.Remark))
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
                if (filter.Content == String.Empty)
                {
                    if (!String.IsNullOrEmpty(voucherDetail.Content))
                        return false;
                }
                else if (filter.Content != voucherDetail.Content)
                    return false;
            if (filter.Fund != null)
                if (filter.Fund != voucherDetail.Fund)
                    return false;
            if (dir != 0)
                if (dir > 0 && voucherDetail.Fund < 0 ||
                    dir < 0 && voucherDetail.Fund > 0)
                    return false;
            if (filter.Remark != null)
                if (filter.Remark == String.Empty)
                {
                    if (!String.IsNullOrEmpty(voucherDetail.Remark))
                        return false;
                }
                else if (filter.Remark != voucherDetail.Remark)
                    return false;
            return true;
        }

        public static bool IsMatch(this VoucherDetail voucherDetail, IQueryCompunded<IDetailQueryAtom> query)
        {
            return IsMatch(query, q => IsMatch(voucherDetail, q.Filter, q.Dir));
        }

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

        /// <summary>
        ///     判断一般检索式是否成立
        /// </summary>
        /// <param name="query">检索式</param>
        /// <param name="atomPredictor">原子检索式成立条件</param>
        /// <returns>是否成立</returns>
        public static bool IsMatch<TAtom>(IQueryCompunded<TAtom> query, Func<TAtom, bool> atomPredictor)
            where TAtom : class
        {
            if (query is TAtom)
                return atomPredictor(query as TAtom);
            if (query is IQueryAry<TAtom>)
            {
                var f = query as IQueryAry<TAtom>;

                switch (f.Operator)
                {
                    case OperatorType.None:
                    case OperatorType.Identity:
                        return IsMatch(f.Filter1, atomPredictor);
                    case OperatorType.Complement:
                        return !IsMatch(f.Filter1, atomPredictor);
                    case OperatorType.Union:
                        return IsMatch(f.Filter1, atomPredictor) || IsMatch(f.Filter2, atomPredictor);
                    case OperatorType.Intersect:
                        return IsMatch(f.Filter1, atomPredictor) && IsMatch(f.Filter2, atomPredictor);
                    case OperatorType.Substract:
                        return IsMatch(f.Filter1, atomPredictor) && ! IsMatch(f.Filter2, atomPredictor);
                    default:
                        throw new InvalidOperationException();
                }
            }
            throw new InvalidOperationException();
        }
    }
}
