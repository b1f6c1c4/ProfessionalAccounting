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

        public AccountingShell(Accountant helper) => m_Accountant = helper;

        /// <inheritdoc />
        public IQueryResult Execute(string expr, IEntitiesSerializer serializer) => Parse(expr)(serializer);

        /// <inheritdoc />
        public bool IsExecutable(string expr) => true;

        /// <summary>
        ///     解析表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>解析结果</returns>
        private Func<IEntitiesSerializer, IQueryResult> Parse(string expr)
        {
            var isRaw = false;
            ISubtotalStringify visitor;
            if (ParsingF.Token(ref expr, false, t => t == "json") != null)
                visitor = new JsonSubtotal();
            else if (ParsingF.Token(ref expr, false, t => t == "json-raw") != null)
            {
                visitor = new JsonSubtotal();
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
                return isRaw ? TryDetailQuery(expr) : TryVoucherQuery(expr);
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
        private Func<IEntitiesSerializer, IQueryResult> TryVoucherQuery(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr))
                throw new ApplicationException("不允许执行空白检索式");

            var res = ParsingF.VoucherQuery(ref expr);
            ParsingF.Eof(expr);
            return serializer => PresentVoucherQuery(res, serializer);
        }

        /// <summary>
        ///     按分类汇总检索式解析
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <param name="trav">呈现器</param>
        /// <returns>执行结果</returns>
        private Func<IEntitiesSerializer, IQueryResult> TryGroupedQuery(string expr, ISubtotalStringify trav)
        {
            var res = ParsingF.GroupedQuery(ref expr);
            ParsingF.Eof(expr);
            return serializer => PresentSubtotal(res, trav);
        }

        /// <summary>
        ///     执行记账凭证检索式并呈现记账凭证
        /// </summary>
        /// <param name="query">记账凭证检索式</param>
        /// <param name="serializer">表示器</param>
        /// <returns>记账凭证表达式</returns>
        private IQueryResult PresentVoucherQuery(IQueryCompunded<IVoucherQueryAtom> query,
            IEntitiesSerializer serializer)
            => new PlainText(serializer.PresentVouchers(m_Accountant.SelectVouchers(query)));

        /// <summary>
        ///     按细目检索式解析
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private Func<IEntitiesSerializer, IQueryResult> TryDetailQuery(string expr)
        {
            var res = ParsingF.DetailQuery(ref expr);
            ParsingF.Eof(expr);
            return serializer => PresentDetailQuery(res, serializer);
        }

        /// <summary>
        ///     执行细目检索式并呈现结果
        /// </summary>
        /// <param name="query">细目检索式</param>
        /// <param name="serializer">表示器</param>
        /// <returns>执行结果</returns>
        private IQueryResult PresentDetailQuery(IVoucherDetailQuery query, IEntitiesSerializer serializer)
            => new PlainText(serializer.PresentVoucherDetails(m_Accountant.SelectVoucherDetails(query)));

        /// <summary>
        ///     执行分类汇总检索式并呈现结果
        /// </summary>
        /// <param name="query">分类汇总检索式</param>
        /// <param name="trav">呈现器</param>
        /// <returns>执行结果</returns>
        private IQueryResult PresentSubtotal(IGroupedQuery query, ISubtotalStringify trav)
        {
            var result = m_Accountant.SelectVoucherDetailsGrouped(query);
            return new PlainText(trav.PresentSubtotal(result, query.Subtotal));
        }
    }
}
