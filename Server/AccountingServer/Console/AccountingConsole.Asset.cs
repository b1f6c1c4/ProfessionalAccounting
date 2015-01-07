using System;
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
            else if (!m_Accountant.UpdateAsset(asset))
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
                foreach (var a in m_Accountant.FilteredSelect(filter))
                    sb.Append(ListAsset(a, false));

                return sb.ToString();
            }
            if (sp[0] == "a-list")
            {
                editable = false;

                var sb = new StringBuilder();
                var filter = ParseAssetQuery(query);
                foreach (var a in m_Accountant.FilteredSelect(filter))
                    sb.Append(ListAsset(a));

                return sb.ToString();
            }
            if (sp[0] == "a-query")
            {
                editable = true;

                var sb = new StringBuilder();
                var filter = ParseAssetQuery(query);
                foreach (var a in m_Accountant.FilteredSelect(filter))
                    sb.Append(CSharpHelper.PresentAsset(a));

                return sb.ToString();
            }
            // TODO: add others

            throw new InvalidOperationException("资产表达式无效");
        }

        /// <summary>
        ///     解析资产检索表达式
        /// </summary>
        /// <param name="s">资产检索表达式</param>
        /// <returns>过滤器</returns>
        private static Asset ParseAssetQuery(string s) { throw new NotImplementedException(); }


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
                            "{0} {1}{2:yyyyMMdd}{3}{4}{5}{6}{7}{8}{9}{10}{11}",
                            asset.ID,
                            asset.Name.CPadRight(35),
                            asset.Date,
                            asset.Value.AsCurrency().CPadLeft(13),
                            asset.Salvge.AsCurrency().CPadLeft(13),
                            asset.Title.AsTitle().CPadLeft(5),
                            asset.DepreciationTitle.AsTitle().CPadLeft(5),
                            asset.DevaluationTitle.AsTitle().CPadLeft(5),
                            asset.ExpenseTitle.AsTitle().CPadLeft(5),
                            asset.ExpenseSubTitle.AsSubTitle(),
                            asset.Life.ToString().CPadLeft(4),
                            asset.Method.ToString().CPadLeft(20));
            sb.AppendLine();
            if (showSchedule && asset.Schedule != null)
                foreach (var assetItem in asset.Schedule)
                {
                    if (assetItem is AcquisationItem)
                    {
                        sb.AppendFormat(
                                        "   {0:yyyMMdd} ACQ:{1} ={3} ({2})",
                                        assetItem.Date,
                                        (assetItem as AcquisationItem).OrigValue.AsCurrency().CPadLeft(13),
                                        assetItem.VoucherID,
                                        assetItem.BookValue.AsCurrency().CPadLeft(13));
                        sb.AppendLine();
                    }
                    else if (assetItem is DepreciateItem)
                    {
                        sb.AppendFormat(
                                        "   {0:yyyMMdd} DEP:{1} ={3} ({2})",
                                        assetItem.Date,
                                        (assetItem as DepreciateItem).Amount.AsCurrency().CPadLeft(13),
                                        assetItem.VoucherID,
                                        assetItem.BookValue.AsCurrency().CPadLeft(13));
                        sb.AppendLine();
                    }
                    else if (assetItem is DevalueItem)
                    {
                        sb.AppendFormat(
                                        "   {0:yyyMMdd} DEV:{1} ={3} ({2})",
                                        assetItem.Date,
                                        (assetItem as DevalueItem).FairValue.AsCurrency().CPadLeft(13),
                                        assetItem.VoucherID,
                                        assetItem.BookValue.AsCurrency().CPadLeft(13));
                        sb.AppendLine();
                    }
                    else if (assetItem is DispositionItem)
                    {
                        sb.AppendFormat(
                                        "   {0:yyyMMdd} DSP:{1} ={3} ({2})",
                                        assetItem.Date,
                                        (assetItem as DispositionItem).NetValue.AsCurrency().CPadLeft(13),
                                        assetItem.VoucherID,
                                        assetItem.BookValue.AsCurrency().CPadLeft(13));
                        sb.AppendLine();
                    }

                    if (assetItem.VoucherID != null)
                        sb.AppendLine(CSharpHelper.PresentVoucher(m_Accountant.SelectVoucher(assetItem.VoucherID)));
                }
            return sb.ToString();
        }

        private string ApplyAll(string s, bool isCollapse)
        {
            var rng = ParseDateQuery(s);

            var sb = new StringBuilder();

            if (isCollapse)
                foreach (var asset in m_Accountant.FilteredSelect(new Asset()))
                {
                    ApplyItem(asset, rng, true);
                    sb.Append(ListAsset(asset));
                }
            else
                foreach (var asset in m_Accountant.FilteredSelect(new Asset()))
                {
                    ApplyItem(asset, rng);
                    sb.Append(ListAsset(asset));
                }

            return sb.ToString();
        }

        private void ApplyItem(Asset asset, DateFilter rng, bool isCollapsed = false)
        {
            // TODO: fixthis
            m_Accountant.Update(asset, rng, isCollapsed);
        }


        private void RecalcAllDepreciation()
        {
            foreach (var asset in m_Accountant.FilteredSelect(new Asset()))
            {
                asset.Salvge = asset.Value * 0.05;
                asset.Method = DepreciationMethod.StraightLine;
                Accountant.Depreciate(asset);
                m_Accountant.UpdateAsset(asset);
            }
        }
    }
}
