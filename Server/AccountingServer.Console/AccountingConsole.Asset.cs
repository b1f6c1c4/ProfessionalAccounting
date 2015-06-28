using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class AccountingConsole
    {
        /// <summary>
        ///     更新或添加资产
        /// </summary>
        /// <param name="code">资产的C#代码</param>
        /// <returns>新资产的C#代码</returns>
        public string ExecuteAssetUpsert(string code)
        {
            var asset = CSharpHelper.ParseAsset(code);

            if (!m_Accountant.Upsert(asset))
                throw new ApplicationException("更新或添加失败");

            return CSharpHelper.PresentAsset(asset);
        }

        /// <summary>
        ///     删除资产
        /// </summary>
        /// <param name="code">资产的C#代码</param>
        /// <returns>是否成功</returns>
        public bool ExecuteAssetRemoval(string code)
        {
            var asset = CSharpHelper.ParseAsset(code);

            if (!asset.ID.HasValue)
                throw new ApplicationException("编号未知");

            return m_Accountant.DeleteAsset(asset.ID.Value);
        }

        /// <summary>
        ///     执行资产表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteAsset(ConsoleParser.AssetContext expr)
        {
            var assetListContext = expr.assetList();
            if (assetListContext != null)
            {
                var dt = assetListContext.AOAll() != null
                             ? null
                             : assetListContext.rangePoint() != null
                                   ? assetListContext.rangePoint().Range.EndDate
                                   : DateTime.Now.Date;

                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAssets(assetListContext.distributedQ())))
                    sb.Append(ListAsset(a, dt, assetListContext.AOList() != null));

                if (assetListContext.AOList() != null)
                    return new EditableText(sb.ToString());
                return new UnEditableText(sb.ToString());
            }
            var assetQueryContext = expr.assetQuery();
            if (assetQueryContext != null)
            {
                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAssets(assetQueryContext.distributedQ())))
                    sb.Append(CSharpHelper.PresentAsset(a));

                return new EditableText(sb.ToString());
            }
            var assetRegisterContext = expr.assetRegister();
            if (assetRegisterContext != null)
            {
                var rng = assetRegisterContext.range() != null
                              ? assetRegisterContext.range().Range
                              : DateFilter.Unconstrained;
                var query = assetRegisterContext.vouchers();

                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAssets(assetRegisterContext.distributedQ())))
                {
                    foreach (var voucher in m_Accountant.RegisterVouchers(a, rng, query))
                        sb.Append(CSharpHelper.PresentVoucher(voucher));

                    m_Accountant.Upsert(a);
                }
                if (sb.Length > 0)
                    return new EditableText(sb.ToString());
                return new Suceed();
            }
            var assetUnregisterContext = expr.assetUnregister();
            if (assetUnregisterContext != null)
            {
                var rng = assetUnregisterContext.range() != null
                              ? assetUnregisterContext.range().Range
                              : DateFilter.Unconstrained;
                var query = assetRegisterContext.vouchers();

                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAssets(assetUnregisterContext.distributedQ())))
                {
                    foreach (var item in a.Schedule.Where(item => item.Date.Within(rng)))
                    {
                        if (query != null)
                        {
                            if (item.VoucherID == null)
                                continue;

                            var voucher = m_Accountant.SelectVoucher(item.VoucherID);
                            if (voucher != null)
                                if (!MatchHelper.IsMatch(query, voucher.IsMatch))
                                    continue;
                        }
                        item.VoucherID = null;
                    }

                    sb.Append(ListAsset(a));
                    m_Accountant.Upsert(a);
                }
                return new EditableText(sb.ToString());
            }
            var assetRedepContext = expr.assetRedep();
            if (assetRedepContext != null)
            {
                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAssets(assetRedepContext.distributedQ())))
                {
                    Accountant.Depreciate(a);
                    sb.Append(CSharpHelper.PresentAsset(a));
                    m_Accountant.Upsert(a);
                }
                return new EditableText(sb.ToString());
            }
            var assetResetSoftContext = expr.assetResetSoft();
            if (assetResetSoftContext != null)
            {
                var rng = assetResetSoftContext.range() != null
                              ? assetResetSoftContext.range().Range
                              : DateFilter.Unconstrained;

                var cnt = 0L;
                foreach (var a in m_Accountant.SelectAssets(assetResetSoftContext.distributedQ()))
                {
                    if (a.Schedule == null)
                        continue;
                    var flag = false;
                    foreach (var item in a.Schedule.Where(item => item.Date.Within(rng))
                                          .Where(item => item.VoucherID != null)
                                          .Where(item => m_Accountant.SelectVoucher(item.VoucherID) == null))
                    {
                        item.VoucherID = null;
                        cnt++;
                        flag = true;
                    }
                    if (flag)
                        m_Accountant.Upsert(a);
                }
                return new NumberAffected(cnt);
            }
            var assetResetMixedContext = expr.assetResetMixed();
            if (assetResetMixedContext != null)
            {
                var rng = assetResetMixedContext.range() != null
                              ? assetResetMixedContext.range().Range
                              : DateFilter.Unconstrained;

                var cnt = 0L;
                foreach (var a in m_Accountant.SelectAssets(assetResetMixedContext.distributedQ()))
                {
                    if (a.Schedule == null)
                        continue;
                    var flag = false;
                    foreach (var item in a.Schedule.Where(item => item.Date.Within(rng))
                                          .Where(item => item.VoucherID != null))
                    {
                        var voucher = m_Accountant.SelectVoucher(item.VoucherID);
                        if (voucher == null)
                        {
                            item.VoucherID = null;
                            cnt++;
                            flag = true;
                        }
                        else if (m_Accountant.DeleteVoucher(voucher.ID))
                        {
                            item.VoucherID = null;
                            cnt++;
                            flag = true;
                        }
                    }
                    if (flag)
                        m_Accountant.Upsert(a);
                }
                return new NumberAffected(cnt);
            }
            var assetResetHardContext = expr.assetResetHard();
            if (assetResetHardContext != null)
            {
                var query = assetResetHardContext.vouchers();

                return new NumberAffected(
                    m_Accountant.SelectAssets(assetResetHardContext.distributedQ())
                                .Sum(
                                     a =>
                                     {
                                         var mainQuery = new VoucherQueryAryBase(
                                             OperatorType.Union,
                                             new IQueryCompunded<IVoucherQueryAtom>[]
                                                 {
                                                     new VoucherQueryAtomBase(
                                                         new Voucher
                                                             {
                                                                 Type = VoucherType.Depreciation
                                                             },
                                                         new VoucherDetail
                                                             {
                                                                 Title = a.DepreciationTitle,
                                                                 Content = a.StringID
                                                             }),
                                                     new VoucherQueryAtomBase(
                                                         new Voucher
                                                             {
                                                                 Type = VoucherType.Devalue
                                                             },
                                                         new VoucherDetail
                                                             {
                                                                 Title = a.DevaluationTitle,
                                                                 Content = a.StringID
                                                             })
                                                 });
                                         return m_Accountant.DeleteVouchers(
                                                                            new VoucherQueryAryBase(
                                                                                OperatorType.Intersect,
                                                                                new IQueryCompunded<IVoucherQueryAtom>[]
                                                                                    {
                                                                                        query,
                                                                                        mainQuery
                                                                                    }));
                                     }));
            }
            var assetApplyContext = expr.assetApply();
            if (assetApplyContext != null)
            {
                var isCollapsed = assetApplyContext.AOCollapse() != null;

                var rng = assetApplyContext.range() != null
                              ? assetApplyContext.range().Range
                              : DateFilter.Unconstrained;

                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAssets(assetApplyContext.distributedQ())))
                {
                    foreach (var item in m_Accountant.Update(a, rng, isCollapsed))
                        sb.AppendLine(ListAssetItem(item));

                    m_Accountant.Upsert(a);
                }
                if (sb.Length > 0)
                    return new EditableText(sb.ToString());
                return new Suceed();
            }
            var assetCheckContext = expr.assetCheck();
            if (assetCheckContext != null)
            {
                var rng = new DateFilter(null, DateTime.Now.Date);

                var sb = new StringBuilder();
                foreach (var a in Sort(m_Accountant.SelectAssets(assetCheckContext.distributedQ())))
                {
                    var sbi = new StringBuilder();
                    foreach (var item in m_Accountant.Update(a, rng, false, true))
                        sbi.AppendLine(ListAssetItem(item));

                    if (sbi.Length != 0)
                    {
                        sb.AppendLine(ListAsset(a, null, false));
                        sb.AppendLine(sbi.ToString());
                    }

                    m_Accountant.Upsert(a);
                }
                if (sb.Length > 0)
                    return new EditableText(sb.ToString());
                return new Suceed();
            }

            throw new InvalidOperationException("资产表达式无效");
        }

        /// <summary>
        ///     显示资产及其折旧计算表
        /// </summary>
        /// <param name="asset">资产</param>
        /// <param name="dt">计算账面价值的时间</param>
        /// <param name="showSchedule">是否显示折旧计算表</param>
        /// <returns>格式化的信息</returns>
        private string ListAsset(Asset asset, DateTime? dt = null, bool showSchedule = true)
        {
            var sb = new StringBuilder();

            var bookValue = Accountant.GetBookValueOn(asset, dt);
            if (dt.HasValue &&
                (!bookValue.HasValue || Accountant.IsZero(bookValue.Value)))
                return null;
            sb.AppendFormat(
                            "{0} {1}{2:yyyyMMdd}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}{13}{14}",
                            asset.StringID,
                            asset.Name.CPadRight(35),
                            asset.Date,
                            asset.Value.AsCurrency().CPadLeft(13),
                            dt.HasValue ? bookValue.AsCurrency().CPadLeft(13) : "-".CPadLeft(13),
                            asset.Salvge.AsCurrency().CPadLeft(13),
                            asset.Title.AsTitle().CPadLeft(5),
                            asset.DepreciationTitle.AsTitle().CPadLeft(5),
                            asset.DevaluationTitle.AsTitle().CPadLeft(5),
                            asset.DepreciationExpenseTitle.AsTitle().CPadLeft(5),
                            asset.DepreciationExpenseSubTitle.AsSubTitle(),
                            asset.DevaluationExpenseTitle.AsTitle().CPadLeft(5),
                            asset.DevaluationExpenseSubTitle.AsSubTitle(),
                            asset.Life.ToString().CPadLeft(4),
                            asset.Method.ToString().CPadLeft(20));
            sb.AppendLine();
            if (showSchedule && asset.Schedule != null)
                foreach (var assetItem in asset.Schedule)
                {
                    sb.AppendLine(ListAssetItem(assetItem));
                    if (assetItem.VoucherID != null)
                        sb.AppendLine(CSharpHelper.PresentVoucher(m_Accountant.SelectVoucher(assetItem.VoucherID)));
                }
            return sb.ToString();
        }

        /// <summary>
        ///     显示折旧计算表条目
        /// </summary>
        /// <param name="assetItem">折旧计算表条目</param>
        /// <returns>格式化的信息</returns>
        private static string ListAssetItem(AssetItem assetItem)
        {
            if (assetItem is AcquisationItem)
                return String.Format(
                                     "   {0:yyyMMdd} ACQ:{1} ={3} ({2})",
                                     assetItem.Date,
                                     (assetItem as AcquisationItem).OrigValue.AsCurrency().CPadLeft(13),
                                     assetItem.VoucherID,
                                     assetItem.Value.AsCurrency().CPadLeft(13));
            if (assetItem is DepreciateItem)
                return String.Format(
                                     "   {0:yyyMMdd} DEP:{1} ={3} ({2})",
                                     assetItem.Date,
                                     (assetItem as DepreciateItem).Amount.AsCurrency().CPadLeft(13),
                                     assetItem.VoucherID,
                                     assetItem.Value.AsCurrency().CPadLeft(13));
            if (assetItem is DevalueItem)
                return String.Format(
                                     "   {0:yyyMMdd} DEV:{1} ={3} ({2})",
                                     assetItem.Date,
                                     (assetItem as DevalueItem).Amount.AsCurrency().CPadLeft(13),
                                     assetItem.VoucherID,
                                     assetItem.Value.AsCurrency().CPadLeft(13));
            if (assetItem is DispositionItem)
                return String.Format(
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
        {
            return enumerable.OrderBy(a => a.Date, new DateComparer()).ThenBy(a => a.Name).ThenBy(a => a.ID);
        }
    }
}
