/* Copyright (C) 2020-2021 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Xml.Serialization;
using AccountingServer.Entities.Util;
using Newtonsoft.Json.Linq;

namespace AccountingServer.BLL.Util
{
    internal static class ExchangeFactory
    {
        public static IExchange Instance { get; set; } =
            new FixerIoExchange { Successor = new CoinMarketCapExchange() };
    }

    /// <summary>
    ///     汇率查询
    /// </summary>
    internal interface IExchange
    {
        /// <summary>
        ///     汇率查询
        /// </summary>
        /// <example>
        ///     <c>IExchange.Query("USD", "CNY") == 6.8</c>
        ///     <c>IExchange.Query("CNY", "USD") == 0.147</c>
        /// </example>
        /// <param name="from">购汇币种</param>
        /// <param name="to">结汇币种</param>
        /// <returns>汇率</returns>
        double Query(string from, string to);
    }

    /// <summary>
    ///     历史汇率查询
    /// </summary>
    public interface IHistoricalExchange
    {
        /// <summary>
        ///     汇率查询
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="from">购汇币种</param>
        /// <param name="to">结汇币种</param>
        /// <returns>汇率</returns>
        double Query(DateTime? date, string from, string to);
    }

    [Serializable]
    [XmlRoot("Exchange")]
    public class ExchangeInfo
    {
        [XmlElement("fixer_io")] public string FixerAccessKey;
        [XmlElement("coin_mkt")] public string CoinAccessKey;
    }

    internal abstract class ExchangeApi : IExchange
    {
        /// <summary>
        ///     汇率API配置
        /// </summary>
        protected static IConfigManager<ExchangeInfo> ExchangeInfo { get; set; } =
            new ConfigManager<ExchangeInfo>("Exchange.xml");

        internal ExchangeApi Successor { private protected get; set; }

        double IExchange.From(DateTime date, string target)
        {
            try
            {
                return From(date, target);
            }
            catch (Exception e)
            {
                if (Successor == null)
                    throw;

                return Successor.From(date, target);
            }
        }

        double IExchange.To(DateTime date, string target)
        {
            try
            {
                return To(date, target);
            }
            catch (Exception e)
            {
                if (Successor == null)
                    throw;

                return Successor.To(date, target);
            }
        }

        protected abstract double From(DateTime date, string target);

        protected abstract double To(DateTime date, string target);
    }

    /// <summary>
    ///     利用fixer.io查询汇率
    /// </summary>
    internal class FixerIoExchange : ExchangeApi
    {
        /// <summary>
        ///     缓存
        /// </summary>
        private readonly Dictionary<string, (DateTime, double)> m_Cache =
            new Dictionary<string, (DateTime, double)>();

        private readonly ReaderWriterLockSlim m_Lock = new ReaderWriterLockSlim();

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
        private double Invoke(DateTime date, string from, string to)
        {
            var url =
                $"http://data.fixer.io/api/{endpoint}?access_key={ExchangeInfo.Config.AccessKey}&symbols={from},{to}";
            Console.WriteLine($"AccountingServer.BLL.FixerIoExchange.Invoke(): {endpoint}: {from}/{to}");
            var req = WebRequest.CreateHttp(url);
            req.KeepAlive = true;
            var res = req.GetResponse();
            using var stream = res.GetResponseStream();
            var reader = new StreamReader(stream ?? throw new NetworkInformationException());
            var json = JObject.Parse(reader.ReadToEnd());
            return json["rates"]![to]!.Value<double>() / json["rates"]![from]!.Value<double>();
        }
    }

    /// <summary>
    ///     利用coinmartketcap.com查询实时汇率
    /// </summary>
    internal class CoinMarketCapExchange : ExchangeApi
    {
        /// <summary>
        ///     缓存
        /// </summary>
        private readonly Dictionary<string, (DateTime, double)> m_Cache =
            new Dictionary<string, (DateTime, double)>();

        private readonly ReaderWriterLockSlim m_Lock = new ReaderWriterLockSlim();

        /// <inheritdoc />
        protected override double From(DateTime date, string target) => Invoke(date, target, BaseCurrency.Now);

        /// <inheritdoc />
        protected override double To(DateTime date, string target) => Invoke(date, BaseCurrency.Now, target);

        /// <summary>
        ///     调用查询接口
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="from">基准</param>
        /// <param name="to">目标</param>
        /// <returns>汇率</returns>
        private double Invoke(DateTime date, string from, string to)
        {
            if (from == to)
                return 1;

            var endpoint = date > DateTime.UtcNow
                ? "latest"
                : throw new ApplicationException("Historical rate unavailable.");
            var token = $"{endpoint}#{from}#{to}";

            m_Lock.EnterReadLock();
            try
            {
                if (m_Cache.ContainsKey(token))
                {
                    var (d, v) = m_Cache[token];
                    if ((DateTime.UtcNow - d).TotalHours < 12)
                        return v;
                }
            }
            finally
            {
                m_Lock.ExitReadLock();
            }

            double? rate = null;

            {
                var url =
                    $"https://pro-api.coinmarketcap.com/v1/cryptocurrency/quotes/{endpoint}?symbol={to}&convert={from}";

                Console.WriteLine($"AccountingServer.BLL.CoinMarketCapExchange.Invoke(): {endpoint}: {from}/{to}");
                var req = WebRequest.CreateHttp(url);
                req.KeepAlive = true;
                req.Headers.Add("X-CMC_PRO_API_KEY", ExchangeInfo.Config.CoinAccessKey);
                req.Accept = "application/json";
                var res = req.GetResponse();
                using (var stream = res.GetResponseStream())
                {
                    var reader = new StreamReader(stream ?? throw new NetworkInformationException());
                    var json = JObject.Parse(reader.ReadToEnd());
                    if (json["status"]["error_code"].Value<int>() == 0)
                        rate = json["data"][to]["quote"][from]["price"].Value<double>();
                }
            }
            if (!rate.HasValue)
            {
                var url =
                    $"https://pro-api.coinmarketcap.com/v1/cryptocurrency/quotes/{endpoint}?symbol={from}&convert={to}";

                Console.WriteLine($"AccountingServer.BLL.CoinMarketCapExchange.Invoke(): {endpoint}: {to}/{from}");
                var req = WebRequest.CreateHttp(url);
                req.KeepAlive = true;
                req.Headers.Add("X-CMC_PRO_API_KEY", ExchangeInfo.Config.CoinAccessKey);
                req.Accept = "application/json";
                var res = req.GetResponse();
                using (var stream = res.GetResponseStream())
                {
                    var reader = new StreamReader(stream ?? throw new NetworkInformationException());
                    var json = JObject.Parse(reader.ReadToEnd());
                    if (json["status"]["error_code"].Value<int>() == 0)
                        rate = json["data"][from]["quote"][to]["price"].Value<double>();
                }
            }

            if (!rate.HasValue)
                throw new InvalidOperationException("Unknown currency.");

            m_Lock.EnterWriteLock();
            try
            {
                m_Cache[token] = (DateTime.UtcNow, rate.Value);
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }

            return rate.Value;
        }
    }
}
