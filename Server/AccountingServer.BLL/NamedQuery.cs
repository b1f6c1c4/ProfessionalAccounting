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
    }

    /// <summary>
    ///     命名查询引用
    /// </summary>
    public interface INamedQueryReference : INamedQuery { }

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
    }
}
