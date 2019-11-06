namespace AccountingServer.Entities.Util
{
    public static class SecurityHelper
    {
        /// <summary>
        ///     是否包含弱检索式
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <returns>若包含则为<c>true</c>，否则为<c>false</c></returns>
        public static bool IsDangerous(this VoucherDetail filter)
        {
            if (filter.Fund.HasValue)
                return false;
            if (!string.IsNullOrEmpty(filter.Content))
                return false;
            if (!string.IsNullOrEmpty(filter.Remark))
                return false;

            return true;
        }

        /// <summary>
        ///     是否包含弱检索式
        /// </summary>
        /// <param name="filter">记账凭证过滤器</param>
        /// <returns>若包含则为<c>true</c>，否则为<c>false</c></returns>
        public static bool IsDangerous(this Voucher filter)
        {
            if (filter.ID != null)
                return false;

            return true;
        }

        /// <summary>
        ///     是否包含弱检索式
        /// </summary>
        /// <param name="filter">分期过滤器</param>
        /// <returns>若包含则为<c>true</c>，否则为<c>false</c></returns>
        public static bool IsDangerous(this IDistributed filter)
        {
            if (filter.ID.HasValue)
                return false;
            if (filter.Name != null)
                return false;
            if (!string.IsNullOrEmpty(filter.Remark))
                return false;

            return true;
        }

        /// <summary>
        ///     是否包含弱检索式
        /// </summary>
        /// <param name="query">记账凭证细目映射检索式</param>
        /// <returns>若包含则为<c>true</c>，否则为<c>false</c></returns>
        public static bool IsDangerous(this IVoucherDetailQuery query)
            => query.VoucherQuery.IsDangerous() && (query.DetailEmitFilter?.DetailFilter?.IsDangerous() ?? true);
    }
}
