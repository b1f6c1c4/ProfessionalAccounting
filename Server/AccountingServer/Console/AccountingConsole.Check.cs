using System;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    internal partial class AccountingConsole
    {
        /// <summary>
        ///     检查每张会计凭证借贷方是否相等
        /// </summary>
        /// <returns>有误的会计凭证表达式</returns>
        private IQueryResult BasicCheck()
        {
            AutoConnect();

            var sb = new StringBuilder();
            foreach (var voucher in m_Accountant.FilteredSelect(null, filter: null))
            {
                var val = m_Accountant.IsBalanced(voucher);
                if (Math.Abs(val) < Accountant.Tolerance)
                    continue;

                if (val > 0)
                    sb.AppendFormat("/* Debit - Credit = {0:R} */", val);
                else
                    sb.AppendFormat("/* Credit - Debit = {0:R} */", -val);
                sb.AppendLine();
                sb.Append(CSharpHelper.PresentVoucher(voucher));
            }
            if (sb.Length > 0)
                return new EditableText(sb.ToString());
            return new Suceed();
        }

        /// <summary>
        ///     检查每科目每内容每日资产无贷方余额，负债无借方余额
        /// </summary>
        /// <returns>发生错误的第一日及其信息</returns>
        private IQueryResult AdvancedCheck()
        {
            AutoConnect();

            var sb = new StringBuilder();
            var flag = false;
            foreach (var title in TitleManager.GetTitles())
                if (Accountant.IsAsset(title.Item1) &&
                    title.Item1 != 1602 &&
                    title.Item1 != 1603 &&
                    title.Item1 != 1702 &&
                    title.Item1 != 1703 &&
                    !(title.Item1 == 1101 && title.Item2 == 02))
                {
                    foreach (
                        var content in
                            m_Accountant.FilteredSelectDetails(
                                                               filter:
                                                                   new VoucherDetail
                                                                       {
                                                                           Title = title.Item1,
                                                                           SubTitle = title.Item2
                                                                       })
                                        .Select(d => d.Content)
                                        .Distinct())
                        foreach (
                            var balance in
                                m_Accountant.GetDailyBalance(
                                                             new Balance
                                                                 {
                                                                     Title = title.Item1,
                                                                     SubTitle = title.Item2,
                                                                     Content = content
                                                                 },
                                                             DateFilter.Unconstrained))
                            if (balance.Fund < -Accountant.Tolerance)
                            {
                                flag = true;
                                sb.AppendFormat(
                                                "{0:yyyyMMdd} {1}{2} {3} {4}:{5:R}",
                                                balance.Date,
                                                title.Item1.AsTitle(),
                                                title.Item2.AsSubTitle(),
                                                title.Item3,
                                                content,
                                                balance.Fund);
                                sb.AppendLine();
                                break;
                            }
                }
                else if (Accountant.IsDebt(title.Item1) ||
                         title.Item1 == 1602 ||
                         title.Item1 == 1603 ||
                         title.Item1 == 1702 ||
                         title.Item1 == 1703)
                    foreach (
                        var content in
                            m_Accountant.FilteredSelectDetails(
                                                               filter:
                                                                   new VoucherDetail
                                                                       {
                                                                           Title = title.Item1,
                                                                           SubTitle = title.Item2
                                                                       })
                                        .Select(d => d.Content)
                                        .Distinct())
                        foreach (
                            var balance in
                                m_Accountant.GetDailyBalance(
                                                             new Balance
                                                                 {
                                                                     Title = title.Item1,
                                                                     SubTitle = title.Item2,
                                                                     Content = content
                                                                 },
                                                             DateFilter.Unconstrained))
                            if (balance.Fund > Accountant.Tolerance)
                            {
                                flag = true;
                                sb.AppendFormat(
                                                "{0:yyyyMMdd} {1}{2} {3} {4}:{5:R}",
                                                balance.Date,
                                                title.Item1.AsTitle(),
                                                title.Item2.AsSubTitle(),
                                                title.Item3,
                                                content,
                                                balance.Fund);
                                sb.AppendLine();
                                break;
                            }
            if (flag)
                return new EditableText(sb.ToString());
            return new Suceed();
        }
    }
}
