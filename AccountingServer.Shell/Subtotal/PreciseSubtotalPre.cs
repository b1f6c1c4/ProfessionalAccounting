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

using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

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

    private void ShowSubtotal(ISubtotalResult sub)
    {
        if (m_WithSubtotal || sub.Items == null)
            Sb.AppendLine($"{m_Path}\t{sub.Fund:R}");
        VisitChildren(sub);
    }

    public override Nothing Visit(ISubtotalRoot sub)
    {
        ShowSubtotal(sub);
        return Nothing.AtAll;
    }

    public override Nothing Visit(ISubtotalDate sub)
    {
        var prev = m_Path;
        m_Path = Merge(m_Path, sub.Date.AsDate(sub.Level));
        ShowSubtotal(sub);
        m_Path = prev;
        return Nothing.AtAll;
    }

    public override Nothing Visit(ISubtotalUser sub)
    {
        var prev = m_Path;
        m_Path = Merge(m_Path, $"U{sub.User.AsUser()}");
        ShowSubtotal(sub);
        m_Path = prev;
        return Nothing.AtAll;
    }

    public override Nothing Visit(ISubtotalCurrency sub)
    {
        var prev = m_Path;
        m_Path = Merge(m_Path, $"@{sub.Currency}");
        ShowSubtotal(sub);
        m_Path = prev;
        return Nothing.AtAll;
    }

    public override Nothing Visit(ISubtotalTitle sub)
    {
        var prev = m_Path;
        m_Path = Merge(m_Path, $"{sub.Title.AsTitle()}:{TitleManager.GetTitleName(sub.Title)}");
        m_Title = sub.Title;
        ShowSubtotal(sub);
        m_Path = prev;
        return Nothing.AtAll;
    }

    public override Nothing Visit(ISubtotalSubTitle sub)
    {
        var prev = m_Path;
        m_Path = Merge(m_Path, $"{sub.SubTitle.AsSubTitle()}:{TitleManager.GetTitleName(m_Title, sub.SubTitle)}");
        ShowSubtotal(sub);
        m_Path = prev;
        return Nothing.AtAll;
    }

    public override Nothing Visit(ISubtotalContent sub)
    {
        var prev = m_Path;
        m_Path = Merge(m_Path, sub.Content);
        ShowSubtotal(sub);
        m_Path = prev;
        return Nothing.AtAll;
    }

    public override Nothing Visit(ISubtotalRemark sub)
    {
        var prev = m_Path;
        m_Path = Merge(m_Path, sub.Remark);
        ShowSubtotal(sub);
        m_Path = prev;
        return Nothing.AtAll;
    }
}
