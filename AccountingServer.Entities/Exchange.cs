using System;

namespace AccountingServer.Entities
{
    /// <summary>
    ///     汇率
    /// </summary>
    [Serializable]
    public class ExchangeRecord
    {
        /// <summary>
        ///     日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        ///     购汇币种
        /// </summary>
        public string From { get; set; }

        /// <summary>
        ///     结汇币种
        /// </summary>
        public string To { get; set; }

        /// <summary>
        ///     汇率
        /// </summary>
        public double Value { get; set; }
    }
}
