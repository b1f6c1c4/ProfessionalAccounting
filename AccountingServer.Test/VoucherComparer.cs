using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.Test
{
    public class VoucherEqualityComparer : IEqualityComparer<Voucher>
    {
        public bool Equals(Voucher x, Voucher y)
        {
            if (x == null &&
                y == null)
                return true;
            if (x == null ||
                y == null)
                return false;
            if (x.ID != y.ID)
                return false;
            if (x.Date != y.Date)
                return false;
            if (x.Type != y.Type)
                return false;
            if (x.Remark != y.Remark)
                return false;

            return x.Details.SequenceEqual(y.Details, new DetailEqualityComparer());
        }

        public int GetHashCode(Voucher obj) => obj.ID?.GetHashCode() ?? 0;
    }

    public class DetailEqualityComparer : IEqualityComparer<VoucherDetail>
    {
        public bool Equals(VoucherDetail x, VoucherDetail y)
        {
            if (x == null &&
                y == null)
                return true;
            if (x == null ||
                y == null)
                return false;
            if (x.User != y.User)
                return false;
            if (x.Currency != y.Currency)
                return false;
            if (x.Title != y.Title)
                return false;
            if (x.SubTitle != y.SubTitle)
                return false;
            if (x.Fund.HasValue != y.Fund.HasValue)
                return false;
            if (x.Fund.HasValue &&
                y.Fund.HasValue)
                if (!(x.Fund.Value - y.Fund.Value).IsZero())
                    return false;
            if (x.Content != y.Content)
                return false;

            return x.Remark == y.Remark;
        }

        public int GetHashCode(VoucherDetail obj) => obj.Fund?.GetHashCode() ?? 0;
    }

    public class BalanceEqualityComparer : IEqualityComparer<Balance>
    {
        public bool Equals(Balance x, Balance y)
        {
            if (x == null &&
                y == null)
                return true;
            if (x == null ||
                y == null)
                return false;
            if (x.User != y.User)
                return false;
            if (x.Currency != y.Currency)
                return false;
            if (x.Title != y.Title)
                return false;
            if (x.SubTitle != y.SubTitle)
                return false;
            if (!(x.Fund - y.Fund).IsZero())
                return false;
            if (x.Content != y.Content)
                return false;

            return x.Remark == y.Remark;
        }

        public int GetHashCode(Balance obj) => obj.Fund.GetHashCode();
    }
    
    public class DistributedItemEqualityComparer : IEqualityComparer<IDistributedItem>
    {
        public bool Equals(IDistributedItem x, IDistributedItem y)
        {
            if (x == null &&
                y == null)
                return true;
            if (x == null ||
                y == null)
                return false;
            if (x.VoucherID != y.VoucherID)
                return false;
            if (x.Date != y.Date)
                return false;
            if (!(x.Value - y.Value).IsZero())
                return false;
            if (x.Remark != y.Remark)
                return false;

            return true;
        }

        public int GetHashCode(IDistributedItem obj) => obj.Date?.GetHashCode() ?? 0;
    }
}
