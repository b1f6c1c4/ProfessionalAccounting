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
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
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
            if (from == to)
                return 1;

            var endpoint = date > DateTime.UtcNow ? "latest" : date.ToString("yyyy-MM-dd");
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

            var url =
                $"http://data.fixer.io/api/{endpoint}?access_key={ExchangeInfo.Config.AccessKey}&symbols={from},{to}";
            Console.WriteLine($"AccountingServer.BLL.FixerIoExchange.Invoke(): {endpoint}: {from}/{to}");
            var req = WebRequest.CreateHttp(url);
            req.KeepAlive = true;
            var res = req.GetResponse();
            double rate;
            using (var stream = res.GetResponseStream())
            {
                var reader = new StreamReader(stream ?? throw new NetworkInformationException());
                var json = JObject.Parse(reader.ReadToEnd());
                rate = json["rates"][to].Value<double>() / json["rates"][from].Value<double>();
            }

            m_Lock.EnterWriteLock();
            try
            {
                m_Cache[token] = (DateTime.UtcNow, rate);
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }

            return rate;
        }
    }
}
