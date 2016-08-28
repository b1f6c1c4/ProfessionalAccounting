using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     检验表达式解释器
    /// </summary>
    internal class CheckShell
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public CheckShell(Accountant helper) { m_Accountant = helper; }

        /// <summary>
        ///     检查每张会计记账凭证借贷方是否相等
        /// </summary>
        /// <returns>有误的会计记账凭证表达式</returns>
        public IQueryResult BasicCheck()
        {
            var sb = new StringBuilder();
            foreach (var voucher in m_Accountant.SelectVouchers(null))
            {
                // ReSharper disable once PossibleInvalidOperationException
                var val = voucher.Details.Sum(d => d.Fund.Value);
                if (val.IsZero())
                    continue;

                sb.AppendLine(val > 0 ? $"/* Debit - Credit = {val:R} */" : $"/* Credit - Debit = {-val:R} */");
                sb.Append(CSharpHelper.PresentVoucher(voucher));
            }
            if (sb.Length > 0)
                return new EditableText(sb.ToString());
            return new Succeed();
        }

        /// <summary>
        ///     检查每科目每内容每日资产无贷方余额，负债无借方余额
        /// </summary>
        /// <returns>发生错误的第一日及其信息</returns>
        public IQueryResult AdvancedCheck()
        {
            var res =
                m_Accountant.SelectVoucherDetailsGrouped(
                                                         new GroupedQueryBase(
                                                             filter: null,
                                                             subtotal: new SubtotalBase
                                                                           {
                                                                               AggrType = AggregationType.ChangedDay,
                                                                               Levels = new[]
                                                                                            {
                                                                                                SubtotalLevel.Title,
                                                                                                SubtotalLevel.SubTitle,
                                                                                                SubtotalLevel.Content,
                                                                                                SubtotalLevel.Day
                                                                                            }
                                                                           }));

            var sb = new StringBuilder();
            foreach (var grpTitle in res.GroupByTitle())
            {
                if (grpTitle.Key >= 4000 &&
                    grpTitle.Key < 5000)
                    continue;

                if (grpTitle.Key == 1901)
                    continue;

                foreach (var grpSubTitle in grpTitle.GroupBySubTitle())
                {
                    if (grpTitle.Key == 1101 &&
                        grpSubTitle.Key == 02)
                        continue;
                    if (grpTitle.Key == 1501 &&
                        grpSubTitle.Key == 02)
                        continue;
                    if (grpTitle.Key == 1503 &&
                        grpSubTitle.Key == 02)
                        continue;
                    if (grpTitle.Key == 1511 &&
                        grpSubTitle.Key == 02)
                        continue;
                    if (grpTitle.Key == 1511 &&
                        grpSubTitle.Key == 03)
                        continue;
                    if (grpTitle.Key == 6603 &&
                        grpSubTitle.Key == null)
                        continue;
                    if (grpTitle.Key == 6603 &&
                        grpSubTitle.Key == 03)
                        continue;
                    if (grpTitle.Key == 6603 &&
                        grpSubTitle.Key == 99)
                        continue;
                    if (grpTitle.Key == 6711 &&
                        grpSubTitle.Key == 10)
                        continue;

                    var isPositive = grpTitle.Key < 2000 || grpTitle.Key >= 6400;
                    if (grpTitle.Key == 1502 ||
                        grpTitle.Key == 1504 ||
                        grpTitle.Key == 1504 ||
                        grpTitle.Key == 1504 ||
                        grpTitle.Key == 1504 ||
                        grpTitle.Key == 1512 ||
                        grpTitle.Key == 1602 ||
                        grpTitle.Key == 1603 ||
                        grpTitle.Key == 1702 ||
                        grpTitle.Key == 1703 ||
                        grpTitle.Key == 1602 ||
                        grpTitle.Key == 1602)
                        isPositive = false;
                    else if (grpTitle.Key == 6603 &&
                             grpSubTitle.Key == 02)
                        isPositive = true;

                    foreach (var grpContent in grpSubTitle.GroupByContent())
                        foreach (var balance in grpContent.AggregateChangedDay())
                        {
                            if (isPositive && balance.Fund.IsNonNegative())
                                continue;
                            if (!isPositive &&
                                balance.Fund.IsNonPositive())
                                continue;

                            sb.AppendLine(
                                          $"{balance.Date:yyyyMMdd} " +
                                          $"{grpTitle.Key.AsTitle()}{grpSubTitle.Key.AsSubTitle()} " +
                                          $"{grpContent.Key}:{balance.Fund:R}");
                            sb.AppendLine();
                            break;
                        }
                }
            }

            if (sb.Length > 0)
                return new EditableText(sb.ToString());
            return new Succeed();
        }
    }
}
