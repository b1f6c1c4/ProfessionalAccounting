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

using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Carry
{
    /// <summary>
    ///     汇率
    /// </summary>
    internal class ExchangeShell : IShellComponent
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public ExchangeShell(Accountant helper) => m_Accountant = helper;

        public IQueryResult Execute(string expr, IEntitiesSerializer serializer)
        {
            expr = expr.Rest();
            var rev = true;
            var val = Parsing.Double(ref expr);
            var curr = Parsing.Token(ref expr).ToUpperInvariant();
            if (!val.HasValue)
            {
                rev = false;
                val = Parsing.DoubleF(ref expr);
            }

            var date = Parsing.UniqueTime(ref expr) ?? ClientDateTime.Today;
            Parsing.Eof(expr);
            var res = rev ? m_Accountant.To(date, curr) : m_Accountant.From(date, curr);

            return new PlainText((res * val.Value).ToString("R"));
        }

        public bool IsExecutable(string expr) => expr.Initial() == "?e";
    }
}
