using AccountingServer.Entities;

namespace AccountingServer.BLL.Util
{
    public sealed class IntersectQueries<TAtom> : IQueryAry<TAtom> where TAtom : class
    {
        public IntersectQueries(IQueryCompunded<TAtom> filter1,
            IQueryCompunded<TAtom> filter2)
        {
            Filter1 = filter1;
            Filter2 = filter2;
        }

        public OperatorType Operator => OperatorType.Intersect;

        public IQueryCompunded<TAtom> Filter1 { get; }

        public IQueryCompunded<TAtom> Filter2 { get; }

        public void Accept(IQueryVisitor<TAtom> visitor) => visitor.Visit(this);
        public T Accept<T>(IQueryVisitor<TAtom, T> visitor) => visitor.Visit(this);
    }
}
