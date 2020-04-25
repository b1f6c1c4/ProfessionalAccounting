using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.Test
{
    public static class Util
    {
        public static DateTime? ToDateTime(this string b1S) => b1S == null
            ? (DateTime?)null
            : ClientDateTime.Parse(b1S);
    }

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

    public class MockConfigManager<T> : IConfigManager<T>
    {
        public MockConfigManager(T config) => Config = config;
        public T Config { get; }

        public void Reload(bool throws) => throw new NotImplementedException();
    }

    public class MockExchange : IExchange, IEnumerable
    {
        private readonly Dictionary<Tuple<DateTime, string>, double> m_Dic =
            new Dictionary<Tuple<DateTime, string>, double>();

        public IEnumerator GetEnumerator() => m_Dic.GetEnumerator();

        public double From(DateTime date, string target) => target == BaseCurrency.Now
            ? 1
            : m_Dic[new Tuple<DateTime, string>(date, target)];

        public double To(DateTime date, string target) => target == BaseCurrency.Now
            ? 1
            : 1D / m_Dic[new Tuple<DateTime, string>(date, target)];

        public void Add(DateTime date, string target, double val) => m_Dic.Add(
            new Tuple<DateTime, string>(date, target),
            val);
    }
}
