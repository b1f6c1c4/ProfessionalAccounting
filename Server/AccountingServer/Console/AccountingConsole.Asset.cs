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
        public static string PresentAsset(DbAsset asset)
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
                            asset.Method.ToString().CPadLeft(25));
            sb.AppendLine();
            return sb.ToString();
        }

        public string PresentAssets()
        {
            var sb = new StringBuilder();
            var query = m_Accountant.SelectAssets(new DbAsset());
            foreach (var a in query)
                sb.Append(PresentAsset(a));
            return sb.ToString();
        }
    }
}
