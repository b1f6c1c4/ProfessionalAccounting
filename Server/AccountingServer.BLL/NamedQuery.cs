using System.Collections.Generic;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     命名查询
    /// </summary>
    public interface INamedQuery
    {
        /// <summary>
        ///     名称
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     是否继承公共记账凭证检索式
        /// </summary>
        bool InheritQuery { get; }
    }

    /// <summary>
    ///     命名查询模板引用
    /// </summary>
    public interface INamedQueryTemplateR : INamedQuery { }

    /// <summary>
    ///     命名查询（非引用）
    /// </summary>
    public interface INamedQueryConcrete : INamedQuery
    {
        /// <summary>
        ///     系数
        /// </summary>
        double Coefficient { get; }

        /// <summary>
        ///     备注
        /// </summary>
        string Remark { get; }
    }

    /// <summary>
    ///     原子命名查询
    /// </summary>
    public interface INamedQ : INamedQueryConcrete
    {
        /// <summary>
        ///     分类汇总检索式
        /// </summary>
        IGroupedQuery GroupingQuery { get; }
    }

    /// <summary>
    ///     复合命名查询
    /// </summary>
    public interface INamedQueries : INamedQueryConcrete
    {
        /// <summary>
        ///     子命名查询
        /// </summary>
        // ReSharper disable once ReturnTypeCanBeEnumerable.Global
        IReadOnlyList<INamedQuery> Items { get; }

        /// <summary>
        ///     公共记账凭证检索式
        /// </summary>
        IQueryCompunded<IVoucherQueryAtom> CommonQuery { get; }
    }

    public class NamedQBase : INamedQ {
        public string Name { get; set; }
        public bool InheritQuery { get; set; }
        public double Coefficient { get; set; }
        public string Remark { get; set; }
        public IGroupedQuery GroupingQuery { get; set; }
    }
}
