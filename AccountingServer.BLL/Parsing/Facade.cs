/* Copyright (C) 2020-2022 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.Diagnostics.CodeAnalysis;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using Antlr4.Runtime;

namespace AccountingServer.BLL.Parsing;

internal interface IDateRange
{
    DateFilter Range { get; }
}

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

        s = s[i..];
        return res;
    }

    internal virtual T QueryParse<T>(ref string s, Func<QueryParser, T> func, Client client)
        where T : RuleContext
    {
        var stream = new AntlrInputStream(s);
        var lexer = new QueryLexer(stream);
        lexer.RemoveErrorListeners();
        var tokens = new CommonTokenStream(lexer);
        var parser = new QueryParser(tokens) { ErrorHandler = new BailErrorStrategy() };
        parser.RemoveErrorListeners();
        var ctx = Check(ref s, func(parser));
        (ctx as IClientDependable).Assign(client);
        return ctx;
    }

    internal virtual T SubtotalParse<T>(ref string s, Func<SubtotalParser, T> func, Client client)
        where T : RuleContext
    {
        var stream = new AntlrInputStream(s);
        var lexer = new SubtotalLexer(stream);
        lexer.RemoveErrorListeners();
        var tokens = new CommonTokenStream(lexer);
        var parser = new SubtotalParser(tokens) { ErrorHandler = new BailErrorStrategy() };
        parser.RemoveErrorListeners();
        var ctx = Check(ref s, func(parser));
        (ctx as IClientDependable).Assign(client);
        return ctx;
    }

    public ITitle Title(string s)
        => Title(ref s);

    public ITitle Title(ref string s)
        => QueryParse(ref s, static p => p.title(), null);

    public DateTime? UniqueTime(string s, Client client)
        => UniqueTime(ref s, client);

    public DateTime? UniqueTime(ref string s, Client client)
        => QueryParse(ref s, static p => p.uniqueTime(), client)?.AsDate();

    public DateFilter Range(string s, Client client)
        => Range(ref s, client);

    public DateFilter Range(ref string s, Client client)
        => QueryParse(ref s, static p => p.range(), client)?.Range;

    public IQueryCompounded<IVoucherQueryAtom> VoucherQuery(string s, Client client)
        => VoucherQuery(ref s, client);

    public IQueryCompounded<IVoucherQueryAtom> VoucherQuery(ref string s, Client client)
        => (IQueryCompounded<IVoucherQueryAtom>)QueryParse(ref s, static p => p.vouchers(), client) ??
            VoucherQueryUnconstrained.Instance;

    public IVoucherDetailQuery DetailQuery(string s, Client client)
        => DetailQuery(ref s, client);

    public IVoucherDetailQuery DetailQuery(ref string s, Client client)
        => QueryParse(ref s, static p => p.voucherDetailQuery(), client);

    public ISubtotal Subtotal(string s, Client client)
        => Subtotal(ref s, client);

    public ISubtotal Subtotal(ref string s, Client client)
        => SubtotalParse(ref s, static p => p.subtotal(), client);

    public IVoucherGroupedQuery VoucherGroupedQuery(string s, Client client)
        => VoucherGroupedQuery(ref s, client);

    public virtual IVoucherGroupedQuery VoucherGroupedQuery(ref string s, Client client)
    {
        var raw = s;
        var query = VoucherQuery(ref s, client);
        var subtotal = Subtotal(ref s, client);
        if (subtotal.GatherType != GatheringType.VoucherCount)
        {
            s = raw;
            throw new FormatException();
        }

        return new VoucherGroupedQueryStub { VoucherQuery = query, Subtotal = subtotal };
    }

    public IGroupedQuery GroupedQuery(string s, Client client)
        => GroupedQuery(ref s, client);

    public virtual IGroupedQuery GroupedQuery(ref string s, Client client)
    {
        var raw = s;
        var query = DetailQuery(ref s, client);
        var subtotal = Subtotal(ref s, client);
        if (subtotal.GatherType == GatheringType.VoucherCount)
        {
            s = raw;
            throw new FormatException();
        }

        return new GroupedQueryStub { VoucherEmitQuery = query, Subtotal = subtotal };
    }

    public IQueryCompounded<IDistributedQueryAtom> DistributedQuery(string s, Client client)
        => DistributedQuery(ref s, client);

    public IQueryCompounded<IDistributedQueryAtom> DistributedQuery(ref string s, Client client)
        => (IQueryCompounded<IDistributedQueryAtom>)QueryParse(ref s, static p => p.distributedQ(), client) ??
            DistributedQueryUnconstrained.Instance;

    private sealed class VoucherGroupedQueryStub : IVoucherGroupedQuery
    {
        public IQueryCompounded<IVoucherQueryAtom> VoucherQuery { get; init; }

        public ISubtotal Subtotal { get; init; }
    }

    private sealed class GroupedQueryStub : IGroupedQuery
    {
        public IVoucherDetailQuery VoucherEmitQuery { get; init; }

        public ISubtotal Subtotal { get; init; }
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

    internal override T QueryParse<T>(ref string s, Func<QueryParser, T> func, Client client)
    {
        if (s == null)
            return null;

        try
        {
            return base.QueryParse(ref s, func, client);
        }
        catch (Exception)
        {
            return null;
        }
    }

    internal override T SubtotalParse<T>(ref string s, Func<SubtotalParser, T> func, Client client)
    {
        if (s == null)
            return null;

        try
        {
            return base.SubtotalParse(ref s, func, client);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public override IVoucherGroupedQuery VoucherGroupedQuery(ref string s, Client client)
    {
        try
        {
            return base.VoucherGroupedQuery(ref s, client);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public override IGroupedQuery GroupedQuery(ref string s, Client client)
    {
        try
        {
            return base.GroupedQuery(ref s, client);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
