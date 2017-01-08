using System;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     结转表达式解释器
    /// </summary>
    internal class CarryShell : IShellComponent
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public CarryShell(Accountant helper) { m_Accountant = helper; }

        /// <inheritdoc />
        public IQueryResult Execute(string expr)
        {
            throw new NotImplementedException();

            throw new InvalidOperationException("表达式无效");
        }

        /// <inheritdoc />
        public bool IsExecutable(string expr) => expr.Initital() == "ca";
    }

    /// <summary>
    ///     年度结转表达式解释器
    /// </summary>
    internal class CarryYearShell : IShellComponent
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public CarryYearShell(Accountant helper) { m_Accountant = helper; }

        /// <inheritdoc />
        public IQueryResult Execute(string expr)
        {
            throw new NotImplementedException();

            throw new InvalidOperationException("表达式无效");
        }

        /// <inheritdoc />
        public bool IsExecutable(string expr) => expr.Initital() == "caa";
    }
}
