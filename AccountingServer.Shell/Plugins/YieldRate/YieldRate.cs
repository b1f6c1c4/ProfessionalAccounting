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

using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL.Parsing;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Util;

namespace AccountingServer.Shell.Plugins.YieldRate;

/// <summary>
///     实际收益率计算
/// </summary>
internal class YieldRate : PluginBase
{
    /// <inheritdoc />
    public override async IAsyncEnumerable<string> Execute(string expr, Session session)
    {
        FacadeF.ParsingF.Eof(expr);

        var result = await session.Accountant.RunGroupedQueryAsync("T1101+T611102+T1501+T611106 G``cd");
        var resx = await session.Accountant.RunGroupedQueryAsync("T1101+T1501``c");
        foreach (
            var (grp, rte) in
            result.Items.Cast<ISubtotalContent>()
                .Join(
                    resx.Items.Cast<ISubtotalContent>(),
                    grp => grp.Content,
                    rsx => rsx.Content,
                    (grp, bal) => (Group: grp,
                        Rate: GetRate(session,
                            grp.Items.Cast<ISubtotalDate>().OrderBy(b => b.Date, new DateComparer()).ToList(),
                            bal.Fund)))
                .OrderByDescending(kvp => kvp.Rate))
            yield return $"{grp.Content.CPadRight(30)} {$"{rte * 360:P2}",7}";
    }

    /// <summary>
    ///     计算实际收益率
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="lst">现金流</param>
    /// <param name="pv">现值</param>
    /// <returns>实际收益率</returns>
    private double GetRate(Session session, IReadOnlyList<ISubtotalDate> lst, double pv)
    {
        if (!pv.IsZero())
            return
                new YieldRateSolver(
                    lst.Select(b => session.Client.Today.Subtract(b.Date!.Value).TotalDays).Concat(new[] { 0D }),
                    lst.Select(b => b.Fund).Concat(new[] { -pv })).Solve();

        return
            new YieldRateSolver(
                lst.Select(b => lst[^1].Date!.Value.Subtract(b.Date!.Value).TotalDays),
                lst.Select(b => b.Fund)).Solve();
    }
}
