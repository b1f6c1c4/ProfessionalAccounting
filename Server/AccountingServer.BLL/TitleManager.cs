using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    public static class TitleManager
    {
        private static readonly XmlDocument XmlDoc;

        static TitleManager()
        {
            using (
                var stream = Assembly.GetExecutingAssembly()
                                     .GetManifestResourceStream("AccountingServer.BLL.Resources.Titles.xml"))
            {
                if (stream == null)
                    return;
                XmlDoc = new XmlDocument();
                XmlDoc.Load(stream);
            }
        }

        /// <summary>
        ///     返回所有会计科目编号和名称
        /// </summary>
        /// <returns>编号和科目名称</returns>
        public static IEnumerable<Tuple<int, int?, string>> GetTitles()
        {
            foreach (XmlElement title in XmlDoc.DocumentElement.ChildNodes)
            {
                yield return new Tuple<int, int?, string>(
                    Convert.ToInt32(title.Attributes["id"].Value),
                    null,
                    title.Attributes["name"].Value);
                foreach (XmlElement subTitle in title.ChildNodes)
                {
                    yield return new Tuple<int, int?, string>(
                        Convert.ToInt32(title.Attributes["id"].Value),
                        Convert.ToInt32(subTitle.Attributes["id"].Value),
                        title.Attributes["name"].Value + "-" + subTitle.Attributes["name"].Value);
                }
            }
        }

        /// <summary>
        ///     返回编号对应的会计科目名称
        /// </summary>
        /// <param name="title">一级科目编号</param>
        /// <param name="subtitle">二级科目编号</param>
        /// <returns>名称</returns>
        public static string GetTitleName(int? title, int? subtitle = null)
        {
            if (!title.HasValue)
                return null;

            var nav = XmlDoc.CreateNavigator();

            if (subtitle.HasValue)
            {
                var res = nav.Select(
                                     String.Format(
                                                   "/Titles/title[@id={0}]/subTitle[@id={1}]/@name",
                                                   title,
                                                   subtitle));
                if (res.Count == 0)
                    return null;
                res.MoveNext();
                return res.Current.Value;
            }
            else
            {
                var res = nav.Select(
                                     String.Format(
                                                   "/Titles/title[@id={0}]/@name",
                                                   title));
                if (res.Count == 0)
                    return null;
                res.MoveNext();
                return res.Current.Value;
            }
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
