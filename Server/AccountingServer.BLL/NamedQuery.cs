using System.Collections.Generic;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    public interface INamedQuery
    {
        string Name { get; }

        double Coefficient { get; }
    }

    public interface INamedQueryReference : INamedQuery { }

    public interface INamedQueryConcrete : INamedQuery
    {
        string Remark { get; }
    }

    public interface INamedQ : INamedQueryConcrete
    {
        IGroupedQuery GroupingQuery { get; }
    }

    public interface INamedQueries : INamedQueryConcrete
    {
        IReadOnlyList<INamedQuery> Items { get; }
    }

    public abstract class NamedQueryBase : INamedQuery
    {
        public string Name { get; set; }
        public double Coefficient { get; set; }
    }

    public class NamedQueryReferenceBase : NamedQueryBase, INamedQueryReference { }

    public class NamedQueryConcreteBase : NamedQueryBase, INamedQueryConcrete
    {
        public string Remark { get; set; }
    }

    public class NamedQBase : NamedQueryConcreteBase, INamedQ
    {
        public IGroupedQuery GroupingQuery { get; set; }
    }

    public class NamedQueriesBase : NamedQueryConcreteBase, INamedQueries
    {
        public IReadOnlyList<INamedQuery> Items { get; set; }
    }
}
