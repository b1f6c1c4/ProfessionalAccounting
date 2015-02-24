﻿using System;
using System.Collections.Generic;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class ConsoleParser
    {
        public partial class NamedQueryContext : INamedQuery
        {
            /// <inheritdoc />
            public INamedQuery InnerQuery
            {
                get
                {
                    if (namedQ() != null)
                        return namedQ();
                    if (namedQueries() != null)
                        return namedQueries();
                    if (namedQueryReference() != null)
                        return namedQueryReference();
                    throw new InvalidOperationException();
                }
            }

            /// <inheritdoc />
            public string Name { get { return InnerQuery.Name; } }
        }

        public partial class NamedQueryReferenceContext : INamedQueryReference
        {
            /// <inheritdoc />
            public string Name { get { return name().DollarQuotedString().Dequotation(); } }
        }

        public partial class NamedQueriesContext : INamedQueries
        {
            /// <inheritdoc />
            public string Name { get { return name().DollarQuotedString().Dequotation(); } }

            /// <inheritdoc />
            public double Coefficient
            {
                get
                {
                    if (coef() == null)
                        return 1;
                    if (coef().Percent() != null)
                    {
                        var s = coef().Percent().GetText();
                        return Double.Parse(s.Substring(1, s.Length - 2)) / 100D;
                    }
                    if (coef().Float() != null)
                    {
                        var s = coef().Float().GetText();
                        return Double.Parse(s.Substring(1, s.Length - 1));
                    }
                    throw new InvalidOperationException();
                }
            }

            /// <inheritdoc />
            public string Remark { get { return DoubleQuotedString().Dequotation(); } }

            /// <inheritdoc />
            public IReadOnlyList<INamedQuery> Items { get { return namedQuery(); } }
        }

        public partial class NamedQContext : INamedQ
        {
            /// <inheritdoc />
            public string Name { get { return name().DollarQuotedString().Dequotation(); } }

            /// <inheritdoc />
            public double Coefficient
            {
                get
                {
                    if (coef() == null)
                        return 1D;
                    if (coef().Percent() != null)
                    {
                        var s = coef().Percent().GetText();
                        return Double.Parse(s.Substring(1, s.Length - 2)) / 100D;
                    }
                    if (coef().Float() != null)
                    {
                        var s = coef().Float().GetText();
                        return Double.Parse(s.Substring(1, s.Length - 1));
                    }
                    throw new InvalidOperationException();
                }
            }

            /// <inheritdoc />
            public string Remark { get { return DoubleQuotedString().Dequotation(); } }

            /// <inheritdoc />
            public IGroupedQuery GroupingQuery { get { return groupedQuery(); } }
        }
    }
}