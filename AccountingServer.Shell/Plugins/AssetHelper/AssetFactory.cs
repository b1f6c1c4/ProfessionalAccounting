using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Plugins.AssetHelper
{
    /// <summary>
    ///     创建资产
    /// </summary>
    internal class AssetFactory : PluginBase
    {
        public AssetFactory(Accountant accountant, IEntitySerializer serializer) : base(
            accountant,
            serializer) { }

        /// <inheritdoc />
        public override IQueryResult Execute(string expr)
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

            var voucher = Accountant.SelectVoucher(voucherID);
            if (voucher == null)
                throw new ApplicationException("找不到记账凭证");

            var detail = voucher.Details.Single(
                vd => (vd.Title == 1601 || vd.Title == 1701) &&
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
                    Currency = detail.Currency,
                    Value = detail.Fund,
                    Salvge = life == 0 ? detail.Fund : (isFixed ? detail.Fund * 0.05 : 0),
                    Life = life,
                    Title = detail.Title,
                    Method = life == 0 ? DepreciationMethod.None : DepreciationMethod.StraightLine,
                    DepreciationTitle = isFixed ? 1602 : 1702,
                    DepreciationExpenseTitle = 6602,
                    DepreciationExpenseSubTitle = isFixed ? 7 : 11,
                    DevaluationTitle = isFixed ? 1603 : 1703,
                    DevaluationExpenseTitle = 6701,
                    DevaluationExpenseSubTitle = isFixed ? 5 : 6,
                    Schedule = new List<AssetItem>
                        {
                            new AcquisationItem
                                {
                                    Date = voucher.Date,
                                    VoucherID = voucher.ID,
                                    // ReSharper disable once PossibleInvalidOperationException
                                    OrigValue = detail.Fund.Value
                                }
                        }
                };

            Accountant.Upsert(asset);
            // ReSharper disable once PossibleInvalidOperationException
            asset = Accountant.SelectAsset(asset.ID.Value);
            Accountant.Depreciate(asset);
            Accountant.Upsert(asset);

            return new EditableText(Serializer.PresentAsset(asset));
        }
    }
}
