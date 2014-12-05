using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    internal partial class AccountingConsole
    {
        /// <summary>
        ///     将资产用C#表示
        /// </summary>
        /// <param name="asset">资产</param>
        /// <returns>C#表达式</returns>
        public static string PresentAsset(Asset asset)
        {
            // TODO: Really Present them
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
            if (asset.Schedule != null)
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
                }
            return sb.ToString();
        }

        public string PresentAssets()
        {
            var sb = new StringBuilder();
            var query = m_Accountant.SelectAssets(new Asset());
            foreach (var a in query)
                sb.Append(PresentAsset(a));
            return sb.ToString();
        }
    }
}
