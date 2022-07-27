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
using System.Linq;
using System.Xml.Serialization;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL.Util;

[Serializable]
[XmlRoot("BaseCurrencies")]
public class BaseCurrencyInfos
{
    [XmlElement("BaseCurrency")] public List<BaseCurrencyInfo> Infos;
}

[Serializable]
public class BaseCurrencyInfo
{
    [XmlElement]
    public DateTime? Date { get; set; }

    [XmlElement]
    public string Currency { get; set; }
}

/// <summary>
///     记账本位币管理
/// </summary>
public static class BaseCurrency
{
    /// <summary>
    ///     记账本位币信息文档
    /// </summary>
    public static IConfigManager<BaseCurrencyInfos> BaseCurrencyInfos { private get; set; } =
        MetaConfigManager.Generate<BaseCurrencyInfos>("BaseCurrency");

    public static IReadOnlyList<BaseCurrencyInfo> History => BaseCurrencyInfos.Config.Infos.AsReadOnly();

    public static string Now
        => BaseCurrencyInfos.Config.Infos.Last().Currency;

    public static string At(DateTime? dt)
        => BaseCurrencyInfos.Config.Infos.Last(bc => DateHelper.CompareDate(bc.Date, dt) <= 0).Currency;
}
