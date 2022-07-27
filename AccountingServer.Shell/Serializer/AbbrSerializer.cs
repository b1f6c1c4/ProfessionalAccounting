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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Serialization;
using AccountingServer.BLL;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Serializer;

public class AbbrSerializer : ExprSerializer
{
    static AbbrSerializer()
        => Cfg.RegisterType<Abbreviations>("Abbr");

    protected override bool AlternativeTitle(ref string expr, ICollection<string> lst, ref ITitle title) =>
        GetAlternativeTitle(ref expr, lst, ref title);

    public static bool GetAlternativeTitle(ref string expr, ICollection<string> lst, ref ITitle title)
    {
        Abbreviation d = null;
        if (
            Parsing.Token(
                ref expr,
                false,
                t => (d = Cfg.Get<Abbreviations>().Abbrs.FirstOrDefault(a => a.Abbr == t)) != null) == null)
            return false;

        title = d;
        if (!d.Editable)
            lst.Add(d.Content);
        return true;
    }
}

[Serializable]
[XmlRoot("Abbrs")]
public class Abbreviations
{
    [XmlElement("Abbr")] public List<Abbreviation> Abbrs;
}

[Serializable]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class Abbreviation : ITitle
{
    [XmlAttribute("string")]
    public string Abbr { get; set; }

    [XmlAttribute("edit")]
    [DefaultValue(false)]
    public bool Editable { get; set; }

    [DefaultValue(null)]
    public string Content { get; set; }

    public int Title { get; set; }

    [DefaultValue(null)]
    public int? SubTitle { get; set; }
}
