/* Copyright (C) 2020-2021 b1f6c1c4
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
using System.Collections.Generic;
using System.Linq;
using System.Security;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Subtotal;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell;

/// <summary>
///     基本表达式解释器
/// </summary>
internal class AccountingShell : IShellComponent
{
    /// <inheritdoc />
    public IAsyncEnumerable<string> Execute(string expr, Session session) => Parse(expr, session.Client)(session);

    /// <inheritdoc />
    public bool IsExecutable(string expr) => true;

    /// <summary>
    ///     解析表达式
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <param name="client">客户端</param>
    /// <returns>解析结果</returns>
    private Func<Session, IAsyncEnumerable<string>> Parse(string expr, Client client)
    {
        var type = ExprType.None;
        ISubtotalStringify visitor;

        if (ParsingF.Token(ref expr, false, t => t == "unsafe") != null)
            type |= ExprType.Unsafe;

        if (ParsingF.Token(ref expr, false, t => t == "json") != null)
        {
            type |= ExprType.GroupedQueries;
            visitor = new JsonSubtotal();
        }
        else if (ParsingF.Token(ref expr, false, t => t == "Rps") != null)
        {
            type |= ExprType.GroupedQueries;
            visitor = new PreciseSubtotalPre();
        }
        else if (ParsingF.Token(ref expr, false, t => t == "rps") != null)
        {
            type |= ExprType.GroupedQueries;
            visitor = new PreciseSubtotalPre(false);
        }
        else if (ParsingF.Token(ref expr, false, t => t == "raw") != null)
        {
            type |= ExprType.GroupedQueries | ExprType.DetailQuery;
            visitor = new RawSubtotal();
        }
        else if (ParsingF.Token(ref expr, false, t => t == "sraw") != null)
        {
            type |= ExprType.GroupedQueries | ExprType.DetailRQuery;
            visitor = new RawSubtotal(true);
        }
        else if (ParsingF.Token(ref expr, false, t => t == "fancy") != null)
        {
            type |= ExprType.FancyQuery;
            visitor = null;
        }
        else
        {
            type |= ExprType.GroupedQueries | ExprType.VoucherQuery;
            visitor = new RichSubtotalPre();
        }

        if (type.HasFlag(ExprType.GroupedQuery))
            try
            {
                return TryGroupedQuery(expr, visitor, client);
            }
            catch (Exception)
            {
                // ignored
            }

        if (type.HasFlag(ExprType.VoucherGroupedQuery))
            try
            {
                return TryVoucherGroupedQuery(expr, visitor, client);
            }
            catch (Exception)
            {
                // ignored
            }

        return (type & ExprType.NonGroupedQueries) switch
            {
                ExprType.VoucherQuery => TryVoucherQuery(expr, !type.HasFlag(ExprType.Unsafe), client),
                ExprType.DetailQuery => TryDetailQuery(expr, !type.HasFlag(ExprType.Unsafe), client),
                ExprType.DetailRQuery => TryDetailRQuery(expr, !type.HasFlag(ExprType.Unsafe), client),
                ExprType.FancyQuery => TryFancyQuery(expr, !type.HasFlag(ExprType.Unsafe), client),
                _ => throw new InvalidOperationException("表达式无效"),
            };
    }

    /// <summary>
    ///     按记账凭证检索式解析
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <param name="safe">仅限强检索式</param>
    /// <param name="client">客户端</param>
    /// <returns>执行结果</returns>
    private Func<Session, IAsyncEnumerable<string>> TryVoucherQuery(string expr, bool safe, Client client)
    {
        var res = ParsingF.VoucherQuery(ref expr, client);
        ParsingF.Eof(expr);
        if (res.IsDangerous() && safe)
            throw new SecurityException("检测到弱检索式");

        return session => PresentVoucherQuery(res, session);
    }

    /// <summary>
    ///     按分类汇总检索式解析
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <param name="trav">呈现器</param>
    /// <param name="client">客户端</param>
    /// <returns>执行结果</returns>
    private Func<Session, IAsyncEnumerable<string>> TryGroupedQuery(string expr, ISubtotalStringify trav, Client client)
    {
        var res = ParsingF.GroupedQuery(ref expr, client);
        ParsingF.Eof(expr);
        return session => PresentSubtotal(res, trav, session);
    }

    /// <summary>
    ///     按记账凭证分类汇总检索式解析
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <param name="trav">呈现器</param>
    /// <param name="client">客户端</param>
    /// <returns>执行结果</returns>
    private Func<Session, IAsyncEnumerable<string>> TryVoucherGroupedQuery(string expr, ISubtotalStringify trav, Client client)
    {
        var res = ParsingF.VoucherGroupedQuery(ref expr, client);
        ParsingF.Eof(expr);
        return session => PresentSubtotal(res, trav, session);
    }

    /// <summary>
    ///     执行记账凭证检索式并呈现记账凭证
    /// </summary>
    /// <param name="query">记账凭证检索式</param>
    /// <param name="session">客户端会话</param>
    /// <returns>记账凭证表达式</returns>
    private IAsyncEnumerable<string> PresentVoucherQuery(IQueryCompounded<IVoucherQueryAtom> query, Session session)
        => session.Serializer.PresentVouchers(session.Accountant.SelectVouchersAsync(query));

    /// <summary>
    ///     按细目检索式解析
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <param name="safe">仅限强检索式</param>
    /// <param name="client">客户端</param>
    /// <returns>执行结果</returns>
    private Func<Session, IAsyncEnumerable<string>> TryDetailQuery(string expr, bool safe, Client client)
    {
        var res = ParsingF.DetailQuery(ref expr, client);
        ParsingF.Eof(expr);
        if (res.IsDangerous() && safe)
            throw new SecurityException("检测到弱检索式");

        return session => PresentDetailQuery(res, session);
    }

    /// <summary>
    ///     执行细目检索式并呈现结果
    /// </summary>
    /// <param name="query">细目检索式</param>
    /// <param name="session">客户端会话</param>
    /// <returns>执行结果</returns>
    private IAsyncEnumerable<string> PresentDetailQuery(IVoucherDetailQuery query, Session session)
        => session.Serializer.PresentVoucherDetails(session.Accountant.SelectVoucherDetailsAsync(query));

    /// <summary>
    ///     按带记账凭证的细目检索式解析
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <param name="safe">仅限强检索式</param>
    /// <param name="client">客户端</param>
    /// <returns>执行结果</returns>
    private Func<Session, IAsyncEnumerable<string>> TryDetailRQuery(string expr, bool safe, Client client)
    {
        var res = ParsingF.DetailQuery(ref expr, client);
        ParsingF.Eof(expr);
        if (res.IsDangerous() && safe)
            throw new SecurityException("检测到弱检索式");

        return session => PresentDetailRQuery(res, session);
    }

    /// <summary>
    ///     执行带记账凭证的细目检索式并呈现结果
    /// </summary>
    /// <param name="query">细目检索式</param>
    /// <param name="session">客户端会话</param>
    /// <returns>执行结果</returns>
    private IAsyncEnumerable<string> PresentDetailRQuery(IVoucherDetailQuery query, Session session)
        => session.Serializer.PresentVoucherDetails(
            session.Accountant.SelectVouchersAsync(query.VoucherQuery).SelectMany(
                v => v.Details.Where(d => d.IsMatch(query.ActualDetailFilter()))
                    .Select(d => new VoucherDetailR(v, d)).ToAsyncEnumerable()));

    /// <summary>
    ///     按记账凭证检索式解析，但仅保留部分细目
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <param name="safe">仅限强检索式</param>
    /// <param name="client">客户端</param>
    /// <returns>执行结果</returns>
    private Func<Session, IAsyncEnumerable<string>> TryFancyQuery(string expr, bool safe, Client client)
    {
        var res = ParsingF.DetailQuery(ref expr, client);
        ParsingF.Eof(expr);
        if (res.IsDangerous() && safe)
            throw new SecurityException("检测到弱检索式");

        return session => PresentFancyQuery(res, session);
    }

    /// <summary>
    ///     执行记账凭证检索式，保留部分细目并呈现结果
    /// </summary>
    /// <param name="query">带记账凭证的细目检索式</param>
    /// <param name="session">客户端会话</param>
    /// <returns>执行结果</returns>
    private IAsyncEnumerable<string> PresentFancyQuery(IVoucherDetailQuery query, Session session)
        => session.Serializer.PresentVouchers(
            session.Accountant.SelectVouchersAsync(query.VoucherQuery).Select(v =>
                {
                    v.Details.RemoveAll(d => !d.IsMatch(query.ActualDetailFilter()));
                    return v;
                }));

    /// <summary>
    ///     执行记账凭证分类汇总检索式并呈现结果
    /// </summary>
    /// <param name="query">记账凭证分类汇总检索式</param>
    /// <param name="trav">呈现器</param>
    /// <param name="session">客户端会话</param>
    /// <returns>执行结果</returns>
    private async IAsyncEnumerable<string> PresentSubtotal(IVoucherGroupedQuery query, ISubtotalStringify trav, Session session)
    {
        var result = await session.Accountant.SelectVouchersGroupedAsync(query);
        yield return trav.PresentSubtotal(result, query.Subtotal, session.Serializer);
    }

    /// <summary>
    ///     执行分类汇总检索式并呈现结果
    /// </summary>
    /// <param name="query">分类汇总检索式</param>
    /// <param name="trav">呈现器</param>
    /// <param name="session">客户端会话</param>
    /// <returns>执行结果</returns>
    private async IAsyncEnumerable<string> PresentSubtotal(IGroupedQuery query, ISubtotalStringify trav, Session session)
    {
        var result = await session.Accountant.SelectVoucherDetailsGroupedAsync(query);
        yield return trav.PresentSubtotal(result, query.Subtotal, session.Serializer);
    }

    [Flags]
    private enum ExprType
    {
        None = 0b00000000,
        GroupedQueries = 0b00000011,
        GroupedQuery = 0b00000001,
        VoucherGroupedQuery = 0b00000010,
        NonGroupedQueries = 0b00111100,
        DetailQuery = 0b00000100,
        DetailRQuery = 0b00001000,
        VoucherQuery = 0b00010000,
        FancyQuery = 0b00100000,
        Unsafe = 0b10000000,
    }
}
