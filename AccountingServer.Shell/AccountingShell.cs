using System;
using AccountingServer.BLL;
using AccountingServer.Entities;
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
        private readonly IEntitiesSerializer m_Serializer;

        public AccountingShell(Accountant helper, IEntitiesSerializer serializer)
        {
            m_Accountant = helper;
            m_Serializer = serializer;
        }

        /// <inheritdoc />
        public IQueryResult Execute(string expr) => Parse(expr)();

        /// <inheritdoc />
        public bool IsExecutable(string expr) => true;

        /// <summary>
        ///     解析表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>解析结果</returns>
        private Func<IQueryResult> Parse(string expr)
        {
            var isRaw = false;
            ISubtotalStringify visitor;
            var serializer = m_Serializer;
            if (ParsingF.Token(ref expr, false, t => t == "json") != null)
            {
                visitor = new JsonSubtotal();
                serializer = new JsonSerializer();
            }
            else if (ParsingF.Token(ref expr, false, t => t == "json-raw") != null)
            {
                visitor = new JsonSubtotal();
                serializer = new JsonSerializer();
                isRaw = true;
            }
            else if (ParsingF.Token(ref expr, false, t => t == "Rps") != null)
                visitor = new PreciseSubtotalPre();
            else if (ParsingF.Token(ref expr, false, t => t == "rps") != null)
                visitor = new PreciseSubtotalPre(false);
            else if (ParsingF.Token(ref expr, false, t => t == "raw") != null)
            {
                visitor = new RawSubtotal();
                isRaw = true;
            }
            else
                visitor = new RichSubtotalPre();

            try
            {
                return TryGroupedQuery(expr, visitor);
            }
            catch (Exception)
            {
                // ignored
            }

            try
            {
                return isRaw ? TryDetailQuery(expr, serializer) : TryVoucherQuery(expr, serializer);
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
        /// <param name="serializer">表示器</param>
        /// <returns>执行结果</returns>
        private Func<IQueryResult> TryVoucherQuery(string expr, IEntitiesSerializer serializer)
        {
            if (string.IsNullOrWhiteSpace(expr))
                throw new ApplicationException("不允许执行空白检索式");

            var res = ParsingF.VoucherQuery(ref expr);
            ParsingF.Eof(expr);
            return () => PresentVoucherQuery(res, serializer);
        }

        /// <summary>
        ///     按分类汇总检索式解析
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <param name="trav">呈现器</param>
        /// <returns>执行结果</returns>
        private Func<IQueryResult> TryGroupedQuery(string expr, ISubtotalStringify trav)
        {
            var res = ParsingF.GroupedQuery(ref expr);
            ParsingF.Eof(expr);
            return () => PresentSubtotal(res, trav);
        }

        /// <summary>
        ///     执行记账凭证检索式并呈现记账凭证
        /// </summary>
        /// <param name="query">记账凭证检索式</param>
        /// <param name="serializer">表示器</param>
        /// <returns>记账凭证表达式</returns>
        private IQueryResult PresentVoucherQuery(IQueryCompunded<IVoucherQueryAtom> query,
            IEntitiesSerializer serializer)
            => new EditableText(serializer.PresentVouchers(m_Accountant.SelectVouchers(query)));

        /// <summary>
        ///     按细目检索式解析
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <param name="serializer">表示器</param>
        /// <returns>执行结果</returns>
        private Func<IQueryResult> TryDetailQuery(string expr, IEntitiesSerializer serializer)
        {
            var res = ParsingF.DetailQuery(ref expr);
            ParsingF.Eof(expr);
            return () => PresentDetailQuery(res, serializer);
        }

        /// <summary>
        ///     执行细目检索式并呈现结果
        /// </summary>
        /// <param name="query">细目检索式</param>
        /// <param name="serializer">表示器</param>
        /// <returns>执行结果</returns>
        private IQueryResult PresentDetailQuery(IVoucherDetailQuery query, IEntitiesSerializer serializer)
            => new EditableText(serializer.PresentVoucherDetails(m_Accountant.SelectVoucherDetails(query)));

        /// <summary>
        ///     执行分类汇总检索式并呈现结果
        /// </summary>
        /// <param name="query">分类汇总检索式</param>
        /// <param name="trav">呈现器</param>
        /// <returns>执行结果</returns>
        private IQueryResult PresentSubtotal(IGroupedQuery query, ISubtotalStringify trav)
        {
            var result = m_Accountant.SelectVoucherDetailsGrouped(query);
            return new UnEditableText(trav.PresentSubtotal(result, query.Subtotal));
        }
    }
}
