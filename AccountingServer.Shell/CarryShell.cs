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

            throw new ArgumentException("表达式类型未知", nameof(expr));
        }

        /// <inheritdoc />
        public bool IsExecutable(string expr) => expr.StartsWith("c", StringComparison.Ordinal);
    }
}
