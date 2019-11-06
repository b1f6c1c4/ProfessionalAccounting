using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL.Util
{
    [Serializable]
    [XmlRoot("BaseCurrencies")]
    public class BaseCurrencyInfos
    {
        [XmlElement("BaseCurrency")] public List<BaseCurrencyInfo> Infos;
    }

    [Serializable]
    public class BaseCurrencyInfo
    {
        [XmlElement]
        public DateTime? Date { get; set; }

        [XmlElement]
        public string Currency { get; set; }
    }

    /// <summary>
    ///     记账本位币管理
    /// </summary>
    public class BaseCurrency
    {
        /// <summary>
        ///     记账本位币信息文档
        /// </summary>
        public static IConfigManager<BaseCurrencyInfos> BaseCurrencyInfos { private get; set; } =
            new ConfigManager<BaseCurrencyInfos>("BaseCurrency.xml");

        public static IReadOnlyList<BaseCurrencyInfo> History => BaseCurrencyInfos.Config.Infos.AsReadOnly();

        public static string Now
            => BaseCurrencyInfos.Config.Infos.Last().Currency;

        public static string At(DateTime? dt)
            => BaseCurrencyInfos.Config.Infos.Last(bc => DateHelper.CompareDate(bc.Date, dt) <= 0).Currency;
    }
}
