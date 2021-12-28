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
using System.Linq;
using System.Security;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Subtotal;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell;

/// <summary>
///     基本表达式解释器
/// </summary>
internal class AccountingShell : IShellComponent
{
    /// <summary>
    ///     基本会计业务处理类
    /// </summary>
    private readonly Accountant m_Accountant;

    public AccountingShell(Accountant helper) => m_Accountant = helper;

    /// <inheritdoc />
    public IQueryResult Execute(string expr, Accountant accountant, IEntitiesSerializer serializer) => Parse(expr)(serializer);

    /// <inheritdoc />
    public bool IsExecutable(string expr) => true;

    /// <summary>
    ///     解析表达式
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <returns>解析结果</returns>
    private Func<IEntitiesSerializer, IQueryResult> Parse(string expr)
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
                return TryGroupedQuery(expr, visitor);
            }
            catch (Exception)
            {
                // ignored
            }

        if (type.HasFlag(ExprType.VoucherGroupedQuery))
            try
            {
                return TryVoucherGroupedQuery(expr, visitor);
            }
            catch (Exception)
            {
                // ignored
            }

        return (type & ExprType.NonGroupedQueries) switch
            {
                ExprType.VoucherQuery => TryVoucherQuery(expr, !type.HasFlag(ExprType.Unsafe)),
                ExprType.DetailQuery => TryDetailQuery(expr, !type.HasFlag(ExprType.Unsafe)),
                ExprType.DetailRQuery => TryDetailRQuery(expr, !type.HasFlag(ExprType.Unsafe)),
                ExprType.FancyQuery => TryFancyQuery(expr, !type.HasFlag(ExprType.Unsafe)),
                _ => throw new InvalidOperationException("表达式无效"),
            };
    }

    /// <summary>
    ///     按记账凭证检索式解析
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <param name="safe">仅限强检索式</param>
    /// <returns>执行结果</returns>
    private Func<IEntitiesSerializer, IQueryResult> TryVoucherQuery(string expr, bool safe)
    {
        var res = ParsingF.VoucherQuery(ref expr, m_Accountant.Client);
        ParsingF.Eof(expr);
        if (res.IsDangerous() && safe)
            throw new SecurityException("检测到弱检索式");

        return serializer => PresentVoucherQuery(res, serializer);
    }

    /// <summary>
    ///     按分类汇总检索式解析
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <param name="trav">呈现器</param>
    /// <returns>执行结果</returns>
    private Func<IEntitiesSerializer, IQueryResult> TryGroupedQuery(string expr, ISubtotalStringify trav)
    {
        var res = ParsingF.GroupedQuery(ref expr, m_Accountant.Client);
        ParsingF.Eof(expr);
        return serializer => PresentSubtotal(res, trav, serializer);
    }

    /// <summary>
    ///     按记账凭证分类汇总检索式解析
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <param name="trav">呈现器</param>
    /// <returns>执行结果</returns>
    private Func<IEntitiesSerializer, IQueryResult> TryVoucherGroupedQuery(string expr, ISubtotalStringify trav)
    {
        var res = ParsingF.VoucherGroupedQuery(ref expr, m_Accountant.Client);
        ParsingF.Eof(expr);
        return serializer => PresentSubtotal(res, trav, serializer);
    }

    /// <summary>
    ///     执行记账凭证检索式并呈现记账凭证
    /// </summary>
    /// <param name="query">记账凭证检索式</param>
    /// <param name="serializer">表示器</param>
    /// <returns>记账凭证表达式</returns>
    private IQueryResult PresentVoucherQuery(IQueryCompounded<IVoucherQueryAtom> query,
        IEntitiesSerializer serializer)
        => new PlainText(serializer.PresentVouchers(m_Accountant.SelectVouchers(query)));

    /// <summary>
    ///     按细目检索式解析
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <param name="safe">仅限强检索式</param>
    /// <returns>执行结果</returns>
    private Func<IEntitiesSerializer, IQueryResult> TryDetailQuery(string expr, bool safe)
    {
        var res = ParsingF.DetailQuery(ref expr, m_Accountant.Client);
        ParsingF.Eof(expr);
        if (res.IsDangerous() && safe)
            throw new SecurityException("检测到弱检索式");

        return serializer => PresentDetailQuery(res, serializer);
    }

    /// <summary>
    ///     执行细目检索式并呈现结果
    /// </summary>
    /// <param name="query">细目检索式</param>
    /// <param name="serializer">表示器</param>
    /// <returns>执行结果</returns>
    private IQueryResult PresentDetailQuery(IVoucherDetailQuery query, IEntitiesSerializer serializer)
        => new PlainText(serializer.PresentVoucherDetails(m_Accountant.SelectVoucherDetails(query)));

    /// <summary>
    ///     按带记账凭证的细目检索式解析
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <param name="safe">仅限强检索式</param>
    /// <returns>执行结果</returns>
    private Func<IEntitiesSerializer, IQueryResult> TryDetailRQuery(string expr, bool safe)
    {
        var res = ParsingF.DetailQuery(ref expr, m_Accountant.Client);
        ParsingF.Eof(expr);
        if (res.IsDangerous() && safe)
            throw new SecurityException("检测到弱检索式");

        return serializer => PresentDetailRQuery(res, serializer);
    }

    /// <summary>
    ///     执行带记账凭证的细目检索式并呈现结果
    /// </summary>
    /// <param name="query">细目检索式</param>
    /// <param name="serializer">表示器</param>
    /// <returns>执行结果</returns>
    private IQueryResult PresentDetailRQuery(IVoucherDetailQuery query, IEntitiesSerializer serializer)
        => new PlainText(
            serializer.PresentVoucherDetails(
                m_Accountant.SelectVouchers(query.VoucherQuery).SelectMany(
                    v => v.Details.Where(d => d.IsMatch(query.ActualDetailFilter()))
                        .Select(d => new VoucherDetailR(v, d)))));

    /// <summary>
    ///     按记账凭证检索式解析，但仅保留部分细目
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <param name="safe">仅限强检索式</param>
    /// <returns>执行结果</returns>
    private Func<IEntitiesSerializer, IQueryResult> TryFancyQuery(string expr, bool safe)
    {
        var res = ParsingF.DetailQuery(ref expr, m_Accountant.Client);
        ParsingF.Eof(expr);
        if (res.IsDangerous() && safe)
            throw new SecurityException("检测到弱检索式");

        return serializer => PresentFancyQuery(res, serializer);
    }

    /// <summary>
    ///     执行记账凭证检索式，保留部分细目并呈现结果
    /// </summary>
    /// <param name="query">带记账凭证的细目检索式</param>
    /// <param name="serializer">表示器</param>
    /// <returns>执行结果</returns>
    private IQueryResult PresentFancyQuery(IVoucherDetailQuery query, IEntitiesSerializer serializer)
        => new PlainText(
            serializer.PresentVouchers(
                m_Accountant.SelectVouchers(query.VoucherQuery).Select(v =>
                    {
                        v.Details.RemoveAll(d => !d.IsMatch(query.ActualDetailFilter()));
                        return v;
                    })));

    /// <summary>
    ///     执行记账凭证分类汇总检索式并呈现结果
    /// </summary>
    /// <param name="query">记账凭证分类汇总检索式</param>
    /// <param name="trav">呈现器</param>
    /// <param name="serializer">表示器</param>
    /// <returns>执行结果</returns>
    private IQueryResult PresentSubtotal(IVoucherGroupedQuery query, ISubtotalStringify trav,
        IEntitiesSerializer serializer)
    {
        var result = m_Accountant.SelectVouchersGrouped(query);
        return new PlainText(trav.PresentSubtotal(result, query.Subtotal, serializer));
    }

    /// <summary>
    ///     执行分类汇总检索式并呈现结果
    /// </summary>
    /// <param name="query">分类汇总检索式</param>
    /// <param name="trav">呈现器</param>
    /// <param name="serializer">表示器</param>
    /// <returns>执行结果</returns>
    private IQueryResult PresentSubtotal(IGroupedQuery query, ISubtotalStringify trav,
        IEntitiesSerializer serializer)
    {
        var result = m_Accountant.SelectVoucherDetailsGrouped(query);
        return new PlainText(trav.PresentSubtotal(result, query.Subtotal, serializer));
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
