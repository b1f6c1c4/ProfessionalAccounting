using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell.Parsing;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     资产表达式解释器
    /// </summary>
    internal class AssetShell
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public AssetShell(Accountant helper) { m_Accountant = helper; }

        /// <summary>
        ///     执行资产表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        public IQueryResult ExecuteAsset(ShellParser.AssetContext expr)
        {
            var assetListContext = expr.assetList();
            if (assetListContext != null)
            {
                var showSchedule = assetListContext.AOList() != null;
                var dt = showSchedule
                             ? null
                             : assetListContext.rangePoint() != null
                                   ? assetListContext.rangePoint().Range.EndDate
                                   : DateTime.Now.Date;

                return ExecuteList(assetListContext.distributedQ(), dt, showSchedule);
            }
            var assetQueryContext = expr.assetQuery();
            if (assetQueryContext != null)
                return ExecuteQuery(assetQueryContext.distributedQ());
            var assetRegisterContext = expr.assetRegister();
            if (assetRegisterContext != null)
                return ExecuteRegister(
                                       assetRegisterContext.distributedQ(),
                                       assetRegisterContext.range().TheRange(),
                                       assetRegisterContext.vouchers());
            var assetUnregisterContext = expr.assetUnregister();
            if (assetUnregisterContext != null)
                return ExecuteUnregister(
                                         assetUnregisterContext.distributedQ(),
                                         assetUnregisterContext.range().TheRange(),
                                         assetUnregisterContext.vouchers());
            var assetRedepContext = expr.assetRedep();
            if (assetRedepContext != null)
                return ExecuteRedep(assetRedepContext.distributedQ());
            var assetResetSoftContext = expr.assetResetSoft();
            if (assetResetSoftContext != null)
                return ExecuteResetSoft(
                                        assetResetSoftContext.distributedQ(),
                                        assetResetSoftContext.range().TheRange());
            var assetResetMixedContext = expr.assetResetMixed();
            if (assetResetMixedContext != null)
                return ExcuteResetMixed(
                                        assetResetMixedContext.distributedQ(),
                                        assetResetMixedContext.range().TheRange());
            var assetResetHardContext = expr.assetResetHard();
            if (assetResetHardContext != null)
                return ExecuteResetHard(assetResetHardContext.distributedQ(), assetResetHardContext.vouchers());
            var assetApplyContext = expr.assetApply();
            if (assetApplyContext != null)
                return ExecuteApply(
                                    assetApplyContext.distributedQ(),
                                    assetApplyContext.range().TheRange(),
                                    assetApplyContext.AOCollapse() != null);
            var assetCheckContext = expr.assetCheck();
            if (assetCheckContext != null)
                return ExecuteCheck(assetCheckContext.distributedQ(), new DateFilter(null, DateTime.Now.Date));

            throw new InvalidOperationException("资产表达式无效");
        }

        /// <summary>
        ///     执行列表表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="dt">计算账面价值的时间</param>
        /// <param name="showSchedule">是否显示折旧计算表</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteList(IQueryCompunded<IDistributedQueryAtom> distQuery, DateTime? dt,
                                         bool showSchedule)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(m_Accountant.SelectAssets(distQuery)))
                sb.Append(ListAsset(a, dt, showSchedule));

            if (showSchedule)
                return new EditableText(sb.ToString());
            return new UnEditableText(sb.ToString());
        }

        /// <summary>
        ///     执行查询表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteQuery(IQueryCompunded<IDistributedQueryAtom> distQuery)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(m_Accountant.SelectAssets(distQuery)))
                sb.Append(CSharpHelper.PresentAsset(a));

            return new EditableText(sb.ToString());
        }

        /// <summary>
        ///     执行注册表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="query">记账凭证检索式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteRegister(IQueryCompunded<IDistributedQueryAtom> distQuery, DateFilter rng,
                                             IQueryCompunded<IVoucherQueryAtom> query)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(m_Accountant.SelectAssets(distQuery)))
            {
                foreach (var voucher in m_Accountant.RegisterVouchers(a, rng, query))
                    sb.Append(CSharpHelper.PresentVoucher(voucher));

                m_Accountant.Upsert(a);
            }
            if (sb.Length > 0)
                return new EditableText(sb.ToString());
            return new Succeed();
        }

        /// <summary>
        ///     执行解除注册表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="query">记账凭证检索式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteUnregister(IQueryCompunded<IDistributedQueryAtom> distQuery, DateFilter rng,
                                               IQueryCompunded<IVoucherQueryAtom> query)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(m_Accountant.SelectAssets(distQuery)))
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

        /// <summary>
        ///     执行重新计算表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteRedep(IQueryCompunded<IDistributedQueryAtom> distQuery)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(m_Accountant.SelectAssets(distQuery)))
            {
                Accountant.Depreciate(a);
                sb.Append(CSharpHelper.PresentAsset(a));
                m_Accountant.Upsert(a);
            }
            return new EditableText(sb.ToString());
        }

        /// <summary>
        ///     执行软重置表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteResetSoft(IQueryCompunded<IDistributedQueryAtom> distQuery, DateFilter rng)
        {
            var cnt = 0L;
            foreach (var a in m_Accountant.SelectAssets(distQuery))
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

        /// <summary>
        ///     执行混合重置表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExcuteResetMixed(IQueryCompunded<IDistributedQueryAtom> distQuery, DateFilter rng)
        {
            var cnt = 0L;
            foreach (var a in m_Accountant.SelectAssets(distQuery))
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

        /// <summary>
        ///     执行硬重置表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="query">记账凭证检索式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteResetHard(IQueryCompunded<IDistributedQueryAtom> distQuery,
                                              IQueryCompunded<IVoucherQueryAtom> query)
        {
            Func<Asset, IQueryCompunded<IVoucherQueryAtom>> getMainQ =
                a => new VoucherQueryAryBase(
                         OperatorType.Union,
                         new IQueryCompunded<IVoucherQueryAtom>[]
                             {
                                 new VoucherQueryAtomBase(
                                     new Voucher { Type = VoucherType.Depreciation },
                                     new VoucherDetail { Title = a.DepreciationTitle, Content = a.StringID }),
                                 new VoucherQueryAtomBase(
                                     new Voucher { Type = VoucherType.Devalue },
                                     new VoucherDetail { Title = a.DevaluationTitle, Content = a.StringID })
                             });
            return new NumberAffected(
                m_Accountant.SelectAssets(distQuery)
                            .Sum(
                                 a => m_Accountant.DeleteVouchers(
                                                                  new VoucherQueryAryBase
                                                                      (
                                                                      OperatorType.Intersect,
                                                                      new[] { query, getMainQ(a) }))));
        }

        /// <summary>
        ///     执行应用表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <param name="isCollapsed">是否压缩</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteApply(IQueryCompunded<IDistributedQueryAtom> distQuery, DateFilter rng,
                                          bool isCollapsed)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(m_Accountant.SelectAssets(distQuery)))
            {
                foreach (var item in m_Accountant.Update(a, rng, isCollapsed))
                    sb.AppendLine(ListAssetItem(item));

                m_Accountant.Upsert(a);
            }
            if (sb.Length > 0)
                return new EditableText(sb.ToString());
            return new Succeed();
        }

        /// <summary>
        ///     执行检查表达式
        /// </summary>
        /// <param name="distQuery">分期检索式</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteCheck(IQueryCompunded<IDistributedQueryAtom> distQuery, DateFilter rng)
        {
            var sb = new StringBuilder();
            foreach (var a in Sort(m_Accountant.SelectAssets(distQuery)))
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
            return new Succeed();
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
                (!bookValue.HasValue || bookValue.Value.IsZero()))
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
                return string.Format(
                                     "   {0:yyyMMdd} ACQ:{1} ={3} ({2})",
                                     assetItem.Date,
                                     (assetItem as AcquisationItem).OrigValue.AsCurrency().CPadLeft(13),
                                     assetItem.VoucherID,
                                     assetItem.Value.AsCurrency().CPadLeft(13));
            if (assetItem is DepreciateItem)
                return string.Format(
                                     "   {0:yyyMMdd} DEP:{1} ={3} ({2})",
                                     assetItem.Date,
                                     (assetItem as DepreciateItem).Amount.AsCurrency().CPadLeft(13),
                                     assetItem.VoucherID,
                                     assetItem.Value.AsCurrency().CPadLeft(13));
            if (assetItem is DevalueItem)
                return string.Format(
                                     "   {0:yyyMMdd} DEV:{1} ={3} ({2})",
                                     assetItem.Date,
                                     (assetItem as DevalueItem).Amount.AsCurrency().CPadLeft(13),
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
