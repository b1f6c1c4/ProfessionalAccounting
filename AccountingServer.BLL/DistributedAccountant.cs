/* Copyright (C) 2020-2025 b1f6c1c4
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
using System.Linq;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL;

/// <summary>
///     分期会计业务处理类
/// </summary>
internal abstract class DistributedAccountant
{
    protected readonly Client Client;

    /// <summary>
    ///     数据库访问
    /// </summary>
    protected readonly DbSession Db;

    protected DistributedAccountant(DbSession db, Client client)
    {
        Db = db;
        Client = client;
    }

    /// <summary>
    ///     获取分期的剩余
    /// </summary>
    /// <param name="dist">分期</param>
    /// <param name="dt">日期，若为<c>null</c>则返回总额</param>
    /// <returns>指定日期的总额，若早于日期则为<c>null</c></returns>
    public static double? GetBookValueOn(IDistributed dist, DateTime? dt)
    {
        if (!dt.HasValue ||
            dist.TheSchedule == null)
            return dist.Value;

        var last = dist.TheSchedule.LastOrDefault(item => DateHelper.CompareDate(item.Date, dt) <= 0);
        if (last != null)
            return last.Value;
        if (DateHelper.CompareDate(dist.Date, dt) <= 0)
            return dist.Value;

        return null;
    }

    /// <summary>
    ///     更新记账凭证的细目
    /// </summary>
    /// <param name="expected">应填细目</param>
    /// <param name="voucher">记账凭证</param>
    /// <param name="success">是否成功</param>
    /// <param name="modified">是否更改了记账凭证</param>
    /// <param name="editOnly">是否只允许更新</param>
    protected static void UpdateDetail(VoucherDetail expected, Voucher voucher,
        out bool success, out bool modified, bool editOnly = false)
    {
        success = false;
        modified = false;

        var fund = expected.Fund ?? throw new ArgumentException("应填细目的金额为null", nameof(expected));
        var user = expected.User;
        expected.User = null;
        expected.Fund = null;
        var isEliminated = fund.IsZero();

        var ds = voucher.Details.Where(d => d.IsMatch(expected)).ToList();

        expected.User = user;
        expected.Fund = fund;

        switch (ds.Count)
        {
            case 0 when isEliminated:
                success = true;
                return;
            case 0 when editOnly:
                return;
            case 0:
                voucher.Details.Add(expected);
                success = true;
                modified = true;
                return;
            case > 1:
                return;
        }

        if (isEliminated)
        {
            if (editOnly)
                return;

            voucher.Details.Remove(ds[0]);
            success = true;
            modified = true;
            return;
        }

        if ((ds[0].Fund!.Value - fund).IsZero())
        {
            success = true;
            return;
        }

        ds[0].Fund = fund;
        modified = true;
    }
}
