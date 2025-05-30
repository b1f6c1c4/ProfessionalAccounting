/* Copyright (C) 2020-2025 b1f6c1c4
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
using System.IO;

namespace AccountingServer.DAL;

public static class Facade
{
    public static IDbAdapter Create(string uri = null, string db = null, string x509 = null)
    {
        uri ??= Environment.GetEnvironmentVariable("MONGO_URI") ?? "mongodb://localhost";
        x509 ??= Environment.GetEnvironmentVariable("MONGO_CERT");

        if (uri.StartsWith("/", StringComparison.Ordinal))
            uri = File.ReadAllText(uri).Trim();
        if (uri.StartsWith("mongodb://", StringComparison.Ordinal) ||
            uri.StartsWith("mongodb+srv://", StringComparison.Ordinal))
            return new MongoDbAdapter(uri, db, x509);

        throw new NotSupportedException("Uri无效");
    }
}
