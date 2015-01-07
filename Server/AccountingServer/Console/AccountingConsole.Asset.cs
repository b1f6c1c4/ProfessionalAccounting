using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    internal partial class AccountingConsole
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
                if (!m_Accountant.Insert(asset))
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
        /// <param name="editable">执行结果是否可编辑</param>
        /// <returns>执行结果</returns>
        public string ExecuteAsset(string s, out bool editable)
        {
            var sp = s.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var query = sp.Length == 1 ? String.Empty : sp[1];

            if (sp[0] == "a")
            {
                editable = false;

                var sb = new StringBuilder();
                var filter = ParseAssetQuery(query);
                foreach (var a in Sort(m_Accountant.FilteredSelect(filter)))
                    sb.Append(ListAsset(a, false));

                return sb.ToString();
            }
            if (sp[0] == "a-list")
            {
                editable = false;

                var sb = new StringBuilder();
                var filter = ParseAssetQuery(query);
                foreach (var a in Sort(m_Accountant.FilteredSelect(filter)))
                    sb.Append(ListAsset(a));

                return sb.ToString();
            }
            if (sp[0] == "a-query")
            {
                editable = true;

                var sb = new StringBuilder();
                var filter = ParseAssetQuery(query);
                foreach (var a in Sort(m_Accountant.FilteredSelect(filter)))
                    sb.Append(CSharpHelper.PresentAsset(a));

                return sb.ToString();
            }
            if (sp[0] == "a-register")
            {
                editable = true;

                var sb = new StringBuilder();
                var filter = ParseAssetQuery(query);
                foreach (var a in Sort(m_Accountant.FilteredSelect(filter)))
                {
                    foreach (var voucher in m_Accountant.RegisterVouchers(a))
                        sb.Append(CSharpHelper.PresentVoucher(voucher));

                    m_Accountant.Update(a);
                }
                return sb.ToString();
            }
            if (sp[0] == "a-unregister")
            {
                editable = true;

                var sb = new StringBuilder();
                var filter = ParseAssetQuery(query);
                foreach (var a in Sort(m_Accountant.FilteredSelect(filter)))
                {
                    foreach (var item in a.Schedule)
                        item.VoucherID = null;

                    sb.Append(a);
                    m_Accountant.Update(a);
                }
                return sb.ToString();
            }
            if (sp[0] == "a-redep")
            {
                editable = true;

                var sb = new StringBuilder();
                var filter = ParseAssetQuery(query);
                foreach (var a in m_Accountant.FilteredSelect(filter))
                {
                    Accountant.Depreciate(a);
                    sb.Append(CSharpHelper.PresentAsset(a));
                    m_Accountant.Update(a);
                }
                return sb.ToString();
            }
            if (sp[0] == "a-reset-hard")
            {
                editable = true;

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
                return "OK " + cnt.ToString(CultureInfo.InvariantCulture);
            }
            if (sp[0].StartsWith("a-apply"))
            {
                editable = true;
                var isCollapsed = sp[0].EndsWith("-collapse");

                DateFilter rng;
                if (query.LastIndexOf('\'') >= 0)
                {
                    var spx = query.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    rng = ParseDateQuery(spx.Length <= 1 ? String.Empty : spx[1], true);
                }
                else
                {
                    rng = ParseDateQuery(query, true);
                }

                var sb = new StringBuilder();
                var filter = ParseAssetQuery(query);
                foreach (var a in m_Accountant.FilteredSelect(filter))
                {
                    foreach (var item in m_Accountant.Update(a, rng, isCollapsed))
                        sb.AppendLine(ListAssetItem(item));

                    m_Accountant.Update(a);
                }
                return sb.ToString();
            }

            throw new InvalidOperationException("资产表达式无效");
        }

        /// <summary>
        ///     显示资产及其计算表
        /// </summary>
        /// <param name="asset">资产</param>
        /// <param name="showSchedule">是否显示计算表</param>
        /// <returns>格式化的信息</returns>
        private string ListAsset(Asset asset, bool showSchedule = true)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(
                            "{0} {1}{2:yyyyMMdd}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}{13}",
                            asset.StringID,
                            asset.Name.CPadRight(35),
                            asset.Date,
                            asset.Value.AsCurrency().CPadLeft(13),
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
                                     (assetItem as DevalueItem).FairValue.AsCurrency().CPadLeft(13),
                                     assetItem.VoucherID,
                                     assetItem.BookValue.AsCurrency().CPadLeft(13));
            if (assetItem is DispositionItem)
                return String.Format(
                                     "   {0:yyyMMdd} DSP:{1} ={3} ({2})",
                                     assetItem.Date,
                                     (assetItem as DispositionItem).NetValue.AsCurrency().CPadLeft(13),
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
