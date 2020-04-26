using System;
using System.Diagnostics.CodeAnalysis;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using Antlr4.Runtime;

namespace AccountingServer.BLL.Parsing
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
    public abstract class FacadeBase
    {
        private static T Check<T>(ref string s, T res)
            where T : RuleContext
        {
            var t = res.GetText();
            var i = 0;
            for (var j = 0; j < t.Length; i++)
            {
                if (s[i] == t[j])
                {
                    j++;
                    continue;
                }

                if (!char.IsWhiteSpace(s[i]))
                    throw new ApplicationException("内部错误");
            }

            s = s.Substring(i);
            return res;
        }

        internal virtual T QueryParse<T>(ref string s, Func<QueryParser, T> func)
            where T : RuleContext
        {
            var stream = new AntlrInputStream(s);
            var lexer = new QueryLexer(stream);
            lexer.RemoveErrorListeners();
            var tokens = new CommonTokenStream(lexer);
            var parser = new QueryParser(tokens) { ErrorHandler = new BailErrorStrategy() };
            parser.RemoveErrorListeners();
            return Check(ref s, func(parser));
        }

        internal virtual T SubtotalParse<T>(ref string s, Func<SubtotalParser, T> func)
            where T : RuleContext
        {
            var stream = new AntlrInputStream(s);
            var lexer = new SubtotalLexer(stream);
            lexer.RemoveErrorListeners();
            var tokens = new CommonTokenStream(lexer);
            var parser = new SubtotalParser(tokens) { ErrorHandler = new BailErrorStrategy() };
            parser.RemoveErrorListeners();
            return Check(ref s, func(parser));
        }

        public ITitle Title(string s)
            => Title(ref s);

        public ITitle Title(ref string s)
            => QueryParse(ref s, p => p.title());

        public DateTime? UniqueTime(string s)
            => UniqueTime(ref s);

        public DateTime? UniqueTime(ref string s)
            => QueryParse(ref s, p => p.uniqueTime());

        public DateFilter Range(string s)
            => Range(ref s);

        public DateFilter Range(ref string s)
            => QueryParse(ref s, p => p.range())?.Range;

        public IQueryCompounded<IVoucherQueryAtom> VoucherQuery(string s)
            => VoucherQuery(ref s);

        public IQueryCompounded<IVoucherQueryAtom> VoucherQuery(ref string s)
            => (IQueryCompounded<IVoucherQueryAtom>)QueryParse(ref s, p => p.vouchers()) ??
                VoucherQueryUnconstrained.Instance;

        public IVoucherDetailQuery DetailQuery(string s)
            => DetailQuery(ref s);

        public IVoucherDetailQuery DetailQuery(ref string s)
            => QueryParse(ref s, p => p.voucherDetailQuery());

        public ISubtotal Subtotal(string s)
            => Subtotal(ref s);

        public ISubtotal Subtotal(ref string s)
            => SubtotalParse(ref s, p => p.subtotal());

        public IVoucherGroupedQuery VoucherGroupedQuery(string s)
            => VoucherGroupedQuery(ref s);

        public virtual IVoucherGroupedQuery VoucherGroupedQuery(ref string s)
        {
            var raw = s;
            var query = VoucherQuery(ref s);
            var subtotal = Subtotal(ref s);
            if (subtotal.GatherType != GatheringType.VoucherCount)
            {
                s = raw;
                throw new FormatException();
            }

            return new VoucherGroupedQueryStub { VoucherQuery = query, Subtotal = subtotal };
        }

        public IGroupedQuery GroupedQuery(string s)
            => GroupedQuery(ref s);

        public virtual IGroupedQuery GroupedQuery(ref string s)
        {
            var raw = s;
            var query = DetailQuery(ref s);
            var subtotal = Subtotal(ref s);
            if (subtotal.GatherType == GatheringType.VoucherCount)
            {
                s = raw;
                throw new FormatException();
            }

            return new GroupedQueryStub { VoucherEmitQuery = query, Subtotal = subtotal };
        }

        public IQueryCompounded<IDistributedQueryAtom> DistributedQuery(string s)
            => DistributedQuery(ref s);

        public IQueryCompounded<IDistributedQueryAtom> DistributedQuery(ref string s)
            => (IQueryCompounded<IDistributedQueryAtom>)QueryParse(ref s, p => p.distributedQ()) ??
                DistributedQueryUnconstrained.Instance;

        private sealed class VoucherGroupedQueryStub : IVoucherGroupedQuery
        {
            public IQueryCompounded<IVoucherQueryAtom> VoucherQuery { get; set; }

            public ISubtotal Subtotal { get; set; }
        }

        private sealed class GroupedQueryStub : IGroupedQuery
        {
            public IVoucherDetailQuery VoucherEmitQuery { get; set; }

            public ISubtotal Subtotal { get; set; }
        }
    }

    public sealed class FacadeF : FacadeBase
    {
        private FacadeF() { }

        public static FacadeBase ParsingF { get; } = new FacadeF();
    }

    public sealed class Facade : FacadeBase
    {
        private Facade() { }

        public static FacadeBase Parsing { get; } = new Facade();

        internal override T QueryParse<T>(ref string s, Func<QueryParser, T> func)
        {
            if (s == null)
                return null;

            try
            {
                return base.QueryParse(ref s, func);
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal override T SubtotalParse<T>(ref string s, Func<SubtotalParser, T> func)
        {
            if (s == null)
                return null;

            try
            {
                return base.SubtotalParse(ref s, func);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override IVoucherGroupedQuery VoucherGroupedQuery(ref string s)
        {
            try
            {
                return base.VoucherGroupedQuery(ref s);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override IGroupedQuery GroupedQuery(ref string s)
        {
            try
            {
                return base.GroupedQuery(ref s);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
