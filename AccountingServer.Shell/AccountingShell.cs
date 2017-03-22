using System;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Subtotal;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.FacadeF;

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

        /// <summary>
        ///     表示器
        /// </summary>
        private readonly IEntitySerializer m_Serializer;

        public AccountingShell(Accountant helper, IEntitySerializer serializer)
        {
            m_Accountant = helper;
            m_Serializer = serializer;
        }

        /// <inheritdoc />
        public IQueryResult Execute(string expr) => Parse(expr)();

        /// <summary>
        ///     解析表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>解析结果</returns>
        private Func<IQueryResult> Parse(string expr)
        {
            if (ParsingF.Token(ref expr, false, t => t == "Rps") != null)
            {
                try
                {
                    return TryGroupedQuery(expr, new PreciseSubtotalPre());
                }
                catch (Exception)
                {
                    // ignored
                }

                try
                {
                    return TryDetailQuery(expr);
                }
                catch (Exception)
                {
                    // ignored
                }

                throw new InvalidOperationException("表达式无效");
            }

            if (ParsingF.Token(ref expr, false, t => t == "rps") != null)
            {
                try
                {
                    return TryGroupedQuery(expr, new PreciseSubtotalPre(false));
                }
                catch (Exception)
                {
                    // ignored
                }

                try
                {
                    return TryDetailQuery(expr);
                }
                catch (Exception)
                {
                    // ignored
                }

                throw new InvalidOperationException("表达式无效");
            }

            try
            {
                return TryGroupedQuery(expr, new RichSubtotalPre());
            }
            catch (Exception)
            {
                // ignored
            }

            try
            {
                return TryVoucherQuery(expr);
            }
            catch (Exception)
            {
                // ignored
            }

            throw new InvalidOperationException("表达式无效");
        }

        /// <summary>
        ///     按记账凭证检索式解析
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private Func<IQueryResult> TryVoucherQuery(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr))
                throw new ApplicationException("不允许执行空白检索式");

            var res = ParsingF.VoucherQuery(ref expr);
            ParsingF.Eof(expr);
            return () => PresentVoucherQuery(res);
        }

        /// <summary>
        ///     按分类汇总检索式解析
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <param name="trav">呈现器</param>
        /// <returns>执行结果</returns>
        private Func<IQueryResult> TryGroupedQuery(string expr, ISubtotalPre trav)
        {
            var res = ParsingF.GroupedQuery(ref expr);
            ParsingF.Eof(expr);
            return () => PresentSubtotal(res, trav);
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
                sb.Append(m_Serializer.PresentVoucher(voucher));

            return new EditableText(sb.ToString());
        }

        /// <summary>
        ///     按细目检索式解析
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private Func<IQueryResult> TryDetailQuery(string expr)
        {
            var res = ParsingF.DetailQuery(ref expr);
            ParsingF.Eof(expr);
            return () => PresentDetailQuery(res);
        }

        /// <summary>
        ///     执行细目检索式并呈现结果
        /// </summary>
        /// <param name="query">细目检索式</param>
        /// <returns>执行结果</returns>
        private IQueryResult PresentDetailQuery(IVoucherDetailQuery query)
        {
            var sb = new StringBuilder();
            var q = query.DetailEmitFilter?.DetailFilter ?? (query.VoucherQuery as IVoucherQueryAtom)?.DetailFilter;
            foreach (var voucher in m_Accountant.SelectVouchers(query.VoucherQuery))
            foreach (var d in voucher.Details)
                if (d.IsMatch(q))
                        sb.Append(m_Serializer.PresentVoucherDetail(d));

            return new EditableText(sb.ToString());
        }

        /// <summary>
        ///     执行分类汇总检索式并呈现结果
        /// </summary>
        /// <param name="query">分类汇总检索式</param>
        /// <param name="trav">呈现器</param>
        /// <returns>执行结果</returns>
        private IQueryResult PresentSubtotal(IGroupedQuery query, ISubtotalPre trav)
        {
            var result = m_Accountant.SelectVoucherDetailsGrouped(query);
            trav.SubtotalArgs = query.Subtotal;
            return new UnEditableText(trav.PresentSubtotal(result));
        }
    }
}
