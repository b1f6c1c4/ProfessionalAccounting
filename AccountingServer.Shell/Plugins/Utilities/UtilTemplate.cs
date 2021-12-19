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
using System.Xml.Serialization;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Plugins.Utilities;

[Serializable]
[XmlRoot("Templates")]
public class UtilTemplates
{
    [XmlElement("Template")] public List<UtilTemplate> Templates;
}

[Serializable]
public enum UtilTemplateType
{
    [XmlEnum(Name = "fixed")] Fixed,
    [XmlEnum(Name = "value")] Value,
    [XmlEnum(Name = "fill")] Fill,
}

[Serializable]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class UtilTemplate : Voucher
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlAttribute("desc")]
    public string Description { get; set; }

    [XmlAttribute("type")]
    public UtilTemplateType TemplateType { get; set; }

    [XmlAttribute("query")]
    public string Query { get; set; }

    [XmlAttribute("default")]
    public string DefaultString { get => Default?.ToString("R"); set => Default = Convert.ToDouble(value); }

    [XmlIgnore]
    public double? Default { get; set; }
}