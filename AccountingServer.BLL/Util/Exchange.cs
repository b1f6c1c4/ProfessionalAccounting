/* Copyright (C) 2020-2025 b1f6c1c4
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
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AccountingServer.Entities.Util;
using Newtonsoft.Json.Linq;

namespace AccountingServer.BLL.Util;

internal static class ExchangeFactory
{
    public static IHistoricalExchange HistoricalInstance { get; set; } = new FixerIoExchange();

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
    ValueTask<double> Query(string from, string to);
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
    ValueTask<double> Query(DateTime? date, string from, string to);
}

[Serializable]
[XmlRoot("Exchange")]
public class ExchangeInfo
{
    [XmlElement("fixer_io")] public List<string> FixerAccessKey;
    [XmlElement("coin_mkt")] public List<string> CoinAccessKey;

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

internal class RoundRobinApiKeys
{
    private long m_CurrentId;
    private long GetCurrentId() => Interlocked.Increment(ref m_CurrentId);

    public async ValueTask<TOut> Execute<TOut>(IList<string> keys, Func<string, ValueTask<TOut?>> func)
        where TOut : struct
    {
        if (keys.Count == 0)
            throw new UnauthorizedAccessException("No access key found");

        var count = keys.Count;
        var st = (int)(GetCurrentId() % count);
        for (var i = 0; i < count; i++)
            if (await func(keys[(i + st) % count]) is var res && res.HasValue)
                return res.Value;
        throw new UnauthorizedAccessException("No more access keys");
    }
}

internal abstract class ExchangeApi : IExchange
{
    /// <summary>
    ///     汇率API配置
    /// </summary>
    static ExchangeApi()
        => Cfg.RegisterType<ExchangeInfo>("Exchange");

    internal ExchangeApi Successor { private get; init; }

    public ValueTask<double> Query(string from, string to)
        => Query(from, to, Enumerable.Empty<Exception>());

    private ValueTask<double> Query(string from, string to, IEnumerable<Exception> err)
    {
        try
        {
            return Invoke(from, to);
        }
        catch (Exception e)
        {
            var ne = err.Prepend(e);
            if (Successor != null)
                return Successor.Query(from, to, ne);

            throw new AggregateException(ne);
        }
    }

    protected abstract ValueTask<double> Invoke(string from, string to);
}

/// <summary>
///     利用fixer.io查询汇率
/// </summary>
internal class FixerIoExchange : ExchangeApi, IHistoricalExchange
{
    private readonly RoundRobinApiKeys m_ApiKeys = new();

    public ValueTask<double> Query(DateTime? date, string from, string to)
    {
        if (!date.HasValue)
            return Invoke(from, to);

        return m_ApiKeys.Execute(Cfg.Get<ExchangeInfo>().FixerAccessKey,
            key => PartialInvoke(from, to, key, date!.Value.ToString("yyyy-MM-dd")));
    }

    protected override ValueTask<double> Invoke(string from, string to)
        => m_ApiKeys.Execute(Cfg.Get<ExchangeInfo>().FixerAccessKey,
            key => PartialInvoke(from, to, key, "latest"));

    private static async ValueTask<double?> PartialInvoke(string from, string to, string key, string endpoint)
    {
        var client = new HttpClient();
        var response =
            await client.GetAsync($"http://data.fixer.io/api/{endpoint}?access_key={key}&symbols={from},{to}");
        response.EnsureSuccessStatusCode();
        var json = JObject.Parse(await response.Content.ReadAsStringAsync());
        if (json["success"]!.Value<bool>())
            return json["rates"]![to]!.Value<double>() / json["rates"]![from]!.Value<double>();
        return null;
    }
}

/// <summary>
///     利用coinmartketcap.com查询实时汇率
/// </summary>
internal class CoinMarketCapExchange : ExchangeApi
{
    private readonly RoundRobinApiKeys m_ApiKeys = new();

    protected override ValueTask<double> Invoke(string from, string to)
    {
        int? fromId = null, toId = null;
        var isCrypto = false;
        foreach (var cur in Cfg.Get<ExchangeInfo>().Currencies)
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

        if (!fromId.HasValue || !toId.HasValue || !isCrypto)
            throw new InvalidOperationException("Not a cryptocurrency");
        return m_ApiKeys.Execute(Cfg.Get<ExchangeInfo>().CoinAccessKey,
            key => PartialInvoke(fromId.Value, toId.Value, key));
    }

    private static async ValueTask<double?> PartialInvoke(int fromId, int toId, string key)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", key);
        client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
        var response = await client.GetAsync(
            $"https://pro-api.coinmarketcap.com/v1/cryptocurrency/quotes/latest?id={fromId}&convert_id={toId}");
        var json = JObject.Parse(await response.Content.ReadAsStringAsync());
        return json["data"]?[fromId.ToString()]?["quote"]?[toId.ToString()]?["price"]?.Value<double>();
    }
}
