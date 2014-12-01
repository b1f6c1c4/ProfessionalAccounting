using System;
using System.Reflection;
using System.Resources;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    public static class TitleManager
    {
        /// <summary>
        ///     返回编号对应的会计科目名称
        /// </summary>
        /// <param name="title">一级科目编号</param>
        /// <param name="subtitle">二级科目编号</param>
        /// <returns>名称</returns>
        public static string GetTitleName(int? title, int? subtitle = null)
        {
            var mgr = new ResourceManager("AccountingServer.BLL.AccountTitle", Assembly.GetExecutingAssembly());
            return mgr.GetString(String.Format("T{0:0000}{1:00}", title, subtitle));
        }

        /// <summary>
        ///     返回细目对应的会计科目名称
        /// </summary>
        /// <param name="detail">细目</param>
        /// <returns>名称</returns>
        public static string GetTitleName(VoucherDetail detail)
        {
            return detail.SubTitle.HasValue
                       ? GetTitleName(detail.Title) + "-" + GetTitleName(detail.Title, detail.SubTitle)
                       : GetTitleName(detail.Title);
        }

        /// <summary>
        ///     返回余额对应的会计科目名称
        /// </summary>
        /// <param name="balance">余额</param>
        /// <returns>名称</returns>
        public static string GetTitleName(Balance balance)
        {
            return balance.SubTitle.HasValue
                       ? GetTitleName(balance.Title) + "-" + GetTitleName(balance.Title, balance.SubTitle)
                       : GetTitleName(balance.Title);
        }
    }
}
