using System;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class ConsoleParser
    {
        public partial class DistributedQAtomContext : IDistributedQueryAtom
        {
            private class MyDistributedFilter : IDistributed
            {
                public Guid? ID { get; set; }
                public string Name { get; set; }
                public string Remark { get; set; }
            }

            public IDistributed Filter
            {
                get
                {
                    var filter = new MyDistributedFilter
                                     {
                                         ID = Guid() != null ? System.Guid.Parse(Guid().GetText()) : (Guid?)null,
                                         Name = DollarQuotedString().Dequotation(),
                                         Remark = PercentQuotedString().Dequotation()
                                     };
                    return filter;
                }
            }
        }

        public partial class DistributedQContext : IQueryAry<IDistributedQueryAtom>
        {
            public OperatorType Operator
            {
                get
                {
                    if (Op == null)
                        return OperatorType.None;
                    if (distributedQ().Count == 1)
                    {
                        if (Op.Text == "+")
                            return OperatorType.Identity;
                        if (Op.Text == "-")
                            return OperatorType.Complement;
                        throw new InvalidOperationException();
                    }
                    if (Op.Text == "+")
                        return OperatorType.Union;
                    if (Op.Text == "-")
                        return OperatorType.Substract;
                    if (Op.Text == "*")
                        return OperatorType.Intersect;
                    throw new InvalidOperationException();
                }
            }

            public IQueryCompunded<IDistributedQueryAtom> Filter1
            {
                get
                {
                    if (distributedQAtom() != null)
                        return distributedQAtom();
                    return distributedQ(0);
                }
            }

            public IQueryCompunded<IDistributedQueryAtom> Filter2 { get { return distributedQ(1); } }
        }
    }
}
