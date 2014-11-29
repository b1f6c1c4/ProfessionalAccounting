using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Entities
{
    public static class MatchHelper
    {
        public static bool IsMatch(this VoucherDetail voucherDetail, VoucherDetail filter)
        {
            if (filter.Item != null)
                if (filter.Item != voucherDetail.Item)
                    return false;
            if (filter.Title != null)
                if (filter.Title != voucherDetail.Title)
                    return false;
            if (filter.SubTitle != null)
                if (filter.SubTitle != voucherDetail.SubTitle)
                    return false;
            if (filter.Content != null)
                if (filter.Content != voucherDetail.Content)
                    return false;
            if (filter.Fund != null)
                if (filter.Fund != voucherDetail.Fund)
                    return false;
            if (filter.Remark != null)
                if (filter.Remark != voucherDetail.Remark)
                    return false;
            return true;
        }
    }
}
