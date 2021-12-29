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
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AccountingServer.BLL.Util;
using AccountingServer.Entities.Util;

namespace AccountingServer.Test;

public class MockConfigManager<T> : IConfigManager<T>
{
    public MockConfigManager(T config) => Config = config;
    public T Config { get; }

    public void Reload(bool throws) => throw new NotImplementedException();
}

public class MockExchange : IHistoricalExchange, IEnumerable
{
    private readonly Dictionary<Tuple<DateTime?, string>, double> m_Dic = new();

    public IEnumerator GetEnumerator() => m_Dic.GetEnumerator();

    public ValueTask<double> Query(DateTime? date, string from, string to)
        => ValueTask.FromResult(m_Dic[new(date, from)] / m_Dic[new(date, to)]);

    public void Add(DateTime date, string target, double val) => m_Dic.Add(
        new(date, target),
        val);
}
