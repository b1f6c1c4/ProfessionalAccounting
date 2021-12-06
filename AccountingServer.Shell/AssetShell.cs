﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     资产表达式解释器
    /// </summary>
    internal class AssetShell : DistributedShell
    {
        public AssetShell(Accountant helper) : base(helper) { }

        /// <inheritdoc />
        protected override string Initial => "a";

        /// <inheritdoc />
        protected override IQueryResult ExecuteList(IQueryCompunded<IDistributedQueryAtom> distQuery, DateTime? dt,
            bool showSchedule, IEntitiesSerializer serializer)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(Accountant.SelectAssets(distQuery)))
                sb.Append(ListAsset(a, serializer, dt, showSchedule));

            return new PlainText(sb.ToString());
        }

        /// <inheritdoc />
        protected override IQueryResult ExecuteQuery(IQueryCompunded<IDistributedQueryAtom> distQuery,
            IEntitiesSerializer serializer)
            => new PlainText(serializer.PresentAssets(Sort(Accountant.SelectAssets(distQuery))));

        /// <inheritdoc />
        protected override IQueryResult ExecuteRegister(IQueryCompunded<IDistributedQueryAtom> distQuery,
            DateFilter rng,
            IQueryCompunded<IVoucherQueryAtom> query, IEntitiesSerializer serializer)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(Accountant.SelectAssets(distQuery)))
            {
                sb.Append(serializer.PresentVouchers(Accountant.RegisterVouchers(a, rng, query)));
                Accountant.Upsert(a);
            }

            if (sb.Length > 0)
                return new DirtyText(sb.ToString());

            return new PlainSucceed();
        }

        /// <inheritdoc />
        protected override IQueryResult ExecuteUnregister(IQueryCompunded<IDistributedQueryAtom> distQuery,
            DateFilter rng,
            IQueryCompunded<IVoucherQueryAtom> query, IEntitiesSerializer serializer)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(Accountant.SelectAssets(distQuery)))
            {
                foreach (var item in a.Schedule.Where(item => item.Date.Within(rng)))
                {
                    if (query != null)
                    {
                        if (item.VoucherID == null)
                            continue;

                        var voucher = Accountant.SelectVoucher(item.VoucherID);
                        if (voucher != null)
                            if (!MatchHelper.IsMatch(query, voucher.IsMatch))
                                continue;
                    }

                    item.VoucherID = null;
                }

                sb.Append(ListAsset(a, serializer));
                Accountant.Upsert(a);
            }

            if (sb.Length > 0)
                return new DirtyText(sb.ToString());

            return new PlainSucceed();
        }

        /// <inheritdoc />
        protected override IQueryResult ExecuteRecal(IQueryCompunded<IDistributedQueryAtom> distQuery,
            IEntitiesSerializer serializer)
        {
            var lst = new List<Asset>();
            foreach (var a in Sort(Accountant.SelectAssets(distQuery)))
            {
                Accountant.Depreciate(a);
                Accountant.Upsert(a);
                lst.Add(a);
            }

            return new DirtyText(serializer.PresentAssets(lst));
        }

        /// <inheritdoc />
        protected override IQueryResult ExecuteResetSoft(IQueryCompunded<IDistributedQueryAtom> distQuery,
            DateFilter rng)
        {
            var cnt = 0L;
            foreach (var a in Accountant.SelectAssets(distQuery))
            {
                if (a.Schedule == null)
                    continue;

                var flag = false;
                foreach (var item in a.Schedule.Where(item => item.Date.Within(rng))
                    .Where(item => item.VoucherID != null)
                    .Where(item => Accountant.SelectVoucher(item.VoucherID) == null))
                {
                    item.VoucherID = null;
                    cnt++;
                    flag = true;
                }

                if (flag)
                    Accountant.Upsert(a);
            }

            return new NumberAffected(cnt);
        }

        /// <inheritdoc />
        protected override IQueryResult ExcuteResetMixed(IQueryCompunded<IDistributedQueryAtom> distQuery,
            DateFilter rng)
        {
            var cnt = 0L;
            foreach (var a in Accountant.SelectAssets(distQuery))
            {
                if (a.Schedule == null)
                    continue;

                var flag = false;
                foreach (var item in a.Schedule.Where(item => item.Date.Within(rng))
                    .Where(item => item.VoucherID != null))
                {
                    var voucher = Accountant.SelectVoucher(item.VoucherID);
                    if (voucher == null)
                    {
                        item.VoucherID = null;
                        cnt++;
                        flag = true;
                    }
                    else if (Accountant.DeleteVoucher(voucher.ID))
                    {
                        item.VoucherID = null;
                        cnt++;
                        flag = true;
                    }
                }

                if (flag)
                    Accountant.Upsert(a);
            }

            return new NumberAffected(cnt);
        }

        protected override IQueryResult ExecuteResetHard(IQueryCompunded<IDistributedQueryAtom> distQuery,
            IQueryCompunded<IVoucherQueryAtom> query) => new NumberAffected(
            Accountant.SelectAssets(distQuery)
                .Sum(
                    a => Accountant.DeleteVouchers(
                        new IntersectQueries<IVoucherQueryAtom>(
                            query ?? VoucherQueryUnconstrained.Instance,
                            ParsingF.VoucherQuery(
                                $"{{ T{a.DepreciationTitle.AsTitle()} {a.StringID.Quotation('\'')} Depreciation }} + {{ T{a.DevaluationTitle.AsTitle()} {a.StringID.Quotation('\'')} Devalue }}")))));

        /// <inheritdoc />
        protected override IQueryResult ExecuteApply(IQueryCompunded<IDistributedQueryAtom> distQuery, DateFilter rng,
            bool isCollapsed)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(Accountant.SelectAssets(distQuery)))
            {
                foreach (var item in Accountant.Update(a, rng, isCollapsed))
                    sb.AppendLine(ListAssetItem(item));

                Accountant.Upsert(a);
            }

            if (sb.Length > 0)
                return new DirtyText(sb.ToString());

            return new PlainSucceed();
        }

        /// <summary>
        ///     执行检查表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="serializer">表示器</param>
        /// <returns>执行结果</returns>
        protected override IQueryResult ExecuteCheck(IQueryCompunded<IDistributedQueryAtom> distQuery, DateFilter rng,
            IEntitiesSerializer serializer)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(Accountant.SelectAssets(distQuery)))
            {
                var sbi = new StringBuilder();
                foreach (var item in Accountant.Update(a, rng, false, true))
                    sbi.AppendLine(ListAssetItem(item));

                if (sbi.Length != 0)
                {
                    sb.AppendLine(ListAsset(a, serializer, null, false));
                    sb.AppendLine(sbi.ToString());
                }

                Accountant.Upsert(a);
            }

            if (sb.Length > 0)
                return new DirtyText(sb.ToString());

            return new PlainSucceed();
        }

        /// <summary>
        ///     显示资产及其折旧计算表
        /// </summary>
        /// <param name="asset">资产</param>
        /// <param name="serializer">表示器</param>
        /// <param name="dt">计算账面价值的时间</param>
        /// <param name="showSchedule">是否显示折旧计算表</param>
        /// <returns>格式化的信息</returns>
        private string ListAsset(Asset asset, IEntitySerializer serializer, DateTime? dt = null,
            bool showSchedule = true)
        {
            var sb = new StringBuilder();

            var bookValue = Accountant.GetBookValueOn(asset, dt);
            if (dt.HasValue &&
                !bookValue?.IsZero() != true)
                return null;

            sb.AppendLine(
                $"{asset.StringID} {asset.Name.CPadRight(35)}{asset.Date:yyyyMMdd}" +
                asset.Value.AsCurrency(asset.Currency).CPadLeft(13) +
                (dt.HasValue ? bookValue.AsCurrency(asset.Currency).CPadLeft(13) : "-".CPadLeft(13)) +
                asset.Salvge.AsCurrency(asset.Currency).CPadLeft(13) +
                asset.Title.AsTitle().CPadLeft(5) +
                asset.DepreciationTitle.AsTitle().CPadLeft(5) +
                asset.DevaluationTitle.AsTitle().CPadLeft(5) +
                asset.DepreciationExpenseTitle.AsTitle().CPadLeft(5) +
                asset.DepreciationExpenseSubTitle.AsSubTitle() +
                asset.DevaluationExpenseTitle.AsTitle().CPadLeft(5) +
                asset.DevaluationExpenseSubTitle.AsSubTitle() +
                asset.Life.ToString().CPadLeft(4) +
                asset.Method.ToString().CPadLeft(20));

            if (showSchedule && asset.Schedule != null)
                foreach (var assetItem in asset.Schedule)
                {
                    sb.AppendLine(ListAssetItem(assetItem));
                    if (assetItem.VoucherID != null)
                        sb.AppendLine(serializer.PresentVoucher(Accountant.SelectVoucher(assetItem.VoucherID)).Wrap());
                }

            return sb.ToString();
        }

        /// <summary>
        ///     显示折旧计算表条目
        /// </summary>
        /// <param name="assetItem">折旧计算表条目</param>
        /// <returns>格式化的信息</returns>
        private static string ListAssetItem(IDistributedItem assetItem)
        {
            if (assetItem is AcquisationItem acq)
                return string.Format(
                    "   {0:yyyMMdd} ACQ:{1} ={3} ({2})",
                    assetItem.Date,
                    acq.OrigValue.AsCurrency().CPadLeft(13),
                    assetItem.VoucherID,
                    assetItem.Value.AsCurrency().CPadLeft(13));
            if (assetItem is DepreciateItem dep)
                return string.Format(
                    "   {0:yyyMMdd} DEP:{1} ={3} ({2})",
                    assetItem.Date,
                    dep.Amount.AsCurrency().CPadLeft(13),
                    assetItem.VoucherID,
                    assetItem.Value.AsCurrency().CPadLeft(13));
            if (assetItem is DevalueItem dev)
                return string.Format(
                    "   {0:yyyMMdd} DEV:{1} ={3} ({2})",
                    assetItem.Date,
                    dev.Amount.AsCurrency().CPadLeft(13),
                    assetItem.VoucherID,
                    assetItem.Value.AsCurrency().CPadLeft(13));
            if (assetItem is DispositionItem)
                return string.Format(
                    "   {0:yyyMMdd} DSP:{1} ={3} ({2})",
                    assetItem.Date,
                    "ALL".CPadLeft(13),
                    assetItem.VoucherID,
                    assetItem.Value.AsCurrency().CPadLeft(13));

            return null;
        }

        /// <summary>
        ///     对资产进行排序
        /// </summary>
        /// <param name="enumerable">资产</param>
        /// <returns>排序后的资产</returns>
        private static IEnumerable<Asset> Sort(IEnumerable<Asset> enumerable)
            => enumerable.OrderBy(a => a.Date, new DateComparer()).ThenBy(a => a.Name).ThenBy(a => a.ID);
    }
}
