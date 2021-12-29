/* Copyright (C) 2020-2021 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using static AccountingServer.Entities.Util.SecurityHelper;

namespace AccountingServer.BLL.Parsing;

internal partial class QueryParser
{
    public partial class DistributedQAtomContext : IClientDependable, IDistributedQueryAtom
    {
        /// <inheritdoc />
        public IDistributed Filter
            => new MyDistributedFilter
                {
                    ID = Guid() != null ? System.Guid.Parse(Guid().GetText()) : null,
                    User = (UserSpec()?.GetText()).ParseUserSpec(Client),
                    Name = RegexString()?.GetText().Dequotation().Replace(@"\/", "/"),
                    Remark = PercentQuotedString()?.GetText().Dequotation(),
                };

        /// <inheritdoc />
        public DateFilter Range
        {
            get
            {
                if (rangeCore() == null)
                    return DateFilter.Unconstrained;

                rangeCore().Client = Client;
                return rangeCore().Range;
            }
        }

        /// <inheritdoc />
        public bool IsDangerous() => Filter.IsDangerous() && Range.IsDangerous(true);

        /// <inheritdoc />
        public T Accept<T>(IQueryVisitor<IDistributedQueryAtom, T> visitor) => visitor.Visit(this);

        /// <summary>
        ///     分期过滤器
        /// </summary>
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private sealed class MyDistributedFilter : IDistributed
        {
            /// <inheritdoc />
            public Guid? ID { get; init; }

            /// <inheritdoc />
            public string User { get; init; }

            /// <inheritdoc />
            public string Name { get; init; }

            /// <inheritdoc />
            public DateTime? Date { get; set; }

            /// <inheritdoc />
            public double? Value { get; set; }

            /// <inheritdoc />
            public string Remark { get; init; }

            /// <inheritdoc />
            public IEnumerable<IDistributedItem> TheSchedule { get; set; }
        }

        public Client Client { private get; set; }
    }

    public partial class DistributedQContext : IClientDependable, IQueryAry<IDistributedQueryAtom>
    {
        /// <inheritdoc />
        public OperatorType Operator
            => Op switch
                {
                    null => OperatorType.None,
                        { Text: var x } when distributedQ() == null => x switch
                        {
                            "+" => OperatorType.Identity,
                            "-" => OperatorType.Complement,
                            _ => throw new MemberAccessException("表达式错误"),
                        },
                        { Text: var x } => x switch
                        {
                            "+" => OperatorType.Union,
                            "-" => OperatorType.Subtract,
                            _ => throw new MemberAccessException("表达式错误"),
                        },
                };

        /// <inheritdoc />
        public IQueryCompounded<IDistributedQueryAtom> Filter1
        {
            get
            {
                if (distributedQ() != null)
                {
                    distributedQ().Client = Client;
                    return distributedQ();
                }

                distributedQ1().Client = Client;
                return distributedQ1();
            }
        }

        /// <inheritdoc />
        public IQueryCompounded<IDistributedQueryAtom> Filter2
        {
            get
            {
                distributedQ1().Client = Client;
                return distributedQ1();
            }
        }

        /// <inheritdoc />
        public bool IsDangerous()
            => Operator switch
                {
                    OperatorType.None => Filter1.IsDangerous(),
                    OperatorType.Identity => Filter1.IsDangerous(),
                    OperatorType.Complement => true,
                    OperatorType.Union => Filter1.IsDangerous() || Filter2.IsDangerous(),
                    OperatorType.Subtract => Filter1.IsDangerous(),
                    _ => throw new ArgumentOutOfRangeException(),
                };

        /// <inheritdoc />
        public T Accept<T>(IQueryVisitor<IDistributedQueryAtom, T> visitor) => visitor.Visit(this);

        public Client Client { private get; set; }
    }

    public partial class DistributedQ1Context : IClientDependable, IQueryAry<IDistributedQueryAtom>
    {
        /// <inheritdoc />
        public OperatorType Operator => Op == null ? OperatorType.None : OperatorType.Intersect;

        /// <inheritdoc />
        public IQueryCompounded<IDistributedQueryAtom> Filter1
        {
            get
            {
                distributedQ0().Client = Client;
                return distributedQ0();
            }
        }

        /// <inheritdoc />
        public IQueryCompounded<IDistributedQueryAtom> Filter2
        {
            get
            {
                distributedQ1().Client = Client;
                return distributedQ1();
            }
        }

        /// <inheritdoc />
        public bool IsDangerous() => Filter1.IsDangerous() && (Filter2?.IsDangerous() ?? true);

        /// <inheritdoc />
        public T Accept<T>(IQueryVisitor<IDistributedQueryAtom, T> visitor) => visitor.Visit(this);

        public Client Client { private get; set; }
    }

    public partial class DistributedQ0Context : IClientDependable, IQueryAry<IDistributedQueryAtom>
    {
        /// <inheritdoc />
        public OperatorType Operator => OperatorType.None;

        /// <inheritdoc />
        public IQueryCompounded<IDistributedQueryAtom> Filter1
        {
            get
            {
                if (distributedQAtom() != null)
                {
                    distributedQAtom().Client = Client;
                    return distributedQAtom();
                }

                distributedQ().Client = Client;
                return distributedQ();
            }
        }

        /// <inheritdoc />
        public IQueryCompounded<IDistributedQueryAtom> Filter2 => null;

        /// <inheritdoc />
        public bool IsDangerous() => Filter1.IsDangerous();

        /// <inheritdoc />
        public T Accept<T>(IQueryVisitor<IDistributedQueryAtom, T> visitor) => visitor.Visit(this);

        public Client Client { private get; set; }
    }
}
