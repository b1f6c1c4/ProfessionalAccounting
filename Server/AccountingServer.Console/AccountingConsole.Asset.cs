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

            if (!asset.ID.HasValue)
            {
                if (!m_Accountant.Upsert(asset))
                    throw new Exception();
            }
            else if (!m_Accountant.Update(asset))
                throw new Exception();

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
                throw new Exception();

            return m_Accountant.DeleteAsset(asset.ID.Value);
        }

        /// <summary>
        ///     执行表达式（资产）
        /// </summary>
        /// <param name="s">表达式</param>
        /// <returns>执行结果</returns>
        public IQueryResult ExecuteAsset(string s)
        {
            AutoConnect();

            var sp = s.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var query = sp.Length == 1 ? String.Empty : sp[1];

            if (sp[0].Equals("a", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                var filter = ParseAssetQuery(query);
                foreach (var a in Sort(m_Accountant.FilteredSelect(filter)))
                    sb.Append(ListAsset(a, DateTime.Now.Date, false));

                return new UnEditableText(sb.ToString());
            }
            if (sp[0].StartsWith("a@", StringComparison.OrdinalIgnoreCase))
            {
                var dt = DateTime.ParseExact(sp[0] + "01", "a@yyyyMMdd", null).AddMonths(1).AddDays(-1);

                var sb = new StringBuilder();
                var filter = ParseAssetQuery(query);
                foreach (var a in Sort(m_Accountant.FilteredSelect(filter)))
                    sb.Append(ListAsset(a, dt, false));

                return new UnEditableText(sb.ToString());
            }
            if (sp[0].Equals("a-all", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                var filter = ParseAssetQuery(query);
                foreach (var a in Sort(m_Accountant.FilteredSelect(filter)))
                    sb.Append(ListAsset(a, null, false));

                return new UnEditableText(sb.ToString());
            }
            if (sp[0].StartsWith("a-li", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                var filter = ParseAssetQuery(query);
                foreach (var a in Sort(m_Accountant.FilteredSelect(filter)))
                    sb.Append(ListAsset(a));

                return new UnEditableText(sb.ToString());
            }
            if (sp[0].StartsWith("a-q", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                var filter = ParseAssetQuery(query);
                foreach (var a in Sort(m_Accountant.FilteredSelect(filter)))
                    sb.Append(CSharpHelper.PresentAsset(a));

                return new EditableText(sb.ToString());
            }
            if (sp[0].StartsWith("a-reg", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                var filter = ParseAssetQuery(query);
                foreach (var a in Sort(m_Accountant.FilteredSelect(filter)))
                {
                    foreach (var voucher in m_Accountant.RegisterVouchers(a))
                        sb.Append(CSharpHelper.PresentVoucher(voucher));

                    m_Accountant.Update(a);
                }
                if (sb.Length > 0)
                    return new EditableText(sb.ToString());
                return new Suceed();
            }
            if (sp[0].StartsWith("a-unr", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                var filter = ParseAssetQuery(query);
                foreach (var a in Sort(m_Accountant.FilteredSelect(filter)))
                {
                    foreach (var item in a.Schedule)
                        item.VoucherID = null;

                    sb.Append(ListAsset(a));
                    m_Accountant.Update(a);
                }
                return new EditableText(sb.ToString());
            }
            if (sp[0].Equals("a-rd", StringComparison.OrdinalIgnoreCase) ||
                sp[0].Equals("a-redep", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                var filter = ParseAssetQuery(query);
                foreach (var a in m_Accountant.FilteredSelect(filter))
                {
                    Accountant.Depreciate(a);
                    sb.Append(CSharpHelper.PresentAsset(a));
                    m_Accountant.Update(a);
                }
                return new EditableText(sb.ToString());
            }
            if (sp[0].Equals("a-reset-hard", StringComparison.OrdinalIgnoreCase))
            {
                var filter = ParseAssetQuery(query);
                var cnt = 0L;
                foreach (var a in m_Accountant.FilteredSelect(filter))
                {
                    cnt += m_Accountant.FilteredDelete(
                                                       new Voucher { Type = VoucherType.Depreciation },
                                                       new VoucherDetail
                                                           {
                                                               Title = a.DepreciationTitle,
                                                               Content = a.StringID
                                                           });
                    cnt += m_Accountant.FilteredDelete(
                                                       new Voucher { Type = VoucherType.Devalue },
                                                       new VoucherDetail
                                                           {
                                                               Title = a.DevaluationTitle,
                                                               Content = a.StringID
                                                           });
                }
                return new NumberAffected(cnt);
            }
            if (sp[0].StartsWith("a-ap", StringComparison.OrdinalIgnoreCase))
            {
                var isCollapsed = sp[0].EndsWith("-collapse", StringComparison.OrdinalIgnoreCase) ||
                                  sp[0].EndsWith("-co", StringComparison.OrdinalIgnoreCase);

                string dq;
                Asset filter = null;
                if (query.LastIndexOf('\'') >= 0)
                {
                    var spx = query.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    dq = spx.Length <= 1 ? String.Empty : spx[1];
                    filter = ParseAssetQuery(spx[0]);
                }
                else
                    dq = query;

                var rng = String.IsNullOrWhiteSpace(dq) ? ParseDateQuery("[0]", true) : ParseDateQuery(dq, true);

                var sb = new StringBuilder();
                foreach (var a in m_Accountant.FilteredSelect(filter))
                {
                    foreach (var item in m_Accountant.Update(a, rng, isCollapsed))
                        sb.AppendLine(ListAssetItem(item));

                    m_Accountant.Update(a);
                }
                if (sb.Length > 0)
                    return new EditableText(sb.ToString());
                return new Suceed();
            }
            if (sp[0].StartsWith("a-chk", StringComparison.OrdinalIgnoreCase))
            {
                var filter = ParseAssetQuery(query);

                var rng = new DateFilter(null, DateTime.Now.Date);

                var sb = new StringBuilder();
                foreach (var a in m_Accountant.FilteredSelect(filter))
                {
                    var sbi = new StringBuilder();
                    foreach (var item in m_Accountant.Update(a, rng, false, true))
                        sbi.AppendLine(ListAssetItem(item));

                    if (sbi.Length != 0)
                    {
                        sb.AppendLine(ListAsset(a, null, false));
                        sb.AppendLine(sbi.ToString());
                    }

                    m_Accountant.Update(a);
                }
                if (sb.Length > 0)
                    return new EditableText(sb.ToString());
                return new Suceed();
            }

            throw new InvalidOperationException("资产表达式无效");
        }

        /// <summary>
        ///     显示资产及其计算表
        /// </summary>
        /// <param name="asset">资产</param>
        /// <param name="dt">计算账面价值的时间</param>
        /// <param name="showSchedule">是否显示计算表</param>
        /// <returns>格式化的信息</returns>
        private string ListAsset(Asset asset, DateTime? dt = null, bool showSchedule = true)
        {
            var sb = new StringBuilder();

            var bookValue = Accountant.GetBookValueOn(asset, dt);
            if (dt.HasValue &&
                (!bookValue.HasValue || bookValue < Accountant.Tolerance))
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

        private static string ListAssetItem(AssetItem assetItem)
        {
            if (assetItem is AcquisationItem)
                return String.Format(
                                     "   {0:yyyMMdd} ACQ:{1} ={3} ({2})",
                                     assetItem.Date,
                                     (assetItem as AcquisationItem).OrigValue.AsCurrency().CPadLeft(13),
                                     assetItem.VoucherID,
                                     assetItem.BookValue.AsCurrency().CPadLeft(13));
            if (assetItem is DepreciateItem)
                return String.Format(
                                     "   {0:yyyMMdd} DEP:{1} ={3} ({2})",
                                     assetItem.Date,
                                     (assetItem as DepreciateItem).Amount.AsCurrency().CPadLeft(13),
                                     assetItem.VoucherID,
                                     assetItem.BookValue.AsCurrency().CPadLeft(13));
            if (assetItem is DevalueItem)
                return String.Format(
                                     "   {0:yyyMMdd} DEV:{1} ={3} ({2})",
                                     assetItem.Date,
                                     (assetItem as DevalueItem).Amount.AsCurrency().CPadLeft(13),
                                     assetItem.VoucherID,
                                     assetItem.BookValue.AsCurrency().CPadLeft(13));
            if (assetItem is DispositionItem)
                return String.Format(
                                     "   {0:yyyMMdd} DSP:{1} ={3} ({2})",
                                     assetItem.Date,
                                     "ALL".CPadLeft(13),
                                     assetItem.VoucherID,
                                     assetItem.BookValue.AsCurrency().CPadLeft(13));
            return null;
        }

        private static IEnumerable<Asset> Sort(IEnumerable<Asset> enumerable)
        {
            return enumerable.OrderBy(a => a.Date, new DateComparer()).ThenBy(a => a.Name).ThenBy(a => a.ID);
        }
    }
}
