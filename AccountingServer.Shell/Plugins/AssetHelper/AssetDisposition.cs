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
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Plugins.AssetHelper;

/// <summary>
///     处置资产
/// </summary>
internal class AssetDisposition : PluginBase
{
    /// <inheritdoc />
    public override async IAsyncEnumerable<string> Execute(string expr, Session session)
    {
        var voucherID = Parsing.Token(ref expr);
        var guids = new List<string>();
        var guidT = Guid.Empty;
        // ReSharper disable once AccessToModifiedClosure
        while (Parsing.Token(ref expr, true, s => Guid.TryParse(s, out guidT)) != null)
            guids.Add(guidT.ToString());
        Parsing.Eof(expr);

        var voucher = await session.Accountant.SelectVoucherAsync(voucherID);
        if (voucher == null)
            throw new ApplicationException("找不到记账凭证");

        foreach (var detail in voucher.Details.Where(static vd => vd.Title is 1601 or 1701)
                     .Where(detail => guids.Count == 0 || guids.Contains(detail.Content)))
        {
            var asset = await session.Accountant.SelectAssetAsync(Guid.Parse(detail.Content));
            foreach (var item in asset.Schedule)
            {
                if (item is DispositionItem)
                    throw new InvalidOperationException("已经处置");

                if (item.Date < voucher.Date)
                {
                    if (item.VoucherID == null)
                        throw new InvalidOperationException("尚未注册");
                }
                else
                {
                    if (item.VoucherID != null)
                        throw new InvalidOperationException("注册过多");
                }
            }

            var id = asset.Schedule.FindIndex(it => it.Date >= voucher.Date);
            if (id != -1)
                asset.Schedule.RemoveRange(id, asset.Schedule.Count - id);

            asset.Schedule.Add(new DispositionItem { Date = voucher.Date, VoucherID = voucher.ID });
            yield return session.Serializer.PresentAsset(asset).Wrap();
            await session.Accountant.UpsertAsync(asset);
        }
    }
}
