using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Xml.Serialization;
using AccountingServer.Entities.Util;
using Newtonsoft.Json.Linq;

namespace AccountingServer.BLL.Util
{
    public static class ExchangeFactory
    {
        public static IExchange Instance { get; set; } = new FixerIoExchange();
    }

    /// <summary>
    ///     汇率查询
    /// </summary>
    public interface IExchange
    {
        /// <summary>
        ///     买入汇率
        /// </summary>
        /// <example>
        ///     若<c>BaseCurrency.Now == "CNY"</c>，则<c>IExchange.From(dt, "USD") == 6.8</c>
        /// </example>
        /// <param name="date">日期</param>
        /// <param name="target">购汇币种</param>
        /// <returns>汇率</returns>
        double From(DateTime date, string target);

        /// <summary>
        ///     卖出汇率
        /// </summary>
        /// <example>
        ///     若<c>BaseCurrency.Now == "CNY"</c>，则<c>IExchange.From(dt, "USD") == 0.147</c>
        /// </example>
        /// <param name="date">日期</param>
        /// <param name="target">结汇币种</param>
        /// <returns>汇率</returns>
        double To(DateTime date, string target);
    }

    [Serializable]
    [XmlRoot("Exchange")]
    public class ExchangeInfo
    {
        [XmlElement("access_key")] public string AccessKey;
    }

    /// <summary>
    ///     利用fixer.io查询汇率
    /// </summary>
    internal class FixerIoExchange : IExchange
    {
        /// <summary>
        ///     汇率API配置
        /// </summary>
        public static IConfigManager<ExchangeInfo> ExchangeInfo { private get; set; } =
            new ConfigManager<ExchangeInfo>("Exchange.xml");

        /// <inheritdoc />
        public double From(DateTime date, string target) => Invoke(date, target, BaseCurrency.Now);

        /// <inheritdoc />
        public double To(DateTime date, string target) => Invoke(date, BaseCurrency.Now, target);

        /// <summary>
        ///     调用查询接口
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="from">基准</param>
        /// <param name="to">目标</param>
        /// <returns>汇率</returns>
        private static double Invoke(DateTime date, string from, string to)
        {
            if (from == to)
                return 1;

            var endpoint = date > DateTime.UtcNow ? "latest" : date.ToString("yyyy-MM-dd");
            var url = $"http://data.fixer.io/api/{endpoint}?access_key={ExchangeInfo.Config.AccessKey}&symbols={from},{to}";
            Console.WriteLine($"AccountingServer.BLL.FixerIoExchange.Invoke(): {endpoint}: {from}/{to}");
            var req = WebRequest.CreateHttp(url);
            req.KeepAlive = true;
            var res = req.GetResponse();
            using (var stream = res.GetResponseStream())
            {
                if (stream == null)
                    throw new NetworkInformationException();

                var reader = new StreamReader(stream);
                var json = JObject.Parse(reader.ReadToEnd());
                return json["rates"][to].Value<double>() / json["rates"][from].Value<double>();
            }
        }
    }
}
