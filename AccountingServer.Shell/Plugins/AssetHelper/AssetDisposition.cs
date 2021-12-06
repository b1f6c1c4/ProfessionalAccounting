using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Plugins.AssetHelper
{
    /// <summary>
    ///     处置资产
    /// </summary>
    internal class AssetDisposition : PluginBase
    {
        public AssetDisposition(Accountant accountant) : base(accountant) { }

        /// <inheritdoc />
        public override IQueryResult Execute(string expr, IEntitiesSerializer serializer)
        {
            var voucherID = Parsing.Token(ref expr);
            var guids = new List<string>();
            var guidT = Guid.Empty;
            // ReSharper disable once AccessToModifiedClosure
            while (Parsing.Token(ref expr, true, s => Guid.TryParse(s, out guidT)) != null)
                guids.Add(guidT.ToString());
            Parsing.Eof(expr);

            var voucher = Accountant.SelectVoucher(voucherID);
            if (voucher == null)
                throw new ApplicationException("找不到记账凭证");

            var sb = new StringBuilder();
            foreach (var detail in voucher.Details.Where(vd => vd.Title == 1601 || vd.Title == 1701))
            {
                if (guids.Count != 0 &&
                    !guids.Contains(detail.Content))
                    continue;

                var asset = Accountant.SelectAsset(Guid.Parse(detail.Content));
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
                sb.Append(serializer.PresentAsset(asset).Wrap());
                Accountant.Upsert(asset);
            }

            if (sb.Length > 0)
                return new DirtyText(sb.ToString());

            return new PlainSucceed();
        }
    }
}
