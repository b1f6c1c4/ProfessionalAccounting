using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.Entities;
using Antlr4.Runtime;

namespace AccountingServer.Console
{
    /// <summary>
    ///     命名查询模板遍历器
    /// </summary>
    /// <typeparam name="TMedium">中间类型</typeparam>
    /// <typeparam name="TResult">输出类型</typeparam>
    public class NamedQueryTraver<TMedium, TResult>
    {
        public delegate TResult LeafFunc(TMedium path, INamedQ query, double coefficient);

        public delegate TMedium MapFunc(TMedium path, INamedQueries query, double coefficient);

        public delegate TResult ReduceFunc(
            TMedium path, INamedQueries query, double coefficient, IEnumerable<TResult> results);

        public LeafFunc Leaf { get; set; }

        public MapFunc Map { get; set; }

        public ReduceFunc Reduce { get; set; }

        private readonly Accountant m_Accountant;

        public NamedQueryTraver(Accountant accountant, DateFilter rng)
        {
            m_Accountant = accountant;
            Range = rng;
        }

        /// <summary>
        ///     用于赋值的日期过滤器
        /// </summary>
        public DateFilter Range { get; set; }

        private INamedQueryConcrete GetConcreteQuery(INamedQuery query)
        {
            while (query is ConsoleParser.NamedQueryContext)
                query = (query as ConsoleParser.NamedQueryContext).InnerQuery;
            while (query is INamedQueryReference)
                query = Dereference(query as INamedQueryReference);

            return query as INamedQueryConcrete;
        }

        public TResult Traversal(TMedium initialPath, INamedQuery query) { return Traversal(initialPath, query, 1); }

        private TResult Traversal(TMedium path, INamedQuery query, double coefficient)
        {
            var q = GetConcreteQuery(query);

            if (q is INamedQ)
                return Leaf(path, q as INamedQ, coefficient);

            if (q is INamedQueries)
            {
                var qs = q as INamedQueries;
                var newPath = Map(path, qs, coefficient);
                return Reduce(
                              path,
                              qs,
                              coefficient,
                              qs.Items.Select(nq => Traversal(newPath, nq, coefficient * qs.Coefficient)));
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        ///     对命名查询模板的名称解引用
        /// </summary>
        /// <param name="reference">名称</param>
        /// <returns>命名查询模板</returns>
        public INamedQuery Dereference(string reference)
        {
            string range, leftExtendedRange;
            if (Range.NullOnly)
                range = leftExtendedRange = "[null]";
            else
            {
                if (Range.StartDate.HasValue)
                    range = Range.EndDate.HasValue
                                ? String.Format(
                                                "[{0:yyyyMMdd}{2}{1:yyyyMMdd}]",
                                                Range.StartDate,
                                                Range.EndDate,
                                                Range.Nullable ? "=" : "~")
                                : String.Format("[{0:yyyyMMdd}{1}]", Range.StartDate, Range.Nullable ? "=" : "~");
                else if (Range.Nullable)
                    range = Range.EndDate.HasValue ? String.Format("[~{0:yyyyMMdd}]", Range.EndDate) : "[]";
                else
                    range = Range.EndDate.HasValue ? String.Format("[={0:yyyyMMdd}]", Range.EndDate) : "[~null]";
                leftExtendedRange = !Range.EndDate.HasValue ? "[]" : String.Format("[~{0:yyyyMMdd}]", Range.EndDate);
            }

            var templateStr = m_Accountant.SelectNamedQueryTemplate(reference)
                                          .Replace("[&RANGE&]", range)
                                          .Replace("[&LEFTEXTENDEDRANGE&]", leftExtendedRange);

            var parser = new ConsoleParser(new CommonTokenStream(new ConsoleLexer(new AntlrInputStream(templateStr))));
            var template = parser.namedQuery();
            return template;
        }

        /// <summary>
        ///     对命名查询模板引用解引用
        /// </summary>
        /// <param name="reference">命名查询模板引用</param>
        /// <returns>命名查询模板</returns>
        private INamedQuery Dereference(INamedQueryReference reference)
        {
            return Dereference(reference.Name);
        }
    }
}
