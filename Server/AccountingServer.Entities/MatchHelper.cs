using System;

namespace AccountingServer.Entities
{
    /// <summary>
    ///     判断实体是否符合过滤器
    /// </summary>
    public static class MatchHelper
    {
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
    }
}
