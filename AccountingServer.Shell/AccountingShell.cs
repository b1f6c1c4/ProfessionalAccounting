using System;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     基本表达式解释器
    /// </summary>
    internal class AccountingShell : IShellComponent
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public AccountingShell(Accountant helper) { m_Accountant = helper; }

        /// <inheritdoc />
        public IQueryResult Execute(string expr)
        {
            {
                var res = Parsing.GroupedQuery(expr);
                if (res != null)
                    return PresentSubtotal(res);
            }

            {
                var l = expr.Length;
                var res = Parsing.VoucherQuery(ref expr);
                if (l != expr.Length)
                    if (res != null)
                        return PresentVoucherQuery(res);
            }

            throw new InvalidOperationException("表达式无效");
        }

        /// <inheritdoc />
        public bool IsExecutable(string expr) => true;

        /// <summary>
        ///     执行记账凭证检索式并呈现记账凭证
        /// </summary>
        /// <param name="query">记账凭证检索式</param>
        /// <returns>记账凭证的C#表达式</returns>
        private IQueryResult PresentVoucherQuery(IQueryCompunded<IVoucherQueryAtom> query)
        {
            var sb = new StringBuilder();
            foreach (var voucher in m_Accountant.SelectVouchers(query))
                sb.Append(CSharpHelper.PresentVoucher(voucher));
            return new EditableText(sb.ToString());
        }

        /// <summary>
        ///     执行分类汇总检索式并呈现结果
        /// </summary>
        /// <param name="query">分类汇总检索式</param>
        /// <returns>执行结果</returns>
        private IQueryResult PresentSubtotal(IGroupedQuery query)
        {
            var result = m_Accountant.SelectVoucherDetailsGrouped(query);

            return new UnEditableText(SubtotalHelper.PresentSubtotal(result, query.Subtotal));
        }
    }
}
