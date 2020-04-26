using System;
using System.Collections;
using System.Collections.Generic;
using AccountingServer.BLL.Util;
using AccountingServer.Entities.Util;

namespace AccountingServer.Test
{
    public class MockConfigManager<T> : IConfigManager<T>
    {
        public MockConfigManager(T config) => Config = config;
        public T Config { get; }

        public void Reload(bool throws) => throw new NotImplementedException();
    }

    public class MockExchange : IExchange, IEnumerable
    {
        private readonly Dictionary<Tuple<DateTime, string>, double> m_Dic =
            new Dictionary<Tuple<DateTime, string>, double>();

        public IEnumerator GetEnumerator() => m_Dic.GetEnumerator();

        public double From(DateTime date, string target) => target == BaseCurrency.Now
            ? 1
            : m_Dic[new Tuple<DateTime, string>(date, target)];

        public double To(DateTime date, string target) => target == BaseCurrency.Now
            ? 1
            : 1D / m_Dic[new Tuple<DateTime, string>(date, target)];

        public void Add(DateTime date, string target, double val) => m_Dic.Add(
            new Tuple<DateTime, string>(date, target),
            val);
    }
}
