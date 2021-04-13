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
using System.Security;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     分期表达式解释器
    /// </summary>
    internal abstract class DistributedShell : IShellComponent
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        protected readonly Accountant Accountant;

        /// <summary>
        ///     复合表达式解释器
        /// </summary>
        private readonly IShellComponent m_Composer;

        protected DistributedShell(Accountant helper)
        {
            Accountant = helper;
            var resetComposer =
                new ShellComposer
                    {
                        new ShellComponent(
                            "soft",
                            (expr, serializer) =>
                                {
                                    var dist = Parsing.DistributedQuery(ref expr);
                                    var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
                                    Parsing.Eof(expr);
                                    return ExecuteResetSoft(dist, rng);
                                }),
                        new ShellComponent(
                            "mixed",
                            (expr, serializer) =>
                                {
                                    var dist = Parsing.DistributedQuery(ref expr);
                                    var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
                                    Parsing.Eof(expr);
                                    return ExecuteResetMixed(dist, rng);
                                }),
                        new ShellComponent(
                            "hard",
                            (expr, serializer) =>
                                {
                                    var dist = Parsing.DistributedQuery(ref expr);
                                    var vouchers = Parsing.OptColVouchers(ref expr);
                                    Parsing.Eof(expr);
                                    return ExecuteResetHard(dist, vouchers);
                                }),
                    };
            m_Composer =
                new ShellComposer
                    {
                        new ShellComponent(
                            "all",
                            (expr, serializer) =>
                                {
                                    var safe = Parsing.Token(ref expr, false, t => t == "unsafe") == null;
                                    var dist = Parsing.DistributedQuery(ref expr);
                                    Parsing.Eof(expr);
                                    if (dist.IsDangerous() && safe)
                                        throw new SecurityException("检测到弱检索式");

                                    return ExecuteList(dist, null, false, serializer);
                                }),
                        new ShellComponent(
                            "li",
                            (expr, serializer) =>
                                {
                                    var safe = Parsing.Token(ref expr, false, t => t == "unsafe") == null;
                                    var dt = Parsing.UniqueTime(ref expr) ?? ClientDateTime.Today;
                                    var dist = Parsing.DistributedQuery(ref expr);
                                    Parsing.Eof(expr);
                                    if (dist.IsDangerous() && safe)
                                        throw new SecurityException("检测到弱检索式");

                                    return ExecuteList(dist, dt, true, serializer);
                                }),
                        new ShellComponent(
                            "q",
                            (expr, serializer) =>
                                {
                                    var safe = Parsing.Token(ref expr, false, t => t == "unsafe") == null;
                                    var dist = Parsing.DistributedQuery(ref expr);
                                    Parsing.Eof(expr);
                                    if (dist.IsDangerous() && safe)
                                        throw new SecurityException("检测到弱检索式");

                                    return ExecuteQuery(dist, serializer);
                                }),
                        new ShellComponent(
                            "reg",
                            (expr, serializer) =>
                                {
                                    var dist = Parsing.DistributedQuery(ref expr);
                                    var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
                                    var vouchers = Parsing.OptColVouchers(ref expr);
                                    Parsing.Eof(expr);
                                    return ExecuteRegister(dist, rng, vouchers, serializer);
                                }),
                        new ShellComponent(
                            "unreg",
                            (expr, serializer) =>
                                {
                                    var dist = Parsing.DistributedQuery(ref expr);
                                    var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
                                    var vouchers = Parsing.OptColVouchers(ref expr);
                                    Parsing.Eof(expr);
                                    return ExecuteUnregister(dist, rng, vouchers, serializer);
                                }),
                        new ShellComponent(
                            "recal",
                            (expr, serializer) =>
                                {
                                    var dist = Parsing.DistributedQuery(ref expr);
                                    Parsing.Eof(expr);
                                    return ExecuteRecal(dist, serializer);
                                }),
                        new ShellComponent("rst", (expr, serializer) => resetComposer.Execute(expr, serializer)),
                        new ShellComponent(
                            "ap",
                            (expr, serializer) =>
                                {
                                    var collapse = Parsing.Optional(ref expr, "col");
                                    var dist = Parsing.DistributedQuery(ref expr);
                                    var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
                                    Parsing.Eof(expr);
                                    return ExecuteApply(dist, rng, collapse);
                                }),
                        new ShellComponent(
                            "chk",
                            (expr, serializer) =>
                                {
                                    var dist = Parsing.DistributedQuery(ref expr);
                                    Parsing.Eof(expr);
                                    return ExecuteCheck(dist, new(null, ClientDateTime.Today), serializer);
                                }),
                        new ShellComponent(
                            null,
                            (expr, serializer) =>
                                {
                                    var dt = Parsing.UniqueTime(ref expr) ?? ClientDateTime.Today;
                                    var dist = Parsing.DistributedQuery(ref expr);
                                    Parsing.Eof(expr);
                                    return ExecuteList(dist, dt, false, serializer);
                                }),
                    };
        }

        /// <summary>
        ///     首字母
        /// </summary>
        protected abstract string Initial { get; }

        /// <inheritdoc />
        public IQueryResult Execute(string expr, IEntitiesSerializer serializer)
            => m_Composer.Execute(expr.Rest(), serializer);

        /// <inheritdoc />
        public bool IsExecutable(string expr) => expr.Initial() == Initial;

        /// <summary>
        ///     执行列表表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="dt">计算账面价值的时间</param>
        /// <param name="showSchedule">是否显示折旧计算表</param>
        /// <param name="serializer">表示器</param>
        /// <returns>执行结果</returns>
        protected abstract IQueryResult ExecuteList(IQueryCompounded<IDistributedQueryAtom> distQuery, DateTime? dt,
            bool showSchedule, IEntitiesSerializer serializer);

        /// <summary>
        ///     执行查询表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="serializer">表示器</param>
        /// <returns>执行结果</returns>
        protected abstract IQueryResult ExecuteQuery(IQueryCompounded<IDistributedQueryAtom> distQuery,
            IEntitiesSerializer serializer);

        /// <summary>
        ///     执行注册表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="query">记账凭证检索式</param>
        /// <param name="serializer">表示器</param>
        /// <returns>执行结果</returns>
        protected abstract IQueryResult ExecuteRegister(IQueryCompounded<IDistributedQueryAtom> distQuery,
            DateFilter rng,
            IQueryCompounded<IVoucherQueryAtom> query, IEntitiesSerializer serializer);

        /// <summary>
        ///     执行解除注册表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="query">记账凭证检索式</param>
        /// <param name="serializer">表示器</param>
        /// <returns>执行结果</returns>
        protected abstract IQueryResult ExecuteUnregister(IQueryCompounded<IDistributedQueryAtom> distQuery,
            DateFilter rng, IQueryCompounded<IVoucherQueryAtom> query, IEntitiesSerializer serializer);

        /// <summary>
        ///     执行重新计算表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="serializer">表示器</param>
        /// <returns>执行结果</returns>
        protected abstract IQueryResult ExecuteRecal(IQueryCompounded<IDistributedQueryAtom> distQuery,
            IEntitiesSerializer serializer);

        /// <summary>
        ///     执行软重置表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>执行结果</returns>
        protected abstract IQueryResult ExecuteResetSoft(IQueryCompounded<IDistributedQueryAtom> distQuery,
            DateFilter rng);

        /// <summary>
        ///     执行混合重置表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>执行结果</returns>
        protected abstract IQueryResult ExecuteResetMixed(IQueryCompounded<IDistributedQueryAtom> distQuery,
            DateFilter rng);

        /// <summary>
        ///     执行硬重置表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="query">记账凭证检索式</param>
        /// <returns>执行结果</returns>
        protected abstract IQueryResult ExecuteResetHard(IQueryCompounded<IDistributedQueryAtom> distQuery,
            IQueryCompounded<IVoucherQueryAtom> query);

        /// <summary>
        ///     执行应用表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="isCollapsed">是否压缩</param>
        /// <returns>执行结果</returns>
        protected abstract IQueryResult ExecuteApply(IQueryCompounded<IDistributedQueryAtom> distQuery, DateFilter rng,
            bool isCollapsed);

        /// <summary>
        ///     执行检查表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="serializer">表示器</param>
        /// <returns>执行结果</returns>
        protected abstract IQueryResult ExecuteCheck(IQueryCompounded<IDistributedQueryAtom> distQuery, DateFilter rng,
            IEntitiesSerializer serializer);
    }
}
