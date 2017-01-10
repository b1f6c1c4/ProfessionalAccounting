using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    [Serializable]
    [XmlRoot("Titles")]
    public class TitleInfos
    {
        [XmlElement("title")] public List<TitleInfo> Titles;
    }

    [Serializable]
    public class TitleInfo
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlAttribute("virtual")]
        [DefaultValue(false)]
        public bool IsVirtual { get; set; }

        /// <summary>
        ///     方向：
        ///     <c>0</c>表示任意
        ///     <c>1</c>表示均在借方
        ///     <c>2</c>表示汇总在借方
        ///     <c>-1</c>表示均在贷方
        ///     <c>-2</c>表示汇总在贷方
        /// </summary>
        [XmlAttribute("dir")]
        [DefaultValue(0)]
        public int Direction { get; set; }

        [XmlElement("subTitle")] public List<SubTitleInfo> SubTitles;
    }

    public class SubTitleInfo
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("id")]
        public int Id { get; set; }

        /// <summary>
        ///     方向：
        ///     <c>0</c>表示任意
        ///     <c>1</c>表示均在借方
        ///     <c>2</c>表示汇总在借方
        ///     <c>-1</c>表示均在贷方
        ///     <c>-2</c>表示汇总在贷方
        /// </summary>
        [XmlAttribute("dir")]
        [DefaultValue(0)]
        public int Direction { get; set; }
    }

    /// <summary>
    ///     会计科目管理
    /// </summary>
    public static class TitleManager
    {
        /// <summary>
        ///     会计科目信息文档
        /// </summary>
        private static readonly ConfigManager<TitleInfos> TitleInfos;

        /// <summary>
        ///     读取会计科目信息
        /// </summary>
        static TitleManager() { TitleInfos = new ConfigManager<TitleInfos>("Titles.xml"); }

        /// <summary>
        ///     返回所有会计科目编号和名称
        /// </summary>
        /// <returns>编号和科目名称</returns>
        public static IReadOnlyList<TitleInfo> Titles => TitleInfos.Config.Titles.AsReadOnly();

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

            var t0 = Titles.FirstOrDefault(t => t.Id == title.Value);
            return !subtitle.HasValue ? t0?.Name : t0?.SubTitles?.FirstOrDefault(t => t.Id == subtitle.Value)?.Name;
        }

        /// <summary>
        ///     返回细目对应的会计科目名称
        /// </summary>
        /// <param name="detail">细目</param>
        /// <returns>名称</returns>
        public static string GetTitleName(VoucherDetail detail) =>
            detail.SubTitle.HasValue
                ? GetTitleName(detail.Title) + "-" + GetTitleName(detail.Title, detail.SubTitle)
                : GetTitleName(detail.Title);
    }
}
