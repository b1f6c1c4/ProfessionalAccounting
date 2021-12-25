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
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Carry;

/// <summary>
///     结转/所有者权益币种转换表达式解释器
/// </summary>
internal partial class CarryShell : IShellComponent
{
    /// <summary>
    ///     基本会计业务处理类
    /// </summary>
    private readonly Accountant m_Accountant;

    public CarryShell(Accountant helper) => m_Accountant = helper;

    /// <inheritdoc />
    public IQueryResult Execute(string expr, IEntitiesSerializer serializer)
    {
        expr = expr.Rest();
        DateFilter rng;
        switch (expr.Initial())
        {
            case "lst":
                expr = expr.Rest();
                rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
                Parsing.Eof(expr);
                return ListHistory(rng);
            case "rst":
                expr = expr.Rest();
                rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
                Parsing.Eof(expr);
                return PerformAction(rng, true);
            default:
                rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
                Parsing.Eof(expr);
                return PerformAction(AutomaticRange(rng), false);
        }
    }

    private DateFilter AutomaticRange(DateFilter rng)
    {
        rng ??= DateFilter.Unconstrained;
        if (rng.NullOnly)
            return rng;

        rng.EndDate = rng.EndDate.HasValue
            ? new DateTime(rng.EndDate!.Value.Year, rng.EndDate!.Value.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            : new DateTime(ClientDateTime.Today.Year, ClientDateTime.Today.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMonths(-1);
        rng.EndDate = rng.EndDate!.Value.AddMonths(1).AddDays(-1);
        if (!rng.StartDate.HasValue)
        {
            var st = m_Accountant.RunVoucherGroupedQuery("[~null] !!y").Items.Cast<ISubtotalDate>()
                .OrderBy(grpd => grpd.Date).FirstOrDefault()?.Date;
            if (st.HasValue)
                rng.StartDate = st;
            else
                rng = DateFilter.TheNullOnly;
        }
        else
            rng.StartDate
                = new DateTime(rng.StartDate!.Value.Year, rng.StartDate!.Value.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return rng;
    }

    private IQueryResult PerformAction(DateFilter rng, bool isRst)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"=== rm -rf Carry {rng.AsDateRange()} ===> {ResetCarry(rng)} removed");
        sb.AppendLine($"=== rm -rf CarryYear {rng.AsDateRange()} ===> {ResetCarryYear(rng)} removed");
        sb.AppendLine($"=== rm -rf Conversion {rng.AsDateRange()} ===> {ResetConversion(rng)} removed");
        if (isRst)
            return new DirtyText(sb.ToString());

        var lst = new List<Voucher>();
        var cnt = 0L;

        void Exec()
        {
            m_Accountant.Upsert(lst);
            cnt += lst.Count;
            lst.Clear();
        }

        if (rng.NullOnly || rng.Nullable)
        {
            lst.AddRange(Carry(sb, null));
            Exec();
            lst.AddRange(CarryYear(sb, null));
        }

        if (!rng.NullOnly)
            for (var dt = rng.StartDate!.Value; dt <= rng.EndDate!.Value; dt = dt.AddMonths(1))
            {
                foreach (var info in BaseCurrency.History)
                    if (info.Date >= dt && info.Date < dt.AddMonths(1))
                    {
                        Exec();
                        lst.AddRange(ConvertEquity(sb, info.Date!.Value, info.Currency));
                    }

                lst.AddRange(Carry(sb, dt));
                if (dt.Month == 12)
                {
                    Exec();
                    lst.AddRange(CarryYear(sb, dt));
                }
            }

        Exec();
        sb.AppendLine($"=== Total vouchers: {cnt}");

        return new DirtyText(sb.ToString());
    }

    /// <inheritdoc />
    public bool IsExecutable(string expr) => expr.Initial() == "ca";
}
