using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     会计科目管理
    /// </summary>
    public static class TitleManager
    {
        /// <summary>
        ///     会计科目信息文档
        /// </summary>
        private static readonly XmlDocument XmlDoc;

        /// <summary>
        ///     读取会计科目信息
        /// </summary>
        static TitleManager()
        {
            try
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
            catch (Exception)
            {
                XmlDoc = null;
            }
        }

        /// <summary>
        ///     返回所有会计科目编号和名称
        /// </summary>
        /// <returns>编号和科目名称</returns>
        public static IEnumerable<Tuple<int, int?, string>> GetTitles()
        {
            CheckXml();
            // ReSharper disable once PossibleNullReferenceException
            foreach (XmlElement title in XmlDoc.DocumentElement.ChildNodes)
            {
                yield return new Tuple<int, int?, string>(
                    Convert.ToInt32(title.Attributes["id"].Value),
                    null,
                    title.Attributes["name"].Value);
                foreach (XmlElement subTitle in title.ChildNodes)
                    yield return new Tuple<int, int?, string>(
                        Convert.ToInt32(title.Attributes["id"].Value),
                        Convert.ToInt32(subTitle.Attributes["id"].Value),
                        title.Attributes["name"].Value + "-" + subTitle.Attributes["name"].Value);
            }
        }

        /// <summary>
        ///     检查会计科目信息是否已加载
        /// </summary>
        private static void CheckXml()
        {
            if (XmlDoc == null)
                throw new MethodAccessException("在加载AccountingServer.BLL.Resources.Titles.xml时失败，无法访问会计科目信息");
        }

        /// <summary>
        ///     返回编号对应的会计科目名称
        /// </summary>
        /// <param name="title">一级科目编号</param>
        /// <param name="subtitle">二级科目编号</param>
        /// <returns>名称</returns>
        public static string GetTitleName(int? title, int? subtitle = null)
        {
            CheckXml();
            if (!title.HasValue)
                return null;

            var nav = XmlDoc.CreateNavigator();

            if (subtitle.HasValue)
            {
                var res = nav.Select($"/Titles/title[@id={title}]/subTitle[@id={subtitle}]/@name");
                if (res.Count == 0)
                    return null;
                res.MoveNext();
                return res.Current.Value;
            }
            else
            {
                var res = nav.Select($"/Titles/title[@id={title}]/@name");
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
            CheckXml();
            return detail.SubTitle.HasValue
                       ? GetTitleName(detail.Title) + "-" + GetTitleName(detail.Title, detail.SubTitle)
                       : GetTitleName(detail.Title);
        }
    }
}
