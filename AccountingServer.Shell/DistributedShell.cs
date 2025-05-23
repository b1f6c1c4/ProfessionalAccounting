/* Copyright (C) 2020-2025 b1f6c1c4
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
using System.Security;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell;

/// <summary>
///     分期表达式解释器
/// </summary>
internal abstract class DistributedShell : IShellComponent
{
    /// <summary>
    ///     复合表达式解释器
    /// </summary>
    private readonly IShellComponent m_Composer;

    protected DistributedShell()
    {
        var resetComposer =
            new ShellComposer
                {
                    new ShellComponent(
                        "soft",
                        (expr, ctx) =>
                            {
                                var dist = Parsing.DistributedQuery(ref expr, ctx.Client);
                                var rng = Parsing.Range(ref expr, ctx.Client) ?? DateFilter.Unconstrained;
                                Parsing.Eof(expr);
                                return ExecuteResetSoft(dist, rng, ctx);
                            }),
                    new ShellComponent(
                        "mixed",
                        (expr, ctx) =>
                            {
                                var dist = Parsing.DistributedQuery(ref expr, ctx.Client);
                                var rng = Parsing.Range(ref expr, ctx.Client) ?? DateFilter.Unconstrained;
                                Parsing.Eof(expr);
                                return ExecuteResetMixed(dist, rng, ctx);
                            }),
                    new ShellComponent(
                        "hard",
                        (expr, ctx) =>
                            {
                                var dist = Parsing.DistributedQuery(ref expr, ctx.Client);
                                var vouchers = Parsing.OptColVouchers(ref expr, ctx.Client);
                                Parsing.Eof(expr);
                                return ExecuteResetHard(dist, vouchers, ctx);
                            }),
                };
        m_Composer =
            new ShellComposer
                {
                    new ShellComponent(
                        "all",
                        (expr, ctx) =>
                            {
                                var safe = Parsing.Token(ref expr, false, static t => t == "unsafe") == null;
                                var dist = Parsing.DistributedQuery(ref expr, ctx.Client);
                                Parsing.Eof(expr);
                                if (dist.IsDangerous() && safe)
                                    throw new SecurityException("检测到弱检索式");

                                return ExecuteList(dist, null, false, ctx);
                            }),
                    new ShellComponent(
                        "li",
                        (expr, ctx) =>
                            {
                                var safe = Parsing.Token(ref expr, false, static t => t == "unsafe") == null;
                                var dt = Parsing.UniqueTime(ref expr, ctx.Client) ?? ctx.Client.Today;
                                var dist = Parsing.DistributedQuery(ref expr, ctx.Client);
                                Parsing.Eof(expr);
                                if (dist.IsDangerous() && safe)
                                    throw new SecurityException("检测到弱检索式");

                                return ExecuteList(dist, dt, true, ctx);
                            }),
                    new ShellComponent(
                        "q",
                        (expr, ctx) =>
                            {
                                var safe = Parsing.Token(ref expr, false, static t => t == "unsafe") == null;
                                var dist = Parsing.DistributedQuery(ref expr, ctx.Client);
                                Parsing.Eof(expr);
                                if (dist.IsDangerous() && safe)
                                    throw new SecurityException("检测到弱检索式");

                                return ExecuteQuery(dist, ctx);
                            }),
                    new ShellComponent(
                        "reg",
                        (expr, ctx) =>
                            {
                                var dist = Parsing.DistributedQuery(ref expr, ctx.Client);
                                var rng = Parsing.Range(ref expr, ctx.Client) ?? DateFilter.Unconstrained;
                                var vouchers = Parsing.OptColVouchers(ref expr, ctx.Client);
                                Parsing.Eof(expr);
                                return ExecuteRegister(dist, rng, vouchers, ctx);
                            }),
                    new ShellComponent(
                        "unreg",
                        (expr, ctx) =>
                            {
                                var dist = Parsing.DistributedQuery(ref expr, ctx.Client);
                                var rng = Parsing.Range(ref expr, ctx.Client) ?? DateFilter.Unconstrained;
                                var vouchers = Parsing.OptColVouchers(ref expr, ctx.Client);
                                Parsing.Eof(expr);
                                return ExecuteUnregister(dist, rng, vouchers, ctx);
                            }),
                    new ShellComponent(
                        "recal",
                        (expr, ctx) =>
                            {
                                var dist = Parsing.DistributedQuery(ref expr, ctx.Client);
                                Parsing.Eof(expr);
                                return ExecuteRecal(dist, ctx);
                            }),
                    new ComponentAdapter("rst", resetComposer),
                    new ShellComponent(
                        "ap",
                        (expr, ctx) =>
                            {
                                var collapse = Parsing.Optional(ref expr, "col");
                                var dist = Parsing.DistributedQuery(ref expr, ctx.Client);
                                var rng = Parsing.Range(ref expr, ctx.Client) ?? DateFilter.Unconstrained;
                                Parsing.Eof(expr);
                                return ExecuteApply(dist, rng, collapse, ctx);
                            }),
                    new ShellComponent(
                        "chk",
                        (expr, ctx) =>
                            {
                                var dist = Parsing.DistributedQuery(ref expr, ctx.Client);
                                Parsing.Eof(expr);
                                return ExecuteCheck(dist, new(null, ctx.Client.Today), ctx);
                            }),
                    new ShellComponent(
                        null,
                        (expr, ctx) =>
                            {
                                var dt = Parsing.UniqueTime(ref expr, ctx.Client) ?? ctx.Client.Today;
                                var dist = Parsing.DistributedQuery(ref expr, ctx.Client);
                                Parsing.Eof(expr);
                                return ExecuteList(dist, dt, false, ctx);
                            }),
                };
    }

    /// <summary>
    ///     首字母
    /// </summary>
    protected abstract string Initial { get; }

    /// <inheritdoc />
    public IAsyncEnumerable<string> Execute(string expr, Context ctx, string term)
    {
        var next = string.IsNullOrEmpty(term) ? Initial : $"{term}-{Initial}";
        ctx.Identity.WillInvoke(next);
        return m_Composer.Execute(expr.Rest(), ctx, next);
    }

    /// <inheritdoc />
    public bool IsExecutable(string expr) => expr.Initial() == Initial;

    /// <summary>
    ///     执行列表表达式
    /// </summary>
    /// <param name="distQuery">分期检索式</param>
    /// <param name="dt">计算账面价值的时间</param>
    /// <param name="showSchedule">是否显示折旧计算表</param>
    /// <param name="ctx">客户端上下文</param>
    /// <returns>执行结果</returns>
    protected abstract IAsyncEnumerable<string> ExecuteList(IQueryCompounded<IDistributedQueryAtom> distQuery,
        DateTime? dt, bool showSchedule, Context ctx);

    /// <summary>
    ///     执行查询表达式
    /// </summary>
    /// <param name="distQuery">分期检索式</param>
    /// <param name="ctx">客户端上下文</param>
    /// <returns>执行结果</returns>
    protected abstract IAsyncEnumerable<string> ExecuteQuery(IQueryCompounded<IDistributedQueryAtom> distQuery,
        Context ctx);

    /// <summary>
    ///     执行注册表达式
    /// </summary>
    /// <param name="distQuery">分期检索式</param>
    /// <param name="rng">日期过滤器</param>
    /// <param name="query">记账凭证检索式</param>
    /// <param name="ctx">客户端上下文</param>
    /// <returns>执行结果</returns>
    protected abstract IAsyncEnumerable<string> ExecuteRegister(IQueryCompounded<IDistributedQueryAtom> distQuery,
        DateFilter rng, IQueryCompounded<IVoucherQueryAtom> query, Context ctx);

    /// <summary>
    ///     执行解除注册表达式
    /// </summary>
    /// <param name="distQuery">分期检索式</param>
    /// <param name="rng">日期过滤器</param>
    /// <param name="query">记账凭证检索式</param>
    /// <param name="ctx">客户端上下文</param>
    /// <returns>执行结果</returns>
    protected abstract IAsyncEnumerable<string> ExecuteUnregister(IQueryCompounded<IDistributedQueryAtom> distQuery,
        DateFilter rng, IQueryCompounded<IVoucherQueryAtom> query, Context ctx);

    /// <summary>
    ///     执行重新计算表达式
    /// </summary>
    /// <param name="distQuery">分期检索式</param>
    /// <param name="ctx">客户端上下文</param>
    /// <returns>执行结果</returns>
    protected abstract IAsyncEnumerable<string> ExecuteRecal(IQueryCompounded<IDistributedQueryAtom> distQuery,
        Context ctx);

    /// <summary>
    ///     执行软重置表达式
    /// </summary>
    /// <param name="distQuery">分期检索式</param>
    /// <param name="rng">日期过滤器</param>
    /// <param name="ctx">客户端上下文</param>
    /// <returns>执行结果</returns>
    protected abstract IAsyncEnumerable<string> ExecuteResetSoft(IQueryCompounded<IDistributedQueryAtom> distQuery,
        DateFilter rng, Context ctx);

    /// <summary>
    ///     执行混合重置表达式
    /// </summary>
    /// <param name="distQuery">分期检索式</param>
    /// <param name="rng">日期过滤器</param>
    /// <param name="ctx">客户端上下文</param>
    /// <returns>执行结果</returns>
    protected abstract IAsyncEnumerable<string> ExecuteResetMixed(IQueryCompounded<IDistributedQueryAtom> distQuery,
        DateFilter rng, Context ctx);

    /// <summary>
    ///     执行硬重置表达式
    /// </summary>
    /// <param name="distQuery">分期检索式</param>
    /// <param name="query">记账凭证检索式</param>
    /// <param name="ctx">客户端上下文</param>
    /// <returns>执行结果</returns>
    protected abstract IAsyncEnumerable<string> ExecuteResetHard(IQueryCompounded<IDistributedQueryAtom> distQuery,
        IQueryCompounded<IVoucherQueryAtom> query, Context ctx);

    /// <summary>
    ///     执行应用表达式
    /// </summary>
    /// <param name="distQuery">分期检索式</param>
    /// <param name="rng">日期过滤器</param>
    /// <param name="isCollapsed">是否压缩</param>
    /// <param name="ctx">客户端上下文</param>
    /// <returns>执行结果</returns>
    protected abstract IAsyncEnumerable<string> ExecuteApply(IQueryCompounded<IDistributedQueryAtom> distQuery,
        DateFilter rng, bool isCollapsed, Context ctx);

    /// <summary>
    ///     执行检查表达式
    /// </summary>
    /// <param name="distQuery">分期检索式</param>
    /// <param name="rng">日期过滤器</param>
    /// <param name="ctx">客户端上下文</param>
    /// <returns>执行结果</returns>
    protected abstract IAsyncEnumerable<string> ExecuteCheck(IQueryCompounded<IDistributedQueryAtom> distQuery,
        DateFilter rng, Context ctx);
}
