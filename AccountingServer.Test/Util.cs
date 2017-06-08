using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.Test
{
    public static class Util
    {
        public static DateTime? ToDateTime(this string b1S) => b1S == null
            ? (DateTime?)null
            : DateTime.Parse(b1S).CastUtc();
    }

    public class VoucherEqualityComparer : IEqualityComparer<Voucher>
    {
        public bool Equals(Voucher x, Voucher y)
        {
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

            return x.Remark == y.Remark;
        }

        public int GetHashCode(VoucherDetail obj) => obj.Fund?.GetHashCode() ?? 0;
    }

    public class MockConfigManager<T> : IConfigManager<T>
    {
        public T Config { get; }

        public MockConfigManager(T config) => Config = config;
    }
}
