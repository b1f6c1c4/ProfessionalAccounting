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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AccountingServer.Shell;

/// <summary>
///     表达式解释组件
/// </summary>
internal interface IShellComponent
{
    /// <summary>
    ///     执行表达式
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <param name="ctx">客户端上下文</param>
    /// <param name="term">表达式头</param>
    /// <returns>执行结果</returns>
    IAsyncEnumerable<string> Execute(string expr, Context ctx, string term);

    /// <summary>
    ///     粗略判断表达式是否可执行
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <returns>是否可执行</returns>
    bool IsExecutable(string expr);
}

/// <summary>
///     从委托创建表达式解释组件
/// </summary>
internal class ShellComponent : IShellComponent
{
    /// <summary>
    ///     操作
    /// </summary>
    private readonly Func<string, Context, IAsyncEnumerable<string>> m_Action;

    /// <summary>
    ///     首段字符串
    /// </summary>
    private readonly string m_Initial;

    public ShellComponent(string initial, Func<string, Context, IAsyncEnumerable<string>> action)
    {
        m_Initial = initial;
        m_Action = action;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<string> Execute(string expr, Context ctx, string term)
    {
        if (m_Initial == null)
            return m_Action(expr, ctx);

        ctx.Identity.WillInvoke(string.IsNullOrEmpty(term) ? m_Initial : $"{term}-{m_Initial}");
        return m_Action(expr.Rest(), ctx);
    }

    /// <inheritdoc />
    public bool IsExecutable(string expr) => m_Initial == null || expr.Initial() == m_Initial;
}

internal class ComponentAdapter : IShellComponent
{
    /// <summary>
    ///     首段字符串
    /// </summary>
    private readonly string m_Initial;

    private readonly IShellComponent m_Component;

    public ComponentAdapter(string initial, IShellComponent component)
    {
        m_Initial = initial;
        m_Component = component;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<string> Execute(string expr, Context ctx, string term)
    {
        if (m_Initial == null)
            return m_Component.Execute(expr, ctx, term);

        var next = string.IsNullOrEmpty(term) ? m_Initial : $"{term}-{m_Initial}";
        ctx.Identity.WillInvoke(next);
        return m_Component.Execute(expr.Rest(), ctx, next);
    }

    /// <inheritdoc />
    public bool IsExecutable(string expr) => m_Initial == null || expr.Initial() == m_Initial;
}

/// <summary>
///     复合表达式解释组件
/// </summary>
internal sealed class ShellComposer : IShellComponent, IEnumerable
{
    private readonly List<IShellComponent> m_Components = new();

    /// <inheritdoc />
    public IEnumerator GetEnumerator() => m_Components.GetEnumerator();

    /// <inheritdoc />
    public IAsyncEnumerable<string> Execute(string expr, Context ctx, string term)
        => FirstExecutable(expr).Execute(expr, ctx, term);

    /// <inheritdoc />
    public bool IsExecutable(string expr) => m_Components.Any(s => s.IsExecutable(expr));

    public void Add(IShellComponent shell) => m_Components.Add(shell);

    /// <summary>
    ///     第一个可以执行的组件
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <returns>组件</returns>
    private IShellComponent FirstExecutable(string expr) =>
        m_Components.FirstOrDefault(s => s.IsExecutable(expr)) ?? throw new InvalidOperationException("表达式无效");
}

internal static class ExprHelper
{
    /// <summary>
    ///     首段字符串
    /// </summary>
    /// <param name="str">原字符串</param>
    /// <returns>首段</returns>
    public static string Initial(this string str)
    {
        if (str == null)
            return null;

        var id = str.IndexOfAny(new[] { ' ', '-' });
        return id < 0 ? str : str[..id];
    }

    /// <summary>
    ///     首段字符串
    /// </summary>
    /// <param name="str">原字符串</param>
    /// <returns>首段</returns>
    public static string Rest(this string str)
    {
        var id = str.IndexOfAny(new[] { ' ', '-' });
        return id < 0 ? "" : str[(id + 1)..].TrimStart();
    }
}
