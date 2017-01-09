using System;
using System.Collections.Generic;
using AccountingServer.Entities;
using Antlr4.Runtime;

namespace AccountingServer.BLL.Parsing
{
    public abstract class FacadeBase
    {
        internal virtual T Parse<T>(ref string s, Func<QueryParser, T> func)
            where T : RuleContext
        {
            var res = func(QueryParser.From(s));
            s = s.Substring(res.GetText().Length);
            return res;
        }

        public DateTime? UniqueTime(string s)
            => Parse(ref s, p => p.uniqueTime());

        public DateTime? UniqueTime(ref string s)
            => Parse(ref s, p => p.uniqueTime());

        public DateFilter? Range(string s)
            => Parse(ref s, p => p.range())?.Range;

        public DateFilter? Range(ref string s)
            => Parse(ref s, p => p.range())?.Range;

        public IQueryCompunded<IVoucherQueryAtom> VoucherQuery(string s)
            => Parse(ref s, p => p.voucherQuery());

        public IQueryCompunded<IVoucherQueryAtom> VoucherQuery(ref string s)
            => Parse(ref s, p => p.voucherQuery());

        public IGroupedQuery GroupedQuery(string s)
            => Parse(ref s, p => p.groupedQuery());

        public IGroupedQuery GroupedQuery(ref string s)
            => Parse(ref s, p => p.groupedQuery());

        public IQueryCompunded<IDistributedQueryAtom> DistributedQuery(string s)
            => Parse(ref s, p => p.distributedQ());

        public IQueryCompunded<IDistributedQueryAtom> DistributedQuery(ref string s)
            => Parse(ref s, p => p.distributedQ());
    }

    public sealed class FacadeF : FacadeBase
    {
        private static readonly FacadeF Instance = new FacadeF();

        private FacadeF() { }

        public static FacadeBase ParsingF => Instance;
    }

    public sealed class Facade : FacadeBase
    {
        private static readonly Facade Instance = new Facade();

        private Facade() { }

        public static FacadeBase Parsing => Instance;

        internal override T Parse<T>(ref string s, Func<QueryParser, T> func)
        {
            try
            {
                return base.Parse(ref s, func);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public static class Helper
    {
        public static IEnumerable<Voucher> RunVoucherQuery(this Accountant acc, string str)
            => acc.SelectVouchers(FacadeF.ParsingF.VoucherQuery(str));

        public static IEnumerable<Balance> RunGroupedQuery(this Accountant acc, string str)
            => acc.SelectVoucherDetailsGrouped(FacadeF.ParsingF.GroupedQuery(str));
    }
}
