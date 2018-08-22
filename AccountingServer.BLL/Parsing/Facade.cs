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

        internal virtual T Parse<T>(ref string s, Func<QueryParser, T> func)
            where T : RuleContext
        {
            var stream = new AntlrInputStream(s);
            var lexer = new QueryLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new QueryParser(tokens) { ErrorHandler = new BailErrorStrategy() };
            return Check(ref s, func(parser));
        }

        internal virtual T SubtotalParse<T>(ref string s, Func<SubtotalParser, T> func)
            where T : RuleContext
        {
            var stream = new AntlrInputStream(s);
            var lexer = new SubtotalLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SubtotalParser(tokens) { ErrorHandler = new BailErrorStrategy() };
            return Check(ref s, func(parser));
        }

        public ITitle Title(string s)
            => Title(ref s);

        public ITitle Title(ref string s)
            => Parse(ref s, p => p.title());

        public DateTime? UniqueTime(string s)
            => UniqueTime(ref s);

        public DateTime? UniqueTime(ref string s)
            => Parse(ref s, p => p.uniqueTime());

        public DateFilter Range(string s)
            => Range(ref s);

        public DateFilter Range(ref string s)
            => Parse(ref s, p => p.range())?.Range;

        public IQueryCompunded<IVoucherQueryAtom> VoucherQuery(string s)
            => VoucherQuery(ref s);

        public IQueryCompunded<IVoucherQueryAtom> VoucherQuery(ref string s)
            => (IQueryCompunded<IVoucherQueryAtom>)Parse(ref s, p => p.vouchers()) ??
                VoucherQueryUnconstrained.Instance;

        public IVoucherDetailQuery DetailQuery(string s)
            => DetailQuery(ref s);

        public IVoucherDetailQuery DetailQuery(ref string s)
            => Parse(ref s, p => p.voucherDetailQuery());

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

            return new VoucherGroupedQueryStub
                {
                    VoucherQuery = query,
                    Subtotal = subtotal
                };
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

            return new GroupedQueryStub
                {
                    VoucherEmitQuery = query,
                    Subtotal = subtotal
                };
        }

        public IQueryCompunded<IDistributedQueryAtom> DistributedQuery(string s)
            => DistributedQuery(ref s);

        public IQueryCompunded<IDistributedQueryAtom> DistributedQuery(ref string s)
            => (IQueryCompunded<IDistributedQueryAtom>)Parse(ref s, p => p.distributedQ()) ??
                DistributedQueryUnconstrained.Instance;

        private sealed class VoucherGroupedQueryStub : IVoucherGroupedQuery
        {
            public IQueryCompunded<IVoucherQueryAtom> VoucherQuery { get; set; }

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
