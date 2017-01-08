using System;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     结转表达式解释器
    /// </summary>
    internal class CarryShell
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public CarryShell(Accountant helper) { m_Accountant = helper; }

        /// <summary>
        ///     执行结转表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        public IQueryResult ExecuteCarry(string expr)
        {
            throw new NotImplementedException();

            throw new ArgumentException("表达式类型未知", nameof(expr));
        }
    }
}
