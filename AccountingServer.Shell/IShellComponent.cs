using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     表达式解释器
    /// </summary>
    internal interface IShellComponent
    {
        /// <summary>
        ///     执行表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        IQueryResult Execute(string expr);

        /// <summary>
        ///     粗略判断表达式是否可执行
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>是否可执行</returns>
        bool IsExecutable(string expr);
    }

    /// <summary>
    ///     复合表达式解释器
    /// </summary>
    internal sealed class ShellComposer : IShellComponent, IEnumerable
    {
        private readonly List<IShellComponent> m_Components = new List<IShellComponent>();

        public void Add(IShellComponent shell) => m_Components.Add(shell);

        /// <inheritdoc />
        public IQueryResult Execute(string expr)
        {
            var comp = m_Components.FirstOrDefault(s => s.IsExecutable(expr));
            if (comp == null)
                throw new InvalidOperationException("表达式无效");

            return comp.Execute(expr);
        }

        /// <inheritdoc />
        public bool IsExecutable(string expr) => m_Components.Any(s => s.IsExecutable(expr));

        public IEnumerator GetEnumerator() => m_Components.GetEnumerator();
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
            var id = str.IndexOfAny(new[] { ' ', '-' });
            return id < 0 ? str : str.Substring(0, id);
        }
    }
}