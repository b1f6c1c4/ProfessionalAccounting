using System;
using System.Collections.Generic;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class ConsoleParser
    {
        public partial class NamedQueryContext : INamedQuery
        {
            private INamedQuery TheOne
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

            public string Name { get { return TheOne.Name; } }

            public double Coefficient { get { return TheOne.Coefficient; } }
        }

        public partial class NamedQueryReferenceContext : INamedQueryReference
        {
            public string Name { get { return name().GetText(); } }

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
        }

        public partial class NamedQueriesContext : INamedQueries
        {
            public string Name { get { return name().GetText(); } }

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

            public string Remark
            {
                get
                {
                    if (DoubleQuotedString() == null)
                        return null;
                    return DoubleQuotedString().GetText();
                }
            }

            public IReadOnlyList<INamedQuery> Items { get { return namedQuery(); } }
        }

        public partial class NamedQContext : INamedQ
        {
            public string Name { get { return name().GetText(); } }

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

            public string Remark
            {
                get
                {
                    if (DoubleQuotedString() == null)
                        return null;
                    return DoubleQuotedString().GetText();
                }
            }

            public IGroupedQuery GroupingQuery { get { return groupedQuery(); } }
        }
    }
}
