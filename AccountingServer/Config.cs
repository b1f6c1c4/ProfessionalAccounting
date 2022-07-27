/* Copyright (C) 2022 b1f6c1c4
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
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AccountingServer.Entities.Util;

public class ConfigFilesManager
{
    public static async IAsyncEnumerable<string> InitializeConfigFiles()
    {
        var names = new[] {
            "BaseCurrency",
            "Symbol",
            "Exchange",
            "Titles",
            "Carry",
            "Cash",
            "Composite",
            "Coupling",
            "Util",
            "Abbr",
        };
        await Task.WhenAll(names.Select(Cfg.RegisterName));
        await foreach (var s in Cfg.ReloadAll())
            yield return s;
    }
}
