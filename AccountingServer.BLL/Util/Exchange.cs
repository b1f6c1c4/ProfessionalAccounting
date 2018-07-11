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
        public static IExchange Instance { get; set; } = new ExchangeCache(new FixerIoExchange());
    }

    /// <summary>
    ///     汇率查询
    /// </summary>
    public interface IExchange
    {
        /// <summary>
        ///     买入汇率
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="target">目标</param>
        /// <returns>汇率</returns>
        double From(DateTime date, string target);

        /// <summary>
        ///     卖出汇率
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="target">目标</param>
        /// <returns>汇率</returns>
        double To(DateTime date, string target);
    }

    /// <summary>
    ///     汇率缓存
    /// </summary>
    internal sealed class ExchangeCache : IExchange
    {
        /// <summary>
        ///     内部汇率查询
        /// </summary>
        private readonly IExchange m_Exchange;

        /// <summary>
        ///     买入缓存
        /// </summary>
        private readonly ConcurrentDictionary<(string, DateTime), double> m_FromCache =
            new ConcurrentDictionary<(string, DateTime), double>();

        /// <summary>
        ///     卖出缓存
        /// </summary>
        private readonly ConcurrentDictionary<(string, DateTime), double> m_ToCache =
            new ConcurrentDictionary<(string, DateTime), double>();

        public ExchangeCache(IExchange exchange) => m_Exchange = exchange;

        /// <inheritdoc />
        public double From(DateTime date, string target)
            => m_FromCache.GetOrAdd((target, date), _ => m_Exchange.From(date, target));

        /// <inheritdoc />
        public double To(DateTime date, string target)
            => m_ToCache.GetOrAdd((target, date), _ => m_Exchange.To(date, target));
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
            var req = WebRequest.CreateHttp(
                $"http://data.fixer.io/api/{endpoint}?access_key={ExchangeInfo.Config.AccessKey}&symbols={from},{to}");
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
