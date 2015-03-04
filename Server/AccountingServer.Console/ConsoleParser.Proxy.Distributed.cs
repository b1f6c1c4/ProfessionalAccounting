using System;
using System.Collections.Generic;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class ConsoleParser
    {
        public partial class DistributedQAtomContext : IDistributedQueryAtom
        {
            /// <summary>
            ///     分期过滤器
            /// </summary>
            private class MyDistributedFilter : IDistributed
            {
                /// <inheritdoc />
                public Guid? ID { get; set; }

                /// <inheritdoc />
                public string Name { get; set; }

                /// <inheritdoc />
                public DateTime? Date { get; set; }

                /// <inheritdoc />
                // ReSharper disable once UnusedAutoPropertyAccessor.Local
                public double? Value { get; set; }

                /// <inheritdoc />
                public string Remark { get; set; }

                /// <inheritdoc />
                // ReSharper disable once UnusedAutoPropertyAccessor.Local
                public IEnumerable<IDistributedItem> TheSchedule { get; set; }
            }

            /// <inheritdoc />
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
            /// <inheritdoc />
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

            /// <inheritdoc />
            public IQueryCompunded<IDistributedQueryAtom> Filter1
            {
                get
                {
                    if (distributedQAtom() != null)
                        return distributedQAtom();
                    return distributedQ(0);
                }
            }

            /// <inheritdoc />
            public IQueryCompunded<IDistributedQueryAtom> Filter2 { get { return distributedQ(1); } }
        }
    }
}
