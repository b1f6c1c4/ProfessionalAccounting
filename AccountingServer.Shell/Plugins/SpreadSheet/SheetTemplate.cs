/* Copyright (C) 2023 b1f6c1c4
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
using System.Xml.Serialization;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Plugins.SpreadSheet;

[Serializable]
[XmlRoot("Templates")]
public class SheetTemplates
{
    [XmlElement("Template")] public List<SheetTemplate> Templates;
}

[Serializable]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class SheetTemplate
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlAttribute("desc")]
    public string Description { get; set; }

    [XmlElement("Column")]
    public List<SheetColumn> Columns { get; set; }
}

[Serializable]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class SheetColumn
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlAttribute("complex")]
    public bool IsComplex { get; set; }

    [XmlText]
    public string Query { get; set; }
}
