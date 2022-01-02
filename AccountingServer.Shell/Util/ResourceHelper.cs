/* Copyright (C) 2020-2022 b1f6c1c4
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
using System.IO;
using System.Resources;

namespace AccountingServer.Shell.Util;

public static class ResourceHelper
{
    public static async IAsyncEnumerable<string> ReadResource(string resName, Type type)
    {
        await using var stream = type.Assembly.GetManifestResourceStream(resName);
        if (stream == null)
            throw new MissingManifestResourceException();

        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
            yield return await reader.ReadLineAsync() + "\n";
    }
}
