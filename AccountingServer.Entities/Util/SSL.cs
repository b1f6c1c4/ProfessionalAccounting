/* Copyright (C) 2025 b1f6c1c4
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
using System.Xml.Serialization;

namespace AccountingServer.Entities.Util;

[Serializable]
public class IdPEntry
{
    [XmlElement("Subject")]
    public string Subject { get; set; }

    [XmlElement("CN")]
    public string CN { get; set; }

    [XmlElement("Issuer")]
    public string Issuer { get; set; }

    [XmlElement("Serial")]
    public string Serial { get; set; }

    [XmlElement("Fingerprint")]
    public string Fingerprint { get; set; }
}
