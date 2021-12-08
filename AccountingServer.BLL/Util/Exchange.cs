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
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            new CoinMarketCapExchange { Successor = new FixerIoExchange() };
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

        [XmlArray("currencies")]
        [XmlArrayItem("conventional", typeof(ConventionalCurrency))]
        [XmlArrayItem("crypto", typeof(CryptoCurrency))]
        public List<Currency> Currencies;
    }

    [Serializable]
    public abstract class Currency { }

    [Serializable]
    public class ConventionalCurrency : Currency
    {
        [XmlAttribute("id")]
        public int CoinMarketCapId { get; set; }

        [XmlText]
        public string Symbol { get; set; }
    }

    [Serializable]
    public class CryptoCurrency : Currency
    {
        [XmlAttribute("id")]
        public int CoinMarketCapId { get; set; }

        [XmlText]
        public string Symbol { get; set; }
    }

    internal abstract class ExchangeApi : IExchange
    {
        /// <summary>
        ///     汇率API配置
        /// </summary>
        protected static IConfigManager<ExchangeInfo> ExchangeInfo { get; set; } =
            new ConfigManager<ExchangeInfo>("Exchange.xml");

        internal ExchangeApi Successor { private get; init; }

        public double Query(string from, string to)
            => Query(from, to, Enumerable.Empty<Exception>());

        private double Query(string from, string to, IEnumerable<Exception> err)
        {
            try
            {
                return Invoke(from, to);
            }
            catch (Exception e)
            {
                var ne = err.Append(e);
                if (Successor != null)
                    return Successor.Query(from, to, ne);

                throw new AggregateException(ne);
            }
        }

        protected abstract double Invoke(string from, string to);
    }

    /// <summary>
    ///     利用fixer.io查询汇率
    /// </summary>
    internal class FixerIoExchange : ExchangeApi
    {
        protected override double Invoke(string from, string to)
        {
            var url =
                $"http://data.fixer.io/api/latest?access_key={ExchangeInfo.Config.FixerAccessKey}&symbols={from},{to}";
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
        protected override double Invoke(string from, string to)
        {
            int? fromId = null, toId = null;
            var isCrypto = false;
            foreach (var cur in ExchangeInfo.Config.Currencies)
                switch (cur)
                {
                    case CryptoCurrency c:
                        if (c.Symbol == from)
                        {
                            fromId = c.CoinMarketCapId;
                            isCrypto = true;
                        }
                        if (c.Symbol == to)
                        {
                            toId = c.CoinMarketCapId;
                            isCrypto = true;
                        }
                        break;
                    case ConventionalCurrency c:
                        if (c.Symbol == from)
                            fromId = c.CoinMarketCapId;
                        if (c.Symbol == to)
                            toId = c.CoinMarketCapId;
                        break;
                }
            if (fromId.HasValue && toId.HasValue && isCrypto)
                return PartialInvoke(fromId.Value, toId.Value);
            throw new InvalidOperationException();
        }

        private static double PartialInvoke(int fromId, int toId)
        {
            var url =
                $"https://pro-api.coinmarketcap.com/v1/cryptocurrency/quotes/latest?id={fromId}&convert_id={toId}";
            var req = WebRequest.CreateHttp(url);
            req.KeepAlive = true;
            req.Headers.Add("X-CMC_PRO_API_KEY", ExchangeInfo.Config.CoinAccessKey);
            req.Accept = "application/json";
            var res = req.GetResponse();
            using var stream = res.GetResponseStream();
            var reader = new StreamReader(stream ?? throw new NetworkInformationException());
            var json = JObject.Parse(reader.ReadToEnd());
            return json["data"]![fromId.ToString()]!["quote"]![toId.ToString()]!["price"]!.Value<double>();
        }
    }
}
