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

using System.Collections.Generic;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Subtotal;

/// <summary>
///     报告结果处理器
/// </summary>
internal class PreciseSubtotalPre : StringSubtotalVisitor
{
    /// <summary>
    ///     是否包含分类汇总
    /// </summary>
    private readonly bool m_WithSubtotal;

    private string m_Path = "";
    private int? m_Title;

    public PreciseSubtotalPre(bool withSubtotal = true) => m_WithSubtotal = withSubtotal;

    /// <summary>
    ///     使用分隔符连接字符串
    /// </summary>
    /// <param name="path">原字符串</param>
    /// <param name="token">要连接上的字符串</param>
    /// <param name="interval">分隔符</param>
    /// <returns>新字符串</returns>
    private static string Merge(string path, string token, string interval = "-")
    {
        if (path.Length == 0)
            return token;

        return path + interval + token;
    }

    private async IAsyncEnumerable<string> ShowSubtotal(ISubtotalResult sub)
    {
        if (m_WithSubtotal || sub.Items == null)
            yield return $"{m_Path}\t{sub.Fund:R}\n";
        await foreach (var s in VisitChildren(sub))
            yield return s;
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalRoot sub)
        => ShowSubtotal(sub);

    public override async IAsyncEnumerable<string> Visit(ISubtotalDate sub)
    {
        var prev = m_Path;
        m_Path = Merge(m_Path, sub.Date.AsDate(sub.Level));
        await foreach (var s in ShowSubtotal(sub))
            yield return s;
        m_Path = prev;
    }

    public override async IAsyncEnumerable<string> Visit(ISubtotalUser sub)
    {
        var prev = m_Path;
        m_Path = Merge(m_Path, sub.User.AsUser());
        await foreach (var s in ShowSubtotal(sub))
            yield return s;
        m_Path = prev;
    }

    public override async IAsyncEnumerable<string> Visit(ISubtotalCurrency sub)
    {
        var prev = m_Path;
        m_Path = Merge(m_Path, $"@{sub.Currency}");
        await foreach (var s in ShowSubtotal(sub))
            yield return s;
        m_Path = prev;
    }

    public override async IAsyncEnumerable<string> Visit(ISubtotalTitle sub)
    {
        var prev = m_Path;
        m_Path = Merge(m_Path, $"{sub.Title.AsTitle()}:{TitleManager.GetTitleName(sub.Title)}");
        m_Title = sub.Title;
        await foreach (var s in ShowSubtotal(sub))
            yield return s;
        m_Path = prev;
    }

    public override async IAsyncEnumerable<string> Visit(ISubtotalSubTitle sub)
    {
        var prev = m_Path;
        m_Path = Merge(m_Path, $"{sub.SubTitle.AsSubTitle()}:{TitleManager.GetTitleName(m_Title, sub.SubTitle)}");
        await foreach (var s in ShowSubtotal(sub))
            yield return s;
        m_Path = prev;
    }

    public override async IAsyncEnumerable<string> Visit(ISubtotalContent sub)
    {
        var prev = m_Path;
        m_Path = Merge(m_Path, sub.Content);
        await foreach (var s in ShowSubtotal(sub))
            yield return s;
        m_Path = prev;
    }

    public override async IAsyncEnumerable<string> Visit(ISubtotalRemark sub)
    {
        var prev = m_Path;
        m_Path = Merge(m_Path, sub.Remark);
        await foreach (var s in ShowSubtotal(sub))
            yield return s;
        m_Path = prev;
    }
}
