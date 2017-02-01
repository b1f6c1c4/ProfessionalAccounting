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
            var stream = new AntlrInputStream(s);
            var lexer = new QueryLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new QueryParser(tokens) { ErrorHandler = new BailErrorStrategy() };
            var res = func(parser);

            var t = res.GetText();
            var i = 0;
            var j = 0;
            while (j < t.Length)
            {
                if (s[i] == t[j])
                {
                    i++;
                    j++;
                    continue;
                }

                if (char.IsWhiteSpace(s[i]))
                    i++;
                else
                    throw new ApplicationException("内部错误");
            }

            s = s.Substring(i);
            return res;
        }

        public ITitle Title(string s)
            => Title(ref s);

        public ITitle Title(ref string s)
            => Parse(ref s, p => p.title());

        public DateTime? UniqueTime(string s)
            => UniqueTime(ref s);

        public DateTime? UniqueTime(ref string s)
            => Parse(ref s, p => p.uniqueTime());

        public DateFilter? Range(string s)
            => Range(ref s);

        public DateFilter? Range(ref string s)
            => Parse(ref s, p => p.range())?.Range;

        public IQueryCompunded<IVoucherQueryAtom> VoucherQuery(string s)
            => VoucherQuery(ref s);

        public IQueryCompunded<IVoucherQueryAtom> VoucherQuery(ref string s)
            => Parse(ref s, p => p.vouchers());

        public IGroupedQuery GroupedQuery(string s)
            => GroupedQuery(ref s);

        public IGroupedQuery GroupedQuery(ref string s)
            => Parse(ref s, p => p.groupedQuery());

        public IQueryCompunded<IDistributedQueryAtom> DistributedQuery(string s)
            => DistributedQuery(ref s);

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
            if (s == null)
                return null;

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
        /// <summary>
        ///     匹配EOF
        /// </summary>
        /// <param name="facade">占位符</param>
        /// <param name="expr">表达式</param>
        public static void Eof(this FacadeBase facade, string expr)
        {
            if (!string.IsNullOrWhiteSpace(expr))
                throw new ArgumentException("语法错误", nameof(expr));
        }

        public static IEnumerable<Voucher> RunVoucherQuery(this Accountant acc, string str)
        {
            var res = FacadeF.ParsingF.VoucherQuery(ref str);
            FacadeF.ParsingF.Eof(str);
            return acc.SelectVouchers(res);
        }

        public static long DeleteVouchers(this Accountant acc, string str)
        {
            var res = FacadeF.ParsingF.VoucherQuery(ref str);
            FacadeF.ParsingF.Eof(str);
            return acc.DeleteVouchers(res);
        }

        public static IEnumerable<Balance> RunGroupedQuery(this Accountant acc, string str)
        {
            var res = FacadeF.ParsingF.GroupedQuery(ref str);
            FacadeF.ParsingF.Eof(str);
            return acc.SelectVoucherDetailsGrouped(res);
        }
    }
}
