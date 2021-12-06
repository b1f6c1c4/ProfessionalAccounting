﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Shell.Serializer;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     表达式解释组件
    /// </summary>
    internal interface IShellComponent
    {
        /// <summary>
        ///     执行表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <param name="serializer">表示器代号</param>
        /// <returns>执行结果</returns>
        IQueryResult Execute(string expr, IEntitiesSerializer serializer);

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
        private readonly Func<string, IEntitiesSerializer, IQueryResult> m_Action;

        /// <summary>
        ///     首段字符串
        /// </summary>
        private readonly string m_Initial;

        public ShellComponent(string initial, Func<string, IEntitiesSerializer, IQueryResult> action)
        {
            m_Initial = initial;
            m_Action = action;
        }

        /// <inheritdoc />
        public IQueryResult Execute(string expr, IEntitiesSerializer serializer)
            => m_Action(m_Initial == null ? expr : expr.Rest(), serializer);

        /// <inheritdoc />
        public bool IsExecutable(string expr) => m_Initial == null || expr.Initital() == m_Initial;
    }

    /// <summary>
    ///     复合表达式解释组件
    /// </summary>
    internal sealed class ShellComposer : IShellComponent, IEnumerable
    {
        private readonly List<IShellComponent> m_Components = new List<IShellComponent>();

        /// <inheritdoc />
        public IEnumerator GetEnumerator() => m_Components.GetEnumerator();

        /// <inheritdoc />
        public IQueryResult Execute(string expr, IEntitiesSerializer serializer)
            => FirstExecutable(expr).Execute(expr, serializer);

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
        public static string Initital(this string str)
        {
            if (str == null)
                return null;

            var id = str.IndexOfAny(new[] { ' ', '-' });
            return id < 0 ? str : str.Substring(0, id);
        }

        /// <summary>
        ///     首段字符串
        /// </summary>
        /// <param name="str">原字符串</param>
        /// <returns>首段</returns>
        public static string Rest(this string str)
        {
            var id = str.IndexOfAny(new[] { ' ', '-' });
            return id < 0 ? "" : str.Substring(id + 1).TrimStart();
        }
    }
}
