/* Copyright (C) 2020-2023 b1f6c1c4
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

namespace AccountingServer.Shell.Plugins.Statement;

[Serializable]
[XmlRoot("Targets")]
public class StmtTargets
{
    [XmlElement("Target")] public List<Target> Targets;
}

[Serializable]
public class Target
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlAttribute("reversed")]
    public bool Reversed { get; set; }

    [XmlAttribute("query")]
    public string Query { get; set; }

    [XmlElement("Length")]
    public List<LengthSpec> Lengths { get; set; }

    [XmlElement("Overlap")]
    public List<OverlapSpec> Overlaps { get; set; }

    [XmlElement("Skip")]
    public List<SkipSpec> Skips { get; set; }
}

public interface ISpec { }

[Serializable]
[XmlType(TypeName="Length")]
public class LengthSpec : ISpec
{
    [XmlAttribute("period")]
    public string Period { get; set; }

    [XmlText]
    public int Tolerance { get; set; }
}

[Serializable]
[XmlType(TypeName="Overlap")]
public class OverlapSpec : ISpec
{
    [XmlAttribute("from")]
    public string From { get; set; }

    [XmlAttribute("to")]
    public string To { get; set; }

    [XmlText]
    public int Tolerance { get; set; }
}

[Serializable]
[XmlType(TypeName="Skip")]
public class SkipSpec : ISpec
{
    [XmlText]
    public string ID { get; set; }
}
