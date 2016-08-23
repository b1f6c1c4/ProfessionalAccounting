using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell.Parsing;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     命名查询遍历器
    /// </summary>
    /// <typeparam name="TMedium">中间类型</typeparam>
    /// <typeparam name="TResult">输出类型</typeparam>
    public class NamedQueryTraver<TMedium, TResult>
    {
        /// <summary>
        ///     公共记账凭证处理器
        /// </summary>
        /// <param name="query">公共记账凭证检索式</param>
        /// <returns>公共记账凭证</returns>
        public delegate IReadOnlyList<Voucher> PreFunc(
            IReadOnlyList<Voucher> preVouchers, IQueryCompunded<IVoucherQueryAtom> query);

        /// <summary>
        ///     原子查询处理器
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="query">查询</param>
        /// <param name="coefficient">系数</param>
        /// <param name="preVouchers">公共记账凭证</param>
        /// <returns>输出</returns>
        public delegate TResult LeafFunc(
            TMedium path, INamedQ query, double coefficient, IReadOnlyList<Voucher> preVouchers);

        /// <summary>
        ///     路径映射器
        /// </summary>
        /// <param name="path">当前路径</param>
        /// <param name="query">当前查询</param>
        /// <param name="coefficient">当前系数</param>
        /// <returns>新路径</returns>
        public delegate TMedium MapFunc(TMedium path, INamedQueries query, double coefficient);

        /// <summary>
        ///     复合查询汇聚器
        /// </summary>
        /// <param name="path">当前路径</param>
        /// <param name="newPath">次级路径</param>
        /// <param name="query">当前查询</param>
        /// <param name="coefficient">当前系数</param>
        /// <param name="results">次级查询输出</param>
        /// <returns>输出</returns>
        public delegate TResult ReduceFunc(
            TMedium path, TMedium newPath, INamedQueries query, double coefficient, IEnumerable<TResult> results);

        /// <summary>
        ///     公共记账凭证处理器
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public PreFunc Pre { get; set; }

        /// <summary>
        ///     原子查询处理器
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public LeafFunc Leaf { get; set; }

        /// <summary>
        ///     路径映射器
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public MapFunc Map { get; set; }

        /// <summary>
        ///     复合查询汇聚器
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public ReduceFunc Reduce { get; set; }

        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public NamedQueryTraver(Accountant accountant, DateFilter rng)
        {
            m_Accountant = accountant;
            Range = rng;
        }

        /// <summary>
        ///     用于赋值的日期过滤器
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public DateFilter Range { get; set; }

        /// <summary>
        ///     获取内层命名查询并解引用
        /// </summary>
        /// <param name="query">命名查询</param>
        /// <param name="preVouchers">公共记账凭证</param>
        /// <returns>最内层的命名查询（非引用）</returns>
        private INamedQueryConcrete GetConcreteQuery(INamedQuery query,
                                                     ref IReadOnlyList<Voucher> preVouchers)
        {
            var flag = true;
            while (flag)
            {
                flag = false;
                while (query is ShellParser.NamedQueryContext)
                {
                    query = (query as ShellParser.NamedQueryContext).InnerQuery;
                    if (!query.InheritQuery)
                        preVouchers = null;
                    flag = true;
                }
                while (query is INamedQueryTemplateR)
                {
                    query = Dereference(query as INamedQueryTemplateR);
                    if (!query.InheritQuery)
                        preVouchers = null;
                    flag = true;
                }
            }

            return query as INamedQueryConcrete;
        }

        /// <summary>
        ///     遍历命名查询同时应用模板
        /// </summary>
        /// <param name="initialPath">初始路径</param>
        /// <param name="query">命名查询模板</param>
        /// <returns>输出</returns>
        public TResult Traversal(TMedium initialPath, INamedQuery query) => Traversal(initialPath, query, 1, null);

        /// <summary>
        ///     遍历命名查询
        /// </summary>
        /// <param name="path">当前路径</param>
        /// <param name="query">当前查询</param>
        /// <param name="coefficient">当前系数</param>
        /// <param name="preVouchers">公共记账凭证</param>
        /// <returns>输出</returns>
        private TResult Traversal(TMedium path, INamedQuery query, double coefficient,
                                  IReadOnlyList<Voucher> preVouchers)
        {
            var q = GetConcreteQuery(query, ref preVouchers);

            if (q is INamedQ)
                return Leaf(path, q as INamedQ, coefficient, preVouchers);

            if (q is INamedQueries)
            {
                var qs = q as INamedQueries;
                if (!qs.InheritQuery)
                    preVouchers = Pre(null, qs.CommonQuery);
                else if (qs.CommonQuery != null)
                    preVouchers = Pre(preVouchers, qs.CommonQuery);
                var newPath = Map(path, qs, coefficient);
                return Reduce(
                              path,
                              newPath,
                              qs,
                              coefficient,
                              qs.Items.Select(nq => Traversal(newPath, nq, coefficient * qs.Coefficient, preVouchers)));
            }

            throw new ArgumentException("命名查询类型未知", nameof(query));
        }

        /// <summary>
        ///     对命名查询模板的名称解引用并套用模板
        /// </summary>
        /// <param name="reference">名称</param>
        /// <returns>命名查询</returns>
        private INamedQuery Dereference(string reference)
        {
            string range, leftExtendedRange;
            if (Range.NullOnly)
                range = leftExtendedRange = "[null]";
            else
            {
                if (Range.StartDate.HasValue)
                    range = Range.EndDate.HasValue
                                ? $"[{Range.StartDate:yyyyMMdd}{(Range.Nullable ? "=" : "~")}{Range.EndDate:yyyyMMdd}]"
                                : $"[{Range.StartDate:yyyyMMdd}{(Range.Nullable ? "=" : "~")}]";
                else if (Range.Nullable)
                    range = Range.EndDate.HasValue ? $"[~{Range.EndDate:yyyyMMdd}]" : "[]";
                else
                    range = Range.EndDate.HasValue ? $"[={Range.EndDate:yyyyMMdd}]" : "[~null]";
                leftExtendedRange = !Range.EndDate.HasValue ? "[]" : $"[~{Range.EndDate:yyyyMMdd}]";
            }

            var templateStr = m_Accountant.SelectNamedQueryTemplate(reference)
                                          .Replace("[&RANGE&]", range)
                                          .Replace("[&LEFTEXTENDEDRANGE&]", leftExtendedRange);

            var template = ShellParser.From(templateStr).namedQuery();
            return template;
        }

        /// <summary>
        ///     对命名查询模板引用解引用并套用模板
        /// </summary>
        /// <param name="reference">命名查询模板引用</param>
        /// <returns>命名查询</returns>
        private INamedQuery Dereference(INamedQueryTemplateR reference) => Dereference(reference.Name);
    }
}
