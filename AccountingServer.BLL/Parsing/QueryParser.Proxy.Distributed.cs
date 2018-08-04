using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;

namespace AccountingServer.BLL.Parsing
{
    internal partial class QueryParser
    {
        public partial class DistributedQAtomContext : IDistributedQueryAtom
        {
            /// <inheritdoc />
            public IDistributed Filter
            {
                get
                {
                    var filter = new MyDistributedFilter
                        {
                            ID = Guid() != null ? System.Guid.Parse(Guid().GetText()) : (Guid?)null,
                            Name = DollarQuotedString()?.GetText().Dequotation(),
                            Remark = PercentQuotedString()?.GetText().Dequotation()
                        };
                    return filter;
                }
            }

            /// <inheritdoc />
            public void Accept(IQueryVisitor<IDistributedQueryAtom> visitor) => visitor.Visit(this);

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IDistributedQueryAtom, T> visitor) => visitor.Visit(this);

            /// <summary>
            ///     分期过滤器
            /// </summary>
            [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
            private sealed class MyDistributedFilter : IDistributed
            {
                /// <inheritdoc />
                public Guid? ID { get; set; }

                /// <inheritdoc />
                public string Name { get; set; }

                /// <inheritdoc />
                public DateTime? Date { get; set; }

                /// <inheritdoc />
                public double? Value { get; set; }

                /// <inheritdoc />
                public string Remark { get; set; }

                /// <inheritdoc />
                public IEnumerable<IDistributedItem> TheSchedule { get; set; }
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

                    if (distributedQ() == null)
                        switch (Op.Text)
                        {
                            case "+":
                                return OperatorType.Identity;
                            case "-":
                                return OperatorType.Complement;
                            default:
                                throw new MemberAccessException("表达式错误");
                        }

                    switch (Op.Text)
                    {
                        case "+":
                            return OperatorType.Union;
                        case "-":
                            return OperatorType.Substract;
                        default:
                            throw new MemberAccessException("表达式错误");
                    }
                }
            }

            /// <inheritdoc />
            public IQueryCompunded<IDistributedQueryAtom> Filter1
            {
                get
                {
                    if (distributedQ() != null)
                        return distributedQ();

                    return distributedQ1();
                }
            }

            /// <inheritdoc />
            public IQueryCompunded<IDistributedQueryAtom> Filter2 => distributedQ1();

            /// <inheritdoc />
            public void Accept(IQueryVisitor<IDistributedQueryAtom> visitor) => visitor.Visit(this);

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IDistributedQueryAtom, T> visitor) => visitor.Visit(this);
        }

        public partial class DistributedQ1Context : IQueryAry<IDistributedQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator => Op == null ? OperatorType.None : OperatorType.Intersect;

            /// <inheritdoc />
            public IQueryCompunded<IDistributedQueryAtom> Filter1 => distributedQ0();

            /// <inheritdoc />
            public IQueryCompunded<IDistributedQueryAtom> Filter2 => distributedQ1();

            /// <inheritdoc />
            public void Accept(IQueryVisitor<IDistributedQueryAtom> visitor) => visitor.Visit(this);

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IDistributedQueryAtom, T> visitor) => visitor.Visit(this);
        }

        public partial class DistributedQ0Context : IQueryAry<IDistributedQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator => OperatorType.None;

            /// <inheritdoc />
            public IQueryCompunded<IDistributedQueryAtom> Filter1 => distributedQAtom() ??
                (IQueryCompunded<IDistributedQueryAtom>)distributedQ();

            /// <inheritdoc />
            public IQueryCompunded<IDistributedQueryAtom> Filter2 => null;

            /// <inheritdoc />
            public void Accept(IQueryVisitor<IDistributedQueryAtom> visitor) => visitor.Visit(this);

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IDistributedQueryAtom, T> visitor) => visitor.Visit(this);
        }
    }
}
