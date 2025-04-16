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
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Plugins.AssetHelper;

/// <summary>
///     创建资产
/// </summary>
internal class AssetFactory : PluginBase
{
    /// <inheritdoc />
    public override async IAsyncEnumerable<string> Execute(string expr, Session session)
    {
        var voucherID = Parsing.Token(ref expr);
        Guid? guid = null;
        var guidT = Guid.Empty;
        // ReSharper disable once AccessToModifiedClosure
        if (Parsing.Token(ref expr, true, s => Guid.TryParse(s, out guidT)) != null)
            guid = guidT;
        var name = Parsing.Token(ref expr);
        var lifeT = Parsing.Double(ref expr);
        Parsing.Eof(expr);

        var voucher = await session.Accountant.SelectVoucherAsync(voucherID);
        if (voucher == null)
            throw new ApplicationException("找不到记账凭证");

        var detail = voucher.Details.Single(
            vd => vd.Title is 1601 or 1701 &&
                (!guid.HasValue || string.Equals(
                    vd.Content,
                    guid.ToString(),
                    StringComparison.OrdinalIgnoreCase)));

        var isFixed = detail.Title == 1601;
        var life = (int?)lifeT ?? (isFixed ? 3 : 0);

        var asset = new Asset
            {
                StringID = detail.Content,
                Name = name,
                Date = voucher.Date,
                User = detail.User,
                Currency = detail.Currency,
                Value = detail.Fund,
                Salvage = life == 0 ? detail.Fund : isFixed ? detail.Fund * 0.05 : 0,
                Life = life,
                Title = detail.Title,
                Method = life == 0 ? DepreciationMethod.None : DepreciationMethod.StraightLine,
                DepreciationTitle = isFixed ? 1602 : 1702,
                DepreciationExpenseTitle = 6602,
                DepreciationExpenseSubTitle = isFixed ? 7 : 11,
                DevaluationTitle = isFixed ? 1603 : 1703,
                DevaluationExpenseTitle = 6701,
                DevaluationExpenseSubTitle = isFixed ? 5 : 6,
                Schedule = new()
                    {
                        new AcquisitionItem
                            {
                                Date = voucher.Date, VoucherID = voucher.ID, OrigValue = detail.Fund!.Value,
                            },
                    },
            };

        await session.Accountant.UpsertAsync(asset);
        asset = await session.Accountant.SelectAssetAsync(asset.ID!.Value);
        Accountant.Depreciate(asset);
        await session.Accountant.UpsertAsync(asset);

        yield return session.Serializer.PresentAsset(asset).Wrap();
    }
}
