using System;
using System.Collections.Generic;
using System.Linq;

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
        ///     判断细目是否符合过滤器
        /// </summary>
        /// <param name="voucherDetail">细目</param>
        /// <param name="filters">过滤器</param>
        /// <param name="useAnd">各细目过滤器之间的关系为合取</param>
        /// <returns>是否符合</returns>
        public static bool IsMatch(this VoucherDetail voucherDetail, IEnumerable<VoucherDetail> filters,
                                   bool useAnd = false)
        {
            return useAnd ? filters.All(voucherDetail.IsMatch) : filters.Any(voucherDetail.IsMatch);
        }
    }
}
