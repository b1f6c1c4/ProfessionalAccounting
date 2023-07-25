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

        var result = await session.Accountant.RunGroupedQueryAsync("Investment-T101204 G``Ccd");
        var dc = new DateComparer();
        var lst = new List<Investment>();
        foreach (var grpC in result.Items.Cast<ISubtotalCurrency>())
        foreach (var grpc in grpC.Items.Cast<ISubtotalContent>())
        {
            var pv = await session.Accountant.RunGroupedQueryAsync(
                $"@{grpC.Currency} Investment {grpc.Content.Quotation('\'')} * Asset``v");
            var ti = await session.Accountant.RunGroupedQueryAsync(
                $"@{grpC.Currency} Investment {grpc.Content.Quotation('\'')} * Asset T000001 > ``v");
            var days = grpc.Items.Cast<ISubtotalDate>().OrderBy(static b => b.Date, dc).ToList();
            var daily = GetRate(session, days, pv.Fund);
            lst.Add(new(
                grpC.Currency, grpc.Content,
                days.First().Date!.Value, days.Last().Date!.Value, 0,
                ti.Fund, pv.Fund - grpc.Fund, pv.Fund,
                (pv.Fund - grpc.Fund) / ti.Fund, Math.Pow(1 + daily, 365.2425) - 1
            ));
        }

        yield return $"{"".CPadRight(30)} Start   ~ EndDate  TotalInvest      NetGain    PresentValue YieldRate  APY\n";
        foreach (var inv in lst.OrderBy(static inv => -inv.Apy))
            yield return $"{inv.Content.CPadRight(30)} {inv.StartDate.AsDate()}~{inv.EndDate.AsDate()} "
                + $"{inv.TotalInvest.AsCurrency(inv.Currency).CPadLeft(13)} {inv.NetGain.AsCurrency(inv.Currency).CPadLeft(13)} "
                + $"{inv.PresentValue.AsCurrency(inv.Currency).CPadLeft(13)} "
                + $"{inv.YieldRate:P5}   {inv.Apy:P5}\n";
    }

    /// <summary>
    ///     计算实际收益率
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="lst">现金流</param>
    /// <param name="pv">现值</param>
    /// <returns>实际收益率</returns>
    private static double GetRate(Session session, IReadOnlyList<ISubtotalDate> lst, double pv)
    {
        if (!pv.IsZero())
            return
                new YieldRateSolver(
                    lst.Select(b => session.Client.Today.Subtract(b.Date!.Value).TotalDays).Concat(new[] { 0D }),
                    lst.Select(static b => b.Fund).Concat(new[] { -pv })).Solve();

        return
            new YieldRateSolver(
                lst.Select(b => lst[^1].Date!.Value.Subtract(b.Date!.Value).TotalDays),
                lst.Select(static b => b.Fund)).Solve();
    }

    private record Investment(string Currency, string Content,
        DateTime StartDate, DateTime EndDate, int Days,
        double TotalInvest, double NetGain, double PresentValue, double YieldRate, double Apy);
}
