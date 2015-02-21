using System;

namespace AccountingServer.Entities
{
    /// <summary>
    ///     判断实体是否符合过滤器
    /// </summary>
    public static class MatchHelper
    {
        /// <summary>
        ///     判断凭证是否符合过滤器
        /// </summary>
        /// <param name="voucher">凭证</param>
        /// <param name="filter">过滤器</param>
        /// <returns>是否符合</returns>
        // ReSharper disable once UnusedMember.Global
        public static bool IsMatch(this Voucher voucher, Voucher filter)
        {
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
        ///     判断细目是否符合过滤器
        /// </summary>
        /// <param name="voucherDetail">细目</param>
        /// <param name="filter">过滤器</param>
        /// <returns>是否符合</returns>
        public static bool IsMatch(this VoucherDetail voucherDetail, VoucherDetail filter)
        {
            if (filter.Item != null)
                if (filter.Item != voucherDetail.Item)
                    return false;
            if (filter.Title != null)
                if (filter.Title != voucherDetail.Title)
                    return false;
            if (filter.SubTitle != null)
                if (filter.SubTitle == 0)
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

        /// <summary>
        ///     判断细目是否符合检索式
        /// </summary>
        /// <param name="voucherDetail">细目</param>
        /// <param name="query">细目检索式</param>
        /// <returns>是否符合</returns>
        public static bool IsMatch(this VoucherDetail voucherDetail, IDetailQueryCompounded query)
        {
            if (query is IDetailQueryAtom)
            {
                var f = query as IDetailQueryAtom;
                if (!IsMatch(voucherDetail, f.Filter))
                    return false;
                return f.Dir == 0 || f.Dir > 0 && voucherDetail.Fund > 0 || f.Dir < 0 && voucherDetail.Fund < 0;
            }
            if (query is IDetailQueryAry)
            {
                var f = query as IDetailQueryAry;
                switch (f.Operator)
                {
                    case OperatorType.None:
                    case OperatorType.Identity:
                        return IsMatch(voucherDetail, f.Filter1);
                    case OperatorType.Complement:
                        return !IsMatch(voucherDetail, f.Filter1);
                    case OperatorType.Union:
                        return IsMatch(voucherDetail, f.Filter1) || IsMatch(voucherDetail, f.Filter2);
                    case OperatorType.Interect:
                        return IsMatch(voucherDetail, f.Filter1) && IsMatch(voucherDetail, f.Filter2);
                    case OperatorType.Substract:
                        return IsMatch(voucherDetail, f.Filter1) && !IsMatch(voucherDetail, f.Filter2);
                }
                throw new InvalidOperationException();
            }
            throw new InvalidOperationException();
        }
    }
}
