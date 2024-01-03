/* Copyright (C) 2020-2024 b1f6c1c4
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

namespace AccountingServer.BLL.Parsing;

internal partial class QueryParser
{
    public partial class DistributedQAtomContext : IClientDependable, IDistributedQueryAtom
    {
        public Client Client { private get; set; }

        /// <inheritdoc />
        public IDistributed Filter
            => new MyDistributedFilter
                {
                    ID = Guid() != null ? System.Guid.Parse(Guid().GetText()) : null,
                    User = (UserSpec()?.GetText()).ParseUserSpec(Client ?? new()), // workaround IsDangerous
                    Name = RegexString()?.GetText().Dequotation().Replace(@"\/", "/"),
                    Remark = PercentQuotedString()?.GetText().Dequotation(),
                };

        /// <inheritdoc />
        public DateFilter Range
            => rangeCore().Assign(Client)?.Range ?? DateFilter.Unconstrained;

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
    }

    public partial class DistributedQContext : IClientDependable, IQueryAry<IDistributedQueryAtom>
    {
        public Client Client { private get; set; }

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
            => (IQueryCompounded<IDistributedQueryAtom>)distributedQ().Assign(Client) ?? distributedQ1().Assign(Client);

        /// <inheritdoc />
        public IQueryCompounded<IDistributedQueryAtom> Filter2
            => distributedQ1().Assign(Client);

        /// <inheritdoc />
        public T Accept<T>(IQueryVisitor<IDistributedQueryAtom, T> visitor) => visitor.Visit(this);
    }

    public partial class DistributedQ1Context : IClientDependable, IQueryAry<IDistributedQueryAtom>
    {
        public Client Client { private get; set; }

        /// <inheritdoc />
        public OperatorType Operator => Op == null ? OperatorType.None : OperatorType.Intersect;

        /// <inheritdoc />
        public IQueryCompounded<IDistributedQueryAtom> Filter1
            => distributedQ0().Assign(Client);

        /// <inheritdoc />
        public IQueryCompounded<IDistributedQueryAtom> Filter2
            => distributedQ1().Assign(Client);

        /// <inheritdoc />
        public T Accept<T>(IQueryVisitor<IDistributedQueryAtom, T> visitor) => visitor.Visit(this);
    }

    public partial class DistributedQ0Context : IClientDependable, IQueryAry<IDistributedQueryAtom>
    {
        public Client Client { private get; set; }

        /// <inheritdoc />
        public OperatorType Operator => OperatorType.None;

        /// <inheritdoc />
        public IQueryCompounded<IDistributedQueryAtom> Filter1
            => (IQueryCompounded<IDistributedQueryAtom>)distributedQAtom().Assign(Client) ??
                distributedQ().Assign(Client);

        /// <inheritdoc />
        public IQueryCompounded<IDistributedQueryAtom> Filter2 => null;

        /// <inheritdoc />
        public T Accept<T>(IQueryVisitor<IDistributedQueryAtom, T> visitor) => visitor.Visit(this);
    }
}
