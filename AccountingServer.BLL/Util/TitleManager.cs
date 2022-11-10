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
using System.Linq;
using System.Xml.Serialization;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL.Util;

[Serializable]
[XmlRoot("Titles")]
public class TitleInfos
{
    [XmlElement("title")] public List<TitleInfo> Titles;
}

[Serializable]
public class TitleInfo
{
    [XmlElement("subTitle")] public List<SubTitleInfo> SubTitles;

    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlAttribute("virtual")]
    [DefaultValue(false)]
    public bool IsVirtual { get; set; }

    /// <summary>
    ///     方向：
    ///     <c>0</c>表示任意
    ///     <c>1</c>表示均在借方
    ///     <c>2</c>表示汇总在借方
    ///     <c>-1</c>表示均在贷方
    ///     <c>-2</c>表示汇总在贷方
    /// </summary>
    [XmlAttribute("dir")]
    [DefaultValue(0)]
    public int Direction { get; set; }
}

public class SubTitleInfo
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlAttribute("id")]
    public int Id { get; set; }

    /// <summary>
    ///     方向：
    ///     <c>0</c>表示任意
    ///     <c>1</c>表示均在借方
    ///     <c>2</c>表示汇总在借方
    ///     <c>-1</c>表示均在贷方
    ///     <c>-2</c>表示汇总在贷方
    /// </summary>
    [XmlAttribute("dir")]
    [DefaultValue(0)]
    public int Direction { get; set; }
}

/// <summary>
///     会计科目管理
/// </summary>
public static class TitleManager
{
    /// <summary>
    ///     会计科目信息文档
    /// </summary>
    static TitleManager()
        => Cfg.RegisterType<TitleInfos>("Titles");

    /// <summary>
    ///     返回所有会计科目编号和名称
    /// </summary>
    /// <returns>编号和科目名称</returns>
    public static IReadOnlyList<TitleInfo> Titles => Cfg.Get<TitleInfos>().Titles.AsReadOnly();

    /// <summary>
    ///     返回编号对应的会计科目名称
    /// </summary>
    /// <param name="title">一级科目编号</param>
    /// <param name="subtitle">二级科目编号</param>
    /// <returns>名称</returns>
    public static string GetTitleName(int? title, int? subtitle = null)
    {
        if (!title.HasValue)
            return null;

        var t0 = Titles.FirstOrDefault(t => t.Id == title.Value);
        return !subtitle.HasValue ? t0?.Name : t0?.SubTitles?.FirstOrDefault(t => t.Id == subtitle.Value)?.Name;
    }

    /// <summary>
    ///     返回细目对应的会计科目名称
    /// </summary>
    /// <param name="detail">细目</param>
    /// <returns>名称</returns>
    public static string GetTitleName(VoucherDetail detail) =>
        detail.SubTitle.HasValue
            ? $"{GetTitleName(detail.Title)}-{GetTitleName(detail.Title, detail.SubTitle)}"
            : GetTitleName(detail.Title);
}
