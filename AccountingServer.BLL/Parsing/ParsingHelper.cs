using System;
using AccountingServer.Entities;
using Antlr4.Runtime;

namespace AccountingServer.BLL.Parsing
{
    public abstract class ParsingHelperBase
    {
        protected virtual T Parse<T>(ref string s, Func<QueryParser, T> func)
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

    public sealed class ParsingHelperF : ParsingHelperBase
    {
        private static readonly ParsingHelperF Instance = new ParsingHelperF();

        private ParsingHelperF() { }

        public static ParsingHelperBase ParsingF => Instance;
    }

    public sealed class ParsingHelper : ParsingHelperBase
    {
        private static readonly ParsingHelper Instance = new ParsingHelper();

        private ParsingHelper() { }

        public static ParsingHelperBase Parsing => Instance;

        protected override T Parse<T>(ref string s, Func<QueryParser, T> func)
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
}
